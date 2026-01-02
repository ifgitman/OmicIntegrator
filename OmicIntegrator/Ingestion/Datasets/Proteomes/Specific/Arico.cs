using EFCore.BulkExtensions;
using NPOI.XSSF.UserModel;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Datasets.Proteomes;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes.Specific
{
    public class Arico : IProteomeLoader
    {
        public async Task LoadPeptides(LoadPeptidesRequest request)
        {
            XSSFWorkbook book = new(request.FileName);

            List<string> sheetNames = [];
            for (int h = 0; h < book.NumberOfSheets; h++)
            {
                sheetNames.Add(book.GetSheetAt(h).SheetName);
            }

            var peptidesSheet = book.GetSheet(ConsoleInput.PickItem(sheetNames, "Peptides sheet:"));

            var peptidesHeaders = peptidesSheet.GetRow(0).ToDictionary(e => e.ColumnIndex, e => e.StringCellValue);

            var PeptideIdCol = ConsoleInput.PickItem(peptidesHeaders, "PeptideId column:");
            var PeptideSiteIdCol = ConsoleInput.PickItem(peptidesHeaders, "PhosphoSiteId column:");
            var SequenceCol = ConsoleInput.PickItem(peptidesHeaders, "Sequence column:");

            var sitesSheet = book.GetSheet(ConsoleInput.PickItem(sheetNames, "Phosphosites sheet:"));

            var sitesHeaders = sitesSheet.GetRow(0).ToDictionary(e => e.ColumnIndex, e => e.StringCellValue);

            var SitesIdCol = ConsoleInput.PickItem(sitesHeaders, "PhosphoSite ID column:");
            var SiteSequenceCol = ConsoleInput.PickItem(sitesHeaders, "Phosphosite sequence column:");
            var ResiduePositionCol = ConsoleInput.PickItem(sitesHeaders, "Residue position column:");

            List<Peptide> peptides = [];
            List<PeptideModification> peptidesModifications = [];

            Dictionary<int, int> SitesResidues = [];
            Dictionary<int, string> SitesSequences = [];

            for (var f = 1; f <= sitesSheet.LastRowNum; f++)
            {
                var row = sitesSheet.GetRow(f);
                if (row == null)
                    continue;

                var siteId = (int)row.GetCell(SitesIdCol).NumericCellValue;

                SitesResidues.Add(siteId,
                                  (int)row.GetCell(ResiduePositionCol).NumericCellValue - 1);

                SitesSequences.Add(siteId,
                                     row.GetCell(SiteSequenceCol).StringCellValue);
            }

            BaseCtx ctx = new();

            for (var f = 1; f <= peptidesSheet.LastRowNum; f++)
            {
                var row = peptidesSheet.GetRow(f);
                if (row == null)
                    continue;

                var siteIdCell = row.GetCell(PeptideSiteIdCol);

                if (siteIdCell == null
                    || siteIdCell.CellType == NPOI.SS.UserModel.CellType.Blank)
                    continue;

                string sitesIds;

                if (siteIdCell.CellType == NPOI.SS.UserModel.CellType.Numeric)
                    sitesIds = siteIdCell.NumericCellValue.ToString();
                else
                    sitesIds = siteIdCell.StringCellValue;

                foreach (var siteId in sitesIds.Split(";"))
                {
                    var peptide = new Peptide()
                    {
                        DatasetId = request.DatasetId,
                        IdInDataSet = $"{row.GetCell(PeptideIdCol).NumericCellValue}.{siteId}",
                        Sequence = row.GetCell(SequenceCol).StringCellValue
                    };
                    peptides.Add(peptide);

                    peptidesModifications.Add(new()
                    {
                        Peptide = peptide,
                        ModificationType = "Phosphorylation",
                        ResiduePosition = Helpers.IndexOfBorders(peptide.Sequence,
                                                                 SitesSequences[int.Parse(siteId)]) +
                                                                 SitesResidues[int.Parse(siteId)]
                    });
                }
            }

            book.Close();

            await ctx.BulkInsertAsync
                (peptides,
                c =>
                {
                    c.SetOutputIdentity = true;
                    c.PreserveInsertOrder = true;
                });

            foreach (var f in peptidesModifications)
                f.PeptideId = f.Peptide.Id;

            await ctx.BulkInsertAsync(peptidesModifications);

            await PeptideSequenceMapper.Map(request.GenomeId, peptides);

            Console.WriteLine("Done");
        }

        public async Task LoadSamples(LoadSamplesRequest request)
        {
            BaseCtx ctx = new();

            XSSFWorkbook book = new(request.FileName);

            List<string> sheetNames = [];
            for (int h = 0; h < book.NumberOfSheets; h++)
            {
                sheetNames.Add(book.GetSheetAt(h).SheetName);
            }

            var sheet = book.GetSheet(ConsoleInput.PickItem(sheetNames));

            var headers = sheet.GetRow(0).ToDictionary(e => e.ColumnIndex, e => e.StringCellValue);

            var peptideIdCol = ConsoleInput.PickItem(headers, "Peptide ID column:");
            var sitesIdCol = ConsoleInput.PickItem(headers, "PhosphoSiteIds column:");

            var samplesColumns = ConsoleInput.PickItems(headers);

            var samples = samplesColumns.ToDictionary
                (c => c,
                 c => new Sample()
                 {
                     TreatmentId = request.TreatmentId,
                     Description = headers[c],
                     Type = SampleType.Phosphoproteome,
                     IntensityThreshold = 0,
                 });

            ctx.Samples.AddRange(samples.Select(m => m.Value));
            await ctx.SaveChangesAsync();

            List<ProteomeValue> values = [];

            for (var f = 1; f <= sheet.LastRowNum; f++)
            {
                var row = sheet.GetRow(f);
                if (row.LastCellNum < headers.Count())
                    continue;

                var peptideIdCell = row.GetCell(peptideIdCol);
                var SitesIdsCell = row.GetCell(sitesIdCol);

                if (peptideIdCell == null
                    || peptideIdCell.CellType == NPOI.SS.UserModel.CellType.Blank
                    || SitesIdsCell == null
                    || SitesIdsCell.CellType == NPOI.SS.UserModel.CellType.Blank)
                    continue;

                string SitiesIds;
                if (SitesIdsCell.CellType == NPOI.SS.UserModel.CellType.Numeric)
                    SitiesIds = SitesIdsCell.NumericCellValue.ToString();
                else
                    SitiesIds = SitesIdsCell.StringCellValue;

                foreach (var PeptideIdData in SitiesIds
                    .Split(";")
                    .Select(s => $"{peptideIdCell.NumericCellValue}.{s}"))
                {
                    if (request.PeptidesIds.TryGetValue(PeptideIdData, out var PeptidoId))
                    {
                        var addingValues = samples
                            .Select(m => new ProteomeValue()
                            {
                                PeptideId = PeptidoId,
                                SampleId = m.Value.Id,
                                Sample = m.Value,
                                Intensity = (decimal)(row.GetCell(m.Key).CellType == NPOI.SS.UserModel.CellType.Blank ?
                                                            0 :
                                                            row.GetCell(m.Key).NumericCellValue)
                            })
                            .ToList();

                        if (addingValues.Any())
                        {
                            values.AddRange(addingValues);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Missing peptide Id: {PeptideIdData}");
                    }
                }
            }

            book.Close();

            await ctx.BulkInsertAsync(values);

            Console.WriteLine("Done");
        }
    }
}
