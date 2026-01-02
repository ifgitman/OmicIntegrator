using LinqKit;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Standardization;
using OmicIntegrator.Helpers;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmicIntegrator.Utilities
{
    public static class DownloadStandardizedData
    {
        public static async Task Program()
        {
            var StandardizationId = await ConsoleInput.PickTableIdInt<TreatmentsStandardization>("Select standardization:", s => s.Id, s => s.Title);

            var outputFile = ConsoleInput.AskFileName("Output file path (*.xlsx):", false);

            BaseCtx ctx = new();

            var Standardization = await ctx.TreatmentsStandardizations.SingleAsync(s => s.Id == StandardizationId);

            Dictionary<string, IEnumerable<long>> GeneSets = [];

            while (ConsoleInput.AskBool("Add Gene sets?"))
            {
                var ids = await ConsoleInput.AskFeatureIds(GenomeId: Standardization.GenomeId);

                GeneSets.Add(Path.GetFileNameWithoutExtension(ConsoleInput.LastFileName), ids);
            }

            var motifsPrograms = await ctx.Motifs.Where(m => !string.IsNullOrEmpty(m.Program)).Select(m => m.Program).Distinct().ToListAsync();

            List<Motif> Motifs = [];
            List<FeatureMotif> motifMatches = [];

            while (true)
            {
                var motifsProgram = ConsoleInput.PickItemOptional(motifsPrograms, "Select protein motifs program (e. g. Wang et al. 2013):");

                if (string.IsNullOrWhiteSpace(motifsProgram))
                    break;

                Motifs.AddRange(await ctx.Motifs
                                    .Where(m => m.Program == motifsProgram
                                                && !string.IsNullOrWhiteSpace(m.Group))
                                    .ToListAsync());

                motifMatches.AddRange(await ctx.FeaturesMotifs
                                        .Where(m => m.Motif.Program == motifsProgram 
                                                    && !string.IsNullOrEmpty(m.Motif.Group))
                                        .ToListAsync());
            }

            var Features = await ctx.Features
                .Where(f => f.Sequence.GenomeId == Standardization.GenomeId
                            && f.Type == "gene"
                            && f.Features.Any(s => s.IsGeneRepresentative))
                .Select(f => new
                {
                    f.Id,
                    f.Code,
                    Alias = !string.IsNullOrWhiteSpace(f.Alias) ? f.Alias : f.ShortName,
                    f.Description
                }).ToListAsync();

            var SA_values = await ctx.SA_SampleTypeValues
                .Where(v => v.StandardizationId == StandardizationId)
                .ToListAsync();

            var summPhosphoSites = await ctx.SummarizedPhosphosites
                .Where(p => p.StandardizationId == StandardizationId)
                .Select(s => new
                {
                    s.FeatureId,
                    s.ResiduePosition,
                    Description = $"{s.Residue}{s.ResiduePosition}",
                    s.TreatmentId,
                })
                .ToListAsync();

            var phosphoProteomesIds = summPhosphoSites.Select(s => s.TreatmentId).Distinct().ToList();

            var phosphoproteomes = await ctx.Treatments
                .Where(t => phosphoProteomesIds.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    Description = $"{t.Dataset.Description} - {t.Description}"
                })
                .ToListAsync();

            var phosphoSites = summPhosphoSites
                .GroupBy(s => new { s.FeatureId, s.ResiduePosition })
                .GroupJoin(motifMatches.Join(Motifs, 
                                             m => m.MotifId, 
                                             m => m.Id, 
                                             (mm, m) => new
                                             {
                                                 mm.FeatureId,
                                                 m.Program,
                                                 m.Group,
                                                 ResiduePosition = mm.Start,
                                             }).Distinct(),
                           ss => ss.Key,
                           m => new {m.FeatureId, m.ResiduePosition},
                           (ss, mm) => new
                           {
                               ss.Key.FeatureId,
                               ss.Key.ResiduePosition,
                               ss.First().Description,
                               Phosphoproteomes = phosphoproteomes.Where(t => ss.Any(s => s.TreatmentId == t.Id)).ToList(),
                               IsShared = ss.DistinctBy(s => s.TreatmentId).Count() == phosphoproteomes.Count,
                               Motifs = mm.Select(m => m.Group).ToList()
                           }).ToList();

            var MotifsGroups = Motifs.DistinctBy(m => new { m.Program, m.Group }).Select(m => m.Group).ToList();

            var output = Features
                .GroupJoin(phosphoSites,
                           f => f.Id,
                           s => s.FeatureId,
                           (f, ss) => new
                           {
                               Feature = f,
                               Sites = ss.OrderBy(s => s.ResiduePosition)
                           })
                .GroupJoin(SA_values.Where(v => v.SampleType == Data.Datasets.SampleType.Transcriptome),
                           f => f.Feature.Id,
                           v => v.FeatureId,
                           (f, ts) => new
                           {
                               f.Feature,
                               f.Sites,
                               Transcript = ts.SingleOrDefault()?.Presence ?? PresenceValue.Uncertain,
                               SA_TPM = ts.SingleOrDefault()?.SAValue ?? 0,
                           })
                .GroupJoin(SA_values.Where(v => v.SampleType == Data.Datasets.SampleType.Proteome),
                           f => f.Feature.Id,
                           v => v.FeatureId,
                           (f, ts) => new
                           {
                               f.Feature,
                               f.Sites,
                               f.Transcript,
                               f.SA_TPM,
                               Protein = ts.SingleOrDefault()?.Presence ?? PresenceValue.Absent,
                               SA_prot = ts.SingleOrDefault()?.SAValue ?? 0,
                           })
                .Select(f => new
                {
                    OmicIntegratorId = f.Feature.Id,
                    f.Feature.Code,
                    f.Feature.Alias,
                    f.Feature.Description,
                    Protein = f.Protein.ToString(),
                    Transcript = f.Transcript.ToString(),
                    f.SA_TPM,
                    f.SA_prot,
                    NumPhosphosites = f.Sites.Count(),
                    AllPhosphoproteomes = f.Sites.SelectMany(s => s.Phosphoproteomes.Select(t => t.Id)).Distinct().Count() == phosphoproteomes.Count,
                    AddCols = GeneSets.Select(s => new
                    {
                        s.Key,
                        Value = (object)s.Value.Contains(f.Feature.Id)
                    })
                    .Concat([new
                    {
                        Key = "All phosphoproteomes",
                        Value = (object)string.Join(", ",
                                                    f.Sites.Where(s => s.IsShared)
                                                           .Select(s => s.Description))
                    }])
                    .Concat(phosphoproteomes.Select(p => new
                    {
                        Key = p.Description,
                        Value = (object)string.Join(", ",
                                                    f.Sites.Where(s => s.Phosphoproteomes.Any(t => t.Id == p.Id) 
                                                                       && !s.IsShared)
                                                           .Select(s => s.Description))
                    }))
                    .Concat(MotifsGroups.Select(m => new
                    {
                        Key = m,
                        Value = (object)string.Join(", ",
                                                    f.Sites.Where(s => s.Motifs.Any(sm => sm == m))
                                                           .Select(s => s.Description))
                    }))
                    .Concat(MotifsGroups.Select(m => new
                    {
                        Key = $"num_{m}",
                        Value = (object)f.Sites.Where(s => s.Motifs.Any(sm => sm == m)).Count()
                    })).ToDictionary(a => a.Key, a => a.Value)
                });

            ExcelWriter excel = new();

            excel.Write(outputFile, output);

            Console.WriteLine("Done");
        }
    }
}