using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NPOI.XSSF.UserModel;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Datasets.Proteomes;
using OmicIntegrator.Helpers;
using Org.BouncyCastle.Bcpg.Sig;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes.Specific
{
    public class Reichel : IProteomeLoader
    {
        public async Task LoadPeptides(LoadPeptidesRequest request)
        {
            var UniprotFile = ConsoleInput.AskFileName("UniProt TAIR file (*.xlsx):");

            BaseCtx ctx = new();
            XSSFWorkbook uniprot = new(UniprotFile);
            var sheet = uniprot.GetSheetAt(0);

            var headers = sheet.GetRow(0);

            var UniProtColumns = headers.ToDictionary(e => e.ColumnIndex, e => e.StringCellValue);

            var ColUniProtId = ConsoleInput.PickItem(UniProtColumns, "Select UniProt code column:");
            var ColTairIds = ConsoleInput.PickItem(UniProtColumns, "Select TAIR code column:");

            Dictionary<string, IEnumerable<string>> CodesUniProtTair = new();

            for (int f = 1; f <= sheet.LastRowNum; f++)
            {
                var row = sheet.GetRow(f);

                CodesUniProtTair.Add(row.GetCell(ColUniProtId).StringCellValue,
                                       row.GetCell(ColTairIds).StringCellValue
                                            .Replace(" ", ";")
                                            .Replace("/", ";")
                                            .Split(";")
                                            .Where(c => !string.IsNullOrWhiteSpace(c))
                                            .Select(c => c.Trim().ToUpper())
                                            .Distinct()
                                            .ToList());
            }

            uniprot.Close();

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

            var CodesUniProtFeatureIds = CodesUniProtTair
                .SelectMany(c => c.Value.Select(fi => new { UniProt = c.Key, FeatureCode = fi }))
                .Join(FeaturesCodesIds,
                      u => u.FeatureCode,
                      f => f.Key,
                      (u, f) => new { u.UniProt, FeatureId = f.Value })
                .GroupBy(u => u.UniProt)
                .ToDictionary(g => g.Key, g => g.Select(f => f.FeatureId).ToList().AsEnumerable());

            XSSFWorkbook book = new(request.FileName);
            sheet = book.GetSheetAt(0);

            List<Peptide> Peptidos = new();
            List<PeptideFeature> PeptidesFeatures = new();

            for (var f = 1; f <= sheet.LastRowNum; f++)
            {
                var row = sheet.GetRow(f);

                var CodesUniProt = row.GetCell(1).StringCellValue.Split(";").Select(c => c.Trim().ToUpper()).Distinct();

                var Peptide = new Peptide()
                {
                    DatasetId = request.DatasetId,
                    IdInDataSet = row.GetCell(0).NumericCellValue.ToString()
                };
                Peptidos.Add(Peptide);

                List<long> FeatureIds = new();
                foreach (var uni in CodesUniProt)
                {
                    if (CodesUniProtFeatureIds.TryGetValue(uni, out var fea))
                    {
                        FeatureIds.AddRange(fea);
                    }
                }

                if (FeatureIds.Any())
                {
                    PeptidesFeatures.AddRange(FeatureIds.Distinct().Select(i => new PeptideFeature()
                    {
                        Peptide = Peptide,
                        FeatureId = i,
                    }));
                }
            }

            book.Close();

            await ctx.BulkInsertAsync
                (Peptidos,
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
            BaseCtx ctx = new();

            XSSFWorkbook book = new(request.FileName);
            var sheet = book.GetSheetAt(0);

            var headers = sheet.GetRow(0);

            var SamplesColumns = ConsoleInput.PickItems(headers.ToDictionary(e => e.ColumnIndex, e => e.StringCellValue), "Select samples columns:");

            var samples = SamplesColumns.ToDictionary
                (c => c,
                 c => new Sample()
                 {
                     TreatmentId = request.TreatmentId,
                     Description = headers.GetCell(c).StringCellValue, 
                     Type = SampleType.Proteome,
                 });

            ctx.Samples.AddRange(samples.Select(m => m.Value));
            await ctx.SaveChangesAsync();

            List<ProteomeValue> values = new();

            for (var f = 1; f <= sheet.LastRowNum; f++)
            {
                var Fila = sheet.GetRow(f);

                values.AddRange(samples.Select(m => new ProteomeValue()
                {
                    PeptideId = request.PeptidesIds[Fila.GetCell(0).NumericCellValue.ToString()],
                    Sample = m.Value,
                    SampleId = m.Value.Id,
                    Intensity = (decimal)Fila.GetCell(m.Key).NumericCellValue
                }));
            }

            book.Close();

            await ctx.BulkInsertAsync(values);
        }
    }
}
