using EFCore.BulkExtensions;
using NetTopologySuite.Noding;
using NPOI.XSSF.UserModel;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Datasets.Proteomes;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes.Specific
{
    public class Kruse : IProteomeLoader
    {
        private static LoadedFile ReadFile(string FileName, bool LoadSampleColumns = false)
        {
            XSSFWorkbook book = new(FileName);
            var sheet = book.GetSheetAt(0);

            var headers = sheet.GetRow(0).ToDictionary(e => e.ColumnIndex, e => e.StringCellValue);

            var IdsColumns = ConsoleInput.PickItems(headers, "Select peptide ids columns ('Mr(expt)' & 'Score'):");

            var SequenceCol = ConsoleInput.PickItem(headers, "Select sequence column:");
            var ModificationsCol = ConsoleInput.PickItem(headers, "Select modifications column:");

            IEnumerable<int> SamplesColumns = [];

            if (LoadSampleColumns)
            {
                SamplesColumns = ConsoleInput.PickItems(headers, "Select samples columns):");
            }

            List<PeptideInfo> Rows = new();
            for (var f = 1; f <= sheet.LastRowNum; f++)
            {
                var row = sheet.GetRow(f);

                Rows.Add(new(string.Join(";", IdsColumns.Select(i => row.GetCell(i).NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                              row.GetCell(SequenceCol).StringCellValue,
                              row.GetCell(ModificationsCol).StringCellValue,
                              SamplesColumns.Select(c => (decimal)row.GetCell(c).NumericCellValue).ToList()));
            }

            book.Close();

            return new()
            {
                SampleNames = SamplesColumns.Select(s => headers[s]).ToList(),
                UniquePeptides = Rows.DistinctBy(d => d.IdInDataSet).ToList() 
            };
        }
        public async Task LoadPeptides(LoadPeptidesRequest request)
        {
            var file = ReadFile(request.FileName);

            BaseCtx ctx = new();

            List<Peptide> Peptides = [];
            List<PeptideModification> PeptidesModificactions = [];
            List<PeptideFeature> PeptidesFeatures = [];

            foreach (var pif in file.UniquePeptides)
            {

                Peptide peptide = new()
                {
                    DatasetId = request.DatasetId,
                    IdInDataSet = pif.IdInDataSet,
                    Sequence = pif.Sequence
                };
                Peptides.Add(peptide);

                var phosphorylations = pif.Modifications.Split(";")
                    .Select(m => new
                    {
                        modif = m,
                        parenthesisOpen = m.IndexOf("("),
                        parenthesisClose = m.IndexOf(")")
                    })
                    .Select(m => new
                    {
                        ModificationType = m.modif.Substring(0, m.parenthesisOpen).Trim(),
                        Residue = m.modif.Substring(m.parenthesisOpen + 1, m.parenthesisClose - m.parenthesisOpen - 1)
                    })
                    .Where(m => m.ModificationType == "Phospho")
                    .Select(m => int.Parse(m.Residue.Substring(1)) - 1)
                    .ToList();

                if (phosphorylations.Any())
                {
                    PeptidesModificactions.AddRange(phosphorylations.Select(f => new PeptideModification()
                    {
                        Peptide = peptide,
                        ModificationType = "Phosphorylation",
                        ResiduePosition = f
                    }));
                }
            }

            await ctx.BulkInsertAsync
                (Peptides,
                 c =>
                 {
                     c.SetOutputIdentity = true;
                     c.PreserveInsertOrder = true;
                 });

            foreach (var f in PeptidesModificactions)
                f.PeptideId = f.Peptide.Id;

            await ctx.BulkInsertAsync(PeptidesModificactions);

            await PeptideSequenceMapper.Map(request.GenomeId, Peptides);
        }

        public async Task LoadSamples(LoadSamplesRequest request)
        {
            var file = ReadFile(request.FileName, true);

            BaseCtx ctx = new();

            var samples = file.SampleNames.Select(c => new Sample()
            {
                TreatmentId = request.TreatmentId,
                Description = c,
                Type = SampleType.Proteome
            }).ToList();

            ctx.Samples.AddRange(samples);
            await ctx.SaveChangesAsync();

            List<ProteomeValue> values = new();

            for (int sind = 0; sind < file.SampleNames.Count(); sind++)
            {
                var sam = samples.ElementAt(sind);

                foreach (var pif in file.UniquePeptides)
                {
                    values.Add(new()
                    {
                        PeptideId = request.PeptidesIds[pif.IdInDataSet],
                        Sample = sam,
                        SampleId = sam.Id,
                        Intensity = pif.Values.ElementAt(sind)
                    });
                }
            }
 
            await ctx.BulkInsertAsync(values);

            Console.WriteLine("Done");
        }
        class LoadedFile
        {
            public IEnumerable<string> SampleNames = [];
            public IEnumerable<PeptideInfo> UniquePeptides = [];
        }
        record PeptideInfo(string IdInDataSet, string Sequence, string Modifications, IEnumerable<decimal> Values);
    }
}