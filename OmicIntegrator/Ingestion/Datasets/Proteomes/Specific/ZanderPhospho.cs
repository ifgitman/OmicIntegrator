using EFCore.BulkExtensions;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Datasets.Proteomes;
using OmicIntegrator.Helpers;
using System.Text;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes.Specific
{
    public class ZanderPhospho : IProteomeLoader
    {
        public async Task LoadPeptides(LoadPeptidesRequest request)
        {
            using var file = File.OpenText(request.FileName);

            BaseCtx ctx = new();

            int fieldIndex = 0;
            var headerFields = CsvSplitter.Split(await file.ReadLineAsync())
                .ToDictionary(c => fieldIndex++,
                              c => c);

            var sequenceCol = ConsoleInput.PickItem(headerFields, "Sequence column:");
            var sequenceProbabilitiesCol = ConsoleInput.PickItem(headerFields, "Sequence phosphorilation probabilities column:");

            List<Peptide> Peptides = [];
            List<PeptideModification> PeptidesModifications = [];

            while (!file.EndOfStream)
            {
                var Line = await file.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(Line))
                    continue;

                var fields = CsvSplitter.Split(Line);

                var sequenceWithSpaces = fields[sequenceCol];

                var peptide = new Peptide()
                {
                    DatasetId = request.DatasetId,
                    IdInDataSet = fields[0],
                    Sequence = sequenceWithSpaces.Replace("_", "")
                };
                Peptides.Add(peptide);

                var sequencePhosphoProbs = fields[sequenceProbabilitiesCol];

                int? parenthesisStart = null;

                Dictionary<int, decimal> probabilities = [];
                StringBuilder seqProbs = new();

                for (int i = 0; i < sequencePhosphoProbs.Length; i++)
                {
                    var character = sequencePhosphoProbs[i];

                    if (!parenthesisStart.HasValue)
                    {
                        if (character == '(')
                            parenthesisStart = i + 1;
                        else
                            seqProbs.Append(character);
                    }
                    else if (character == ')')
                    {
                        probabilities.Add(seqProbs.Length - 1,
                                          decimal.Parse(sequencePhosphoProbs
                                                 .Substring(parenthesisStart.Value, i - parenthesisStart.Value),
                                                            System.Globalization.CultureInfo.InvariantCulture));
                        parenthesisStart = null;
                    }
                }

                if (probabilities.Any())
                {
                    var probsStart = Helpers.IndexOfBorders(sequenceWithSpaces.Replace("_", "").Split(";").First(),
                                                            seqProbs.ToString());

                    if (!probsStart.HasValue)
                        System.Diagnostics.Debugger.Break();

                    if (probsStart.HasValue)
                    {
                        PeptidesModifications.Add(new()
                        {
                            Peptide = peptide,
                            ModificationType = "Phosphorylation",
                            ResiduePosition = probsStart.Value + probabilities.OrderByDescending(p => p.Value).First().Key
                        });
                    }
                }
            }

            file.Close();

            await ctx.BulkInsertAsync
                (Peptides,
                c =>
                {
                    c.SetOutputIdentity = true;
                    c.PreserveInsertOrder = true;
                });

            foreach (var f in PeptidesModifications)
                f.PeptideId= f.Peptide.Id;

            await ctx.BulkInsertAsync(PeptidesModifications);

            await PeptideSequenceMapper.Map(request.GenomeId, Peptides);
        }
        public async Task LoadSamples(LoadSamplesRequest request)
        {
            using var file = File.OpenText(request.FileName);

            BaseCtx ctx = new();

            int fieldIndex = 0;
            var headerFields = CsvSplitter.Split(await file.ReadLineAsync())
                .ToDictionary(c => fieldIndex++,
                              c => c);

            var samplesColumns = ConsoleInput.PickItems(headerFields, "Select samples columns:");

            var samples = samplesColumns.ToDictionary
                (c => c,
                 c => new Sample()
                 {
                     TreatmentId = request.TreatmentId,
                     Description = headerFields[c],
                     Type = SampleType.Phosphoproteome,
                     IntensityThreshold = 0,
                 });

            ctx.Samples.AddRange(samples.Select(m => m.Value));
            await ctx.SaveChangesAsync();

            List<ProteomeValue> values = new();

            while (!file.EndOfStream)
            {
                var Line = await file.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(Line))
                    continue;

                var fields = CsvSplitter.Split(Line);

                values.AddRange(samples.Select(m => new ProteomeValue()
                {
                    PeptideId = request.PeptidesIds[fields[0]],
                    Sample = m.Value,
                    SampleId = m.Value.Id,
                    Intensity = decimal.Parse(fields[m.Key], System.Globalization.CultureInfo.InvariantCulture)
                }));
            }

            file.Close();

            await ctx.BulkInsertAsync(values);
        }
    }
}
