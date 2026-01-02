using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NPOI.POIFS.Crypt.Dsig.Facets;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Standardization;
using OmicIntegrator.Functions;
using OmicIntegrator.Helpers;
using Org.BouncyCastle.Bcpg.Sig;

namespace OmicIntegrator.Standardization
{
    public static class PerformStandardization
    {
        public static async Task Program()
        {
            var GenomeId = await ConsoleInput.PickGenomeId();

            var OutputFile = ConsoleInput.AskFileName("Output file (*.xlsx):", false);

            BaseCtx ctx = new();

            var allTreatments = await ctx.Treatments.Select(t => new
            {
                t.DatasetId,
                t.Id,
                t.Description
            }).ToListAsync();

            List<int> TreatmentIds = [];
            bool Finished = false;
            do
            {
                var DatasetId = await ConsoleInput.PickTableIdIntOptional<Dataset>("Select dataset:", d => d.Id, d => d.Description, d => d.GenomeId == GenomeId);

                if (!DatasetId.HasValue)
                    break;

                var trts = allTreatments.Where(t => t.DatasetId == DatasetId).ToList();

                if (trts.Count == 1)
                {
                    TreatmentIds.Add(trts.Single().Id);
                }
                else
                {
                    TreatmentIds.Add(ConsoleInput.PickItem(trts.ToDictionary(t => t.Description, t => t) , "Select treatment:")
                                        .Id);
                }
            }
            while (!Finished);

            bool OnlyRepresentativeFeatures = ConsoleInput.AskBool("Only representative features?");

            bool PeptidesTwoOrMoreAboveThreshold = ConsoleInput.AskBool("Only peptides above threshold in two or more samples? (to reproduce Gitman et al. 2025)");

            IEnumerable<long> ExcludedFeatures = [];

            if (ConsoleInput.AskBool("Exclude features?"))
            {
                ExcludedFeatures = await ConsoleInput.AskFeatureIds("Excluded features:", GenomeId);
            }

            var qFeatures = ctx.Features
                .Where(f => f.Type == "gene");

            if (OnlyRepresentativeFeatures)
            {
                qFeatures = qFeatures.Where(v => v.Features.Any(f => f.IsGeneRepresentative));
            }

            if (ExcludedFeatures.Any())
            {
                qFeatures = qFeatures.Where(v => !ExcludedFeatures.Contains(v.Id));
            }

            var FeaturesIds = await qFeatures.Select(f => f.Id).ToListAsync();

            var sequences = await FeatureExtractor.ProteinSequence(FeaturesIds);

            var Treatments = await ctx.Treatments
                .Where(t => TreatmentIds.Contains(t.Id))
                .Select(t => new
                {
                    t.DatasetId,
                    TreatmentId = t.Id,
                    TreatmentDescription = $"{t.Dataset.Description} - {t.Description}",
                })
                .ToListAsync();

            var DatasetsIds = Treatments.Select(t => t.DatasetId).Distinct().ToList();

            #region "transcriptomic data"
            var rnaValues = await ctx.RnaValues
                .Where(v => TreatmentIds.Contains(v.Sample.TreatmentId))
                .Select(v => new
                {
                    v.Sample.TreatmentId,
                    v.FeatureId,
                    v.Tpm
                })
                .ToListAsync();

            var averagedTpms = rnaValues
                .GroupBy(v => new { v.TreatmentId, v.FeatureId })
                .Select(g => new
                {
                    g.Key.TreatmentId,
                    SampleType = SampleType.Transcriptome.ToString(),
                    g.Key.FeatureId,
                    AveragedValue = g.Average(v => v.Tpm),
                    ExclusivePeptides = default(int?)
                })
                .ToList();
            #endregion

            #region Proteomic and phosphoproteomic data
            var Peptides = await ctx.ProteomesPeptides
                .Where(p => DatasetsIds.Contains(p.DatasetId))
                .Select(p => new 
                {
                    p.Id, 
                    p.DatasetId,
                    PhosphoResidue = p.Modifications
                                        .Where(m => m.ModificationType == "Phosphorylation" && m.ResiduePosition.HasValue)
                                        .Select(m => m.ResiduePosition)
                                        .SingleOrDefault()

                })
                .ToListAsync();

            var PeptidesFeatures = await ctx.ProteomesPeptidesFeatures
                .Where(pf => DatasetsIds.Contains(pf.Peptide.DatasetId))
                .Select(pf => new
                {
                    pf.PeptideId,
                    pf.FeatureId,
                    pf.Position
                })
                .ToListAsync();

            var Values = await ctx.ProteomesValues
                .Where(v => TreatmentIds.Contains(v.Sample.TreatmentId))
                .Select(v => new
                {
                    v.Sample.TreatmentId,
                    v.SampleId,
                    SampleType = v.Sample.Type,
                    v.PeptideId,
                    v.Intensity,
                    AboveThreshold = v.Sample.IntensityThreshold.HasValue ? v.Intensity > v.Sample.IntensityThreshold.Value : default(bool?),
                })
                .ToListAsync();

            var PeptidesTreatmentAverage = Values
                .GroupBy(v => new { v.TreatmentId, v.SampleType, v.PeptideId })
                .Where(g => !PeptidesTwoOrMoreAboveThreshold
                            || g.Any(v => !v.AboveThreshold.HasValue)
                            || g.Count(v => v.AboveThreshold.Value) >= 2)
                .Select(g => new
                {
                    g.Key.TreatmentId,
                    g.Key.SampleType,
                    g.Key.PeptideId,
                    MeanIntensity = g.Average(v => v.Intensity),
                })
                .ToList();

            var averagedPeptidesSum = PeptidesTreatmentAverage
                .GroupJoin(PeptidesFeatures,
                           v => v.PeptideId,
                           f => f.PeptideId,
                           (v, fs) => fs.Select(f => new
                           {
                               v.TreatmentId,
                               SampleType = v.SampleType.ToString(),
                               v.PeptideId,
                               v.MeanIntensity,
                               f.FeatureId,
                               IsExclusive = fs.Count() == 1
                           }))
                .SelectMany(fs => fs)
                .GroupBy(f => new { f.TreatmentId, f.SampleType, f.FeatureId })
                .Select(g => new
                {
                    g.Key.TreatmentId,
                    g.Key.SampleType,
                    g.Key.FeatureId,
                    AveragedValue = g.Sum(v => v.MeanIntensity),
                    ExclusivePeptides = (int?)g.Count(v => v.IsExclusive)
                })
                .ToList();
            #endregion

            var output = averagedTpms.Concat(averagedPeptidesSum).Where(v => FeaturesIds.Contains(v.FeatureId)).ToList();

            ExcelWriter excel = new();

            excel.Write(OutputFile, output);

            var ROutputPath = Path.Combine(Path.GetDirectoryName(OutputFile),
                                           $"{Path.GetFileNameWithoutExtension(OutputFile)}_standardized.xlsx");

            Console.WriteLine($"Run ./R_scripts/TreatmentsStandardization.R (update Excel file location)\r\nOmicIntegrator_standardization_file <- \"{OutputFile.Replace("\\", "/")}\"\r\n");

            do
            {
                Console.WriteLine($"Press ENTER when {ROutputPath} file is ready.");

                Console.ReadLine();
            }
            while (!File.Exists(ROutputPath));

            TreatmentsStandardization standardization = new()
            {
                GenomeId = GenomeId,
                Title = Path.GetFileNameWithoutExtension(OutputFile)
            };

            ctx.TreatmentsStandardizations.Add(standardization);

            await ctx.SaveChangesAsync();

            #region Load R output from Excel file
            Console.WriteLine("Loading R output...");

            List<TreatmentValue> addingSValues = [];

            using ExcelReader r_treatments = new(ROutputPath, true, "Treatments");
            while (r_treatments.ReadNextRow(out var row))
            {
                addingSValues.Add(new()
                {
                    StandardizationId = standardization.Id,
                    TreatmentId = row.GetCellByHeader<int>("TreatmentId"),
                    FeatureId = row.GetCellByHeader<long>("FeatureId"),
                    SValue = row.GetCellByHeader<decimal?>("S-value"),
                    WithExclusivity = row.GetCellByHeader<bool?>("WithExclusivity"),
                });
            }

            List<SampleTypeValue> addingSaValues = [];

            using ExcelReader r_sampleTypes = new(ROutputPath, true, "SampleTypes");

            while (r_sampleTypes.ReadNextRow(out var row))
            {
                addingSaValues.Add(new()
                {
                    StandardizationId = standardization.Id,
                    SampleType = Enum.Parse<SampleType>(row.GetCellByHeader<string>("SampleType")),
                    FeatureId = row.GetCellByHeader<long>("FeatureId"),
                    SAValue = row.GetCellByHeader<decimal?>("SA-value"),
                    SValue_SD = row.GetCellByHeader<decimal?>("S-value_sd"),
                    Presence = Enum.Parse<PresenceValue>(row.GetCellByHeader<string>("Presence")),
                    HighAbundance = row.GetCellByHeader<bool>("HighAbundance")
                });
            }

            await ctx.BulkInsertAsync(addingSValues);
            await ctx.BulkInsertAsync(addingSaValues);
            #endregion

            #region Phosphosites
            Console.WriteLine("Summarizing phosphosites...");

            var addingPhosphoSites = PeptidesTreatmentAverage
                .Where(v => v.SampleType == SampleType.Phosphoproteome)
                .Join(Peptides.Where(p => p.PhosphoResidue.HasValue),
                      a => a.PeptideId,
                      p => p.Id,
                      (a, p) => new
                      {
                          a.TreatmentId,
                          a.PeptideId,
                          p.PhosphoResidue
                      })
                .GroupJoin(PeptidesFeatures.Where(pf => pf.Position.HasValue),
                      p => p.PeptideId,
                      pf => pf.PeptideId,
                      (p, pfs) => pfs.Select(f => new
                      {
                          p.TreatmentId,
                          p.PeptideId,
                          f.FeatureId,
                          ResiduePosition = f.Position.Value + p.PhosphoResidue.Value,
                          IsExclusive = pfs.Count() == 1
                      }))
                .SelectMany(f => f)
                .GroupBy(f => new { f.TreatmentId, f.FeatureId, f.ResiduePosition })
                .Select(g => new SummarizedPhosphosite()
                {
                    StandardizationId = standardization.Id,
                    TreatmentId = g.Key.TreatmentId,
                    FeatureId = g.Key.FeatureId,
                    ResiduePosition = g.Key.ResiduePosition + 1,
                    Residue = sequences[g.Key.FeatureId][g.Key.ResiduePosition],
                    ExclusivePeptides = g.Count(p => p.IsExclusive),
                })
                .ToList();

            await ctx.BulkInsertAsync(addingPhosphoSites);
            #endregion

            Console.WriteLine("Done");
        }
    }
}