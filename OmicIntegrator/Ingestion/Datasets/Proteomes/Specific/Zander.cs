using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Datasets.Proteomes;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes.Specific
{
    public class Zander : IProteomeLoader
    {
        public async Task LoadPeptides(LoadPeptidesRequest request)
        {
            using var file = File.OpenText(request.FileName);

            BaseCtx ctx = new();

            var ExcludedFeatureIds = await ConsoleInput.AskFeatureIds("Excluded features file:", request.GenomeId);

            var FeaturesCodesIds = (await ctx.Features
                .Where(f => f.Sequence.GenomeId == request.GenomeId
                            && f.Type == "gene"
                            && !ExcludedFeatureIds.Contains(f.Id))
                .Select(f => new { f.Id, Code = f.Code.ToUpper() })
                .ToListAsync())
                .GroupBy(f => f.Code)
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.Single().Id);

            List<Peptide> Peptides = new();
            List<PeptideFeature> PeptidesFeatures = new();

            _ = await file.ReadLineAsync();

            while (!file.EndOfStream)
            {
                var Line = await file.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(Line))
                    continue;

                var fields = CsvSplitter.Split(Line);

                var peptide = new Peptide()
                {
                    DatasetId = request.DatasetId,
                    IdInDataSet = fields[0]
                };
                Peptides.Add(peptide);

                var FeatureCodes = fields[1]
                    .Split(";")
                    .Select(c => c.Split(".").First().Trim().ToUpper())
                    .Distinct()
                    .ToList();

                foreach (var cod in FeatureCodes)
                {
                    if (FeaturesCodesIds.TryGetValue(cod, out var id))
                    {
                        PeptidesFeatures.Add(new()
                        {
                            Peptide = peptide,
                            FeatureId = id
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

            foreach (var f in PeptidesFeatures)
                f.PeptideId = f.Peptide.Id;

            await ctx.BulkInsertAsync(PeptidesFeatures);
        }

        public async Task LoadSamples(LoadSamplesRequest request)
        {
            using var file = File.OpenText(request.FileName);

            BaseCtx ctx = new();

            int fieldIndex = 0;
            var fieldHeaders = CsvSplitter.Split(await file.ReadLineAsync())
                .ToDictionary(c => fieldIndex++,
                              c => c);

            var SamplesColumns = ConsoleInput.PickItems(fieldHeaders);

            var Samples = SamplesColumns.ToDictionary
                (c => c,
                 c => new Sample()
                 {
                     TreatmentId = request.TreatmentId,
                     Description = fieldHeaders[c],
                     Type = SampleType.Proteome
                 });

            ctx.Samples.AddRange(Samples.Select(m => m.Value));
            await ctx.SaveChangesAsync();

            List<ProteomeValue> values = [];

            while (!file.EndOfStream)
            {
                var Linea = await file.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(Linea))
                    continue;

                var Campos = CsvSplitter.Split(Linea);

                values.AddRange(Samples.Select(m => new ProteomeValue()
                {
                    PeptideId = request.PeptidesIds[Campos[0]],
                    Sample = m.Value,
                    SampleId = m.Value.Id,
                    Intensity = decimal.Parse(Campos[m.Key], System.Globalization.CultureInfo.InvariantCulture)
                }));
            }

            file.Close();

            await ctx.BulkInsertAsync(values);
        }
    }
}
