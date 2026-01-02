using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets.Proteomes;
using OmicIntegrator.Functions;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes
{
    public static class PeptideSequenceMapper
    {
        public static async Task Map(int GenomeId, IEnumerable<Peptide> Peptides)
        {
            BaseCtx ctx = new();

            var qFeatures = ctx.Features
                .Where(f => f.Sequence.GenomeId == GenomeId
                            && f.Type == "gene");

            if (ConsoleInput.AskBool("Exclude genes?"))
            {
                var ExcludedFeatureIds = await ConsoleInput.AskFeatureIds("Excluded features file:", GenomeId);

                qFeatures = qFeatures.Where(f => !ExcludedFeatureIds.Contains(f.Id));
            }

            var FeatureIds = await qFeatures
                .Select(f => f.Id)
                .ToListAsync();

            var sequences = await FeatureExtractor.ProteinSequence(FeatureIds);

            List<PeptideFeature> mappings = new();

            long iter = 0;

            foreach (var pep in Peptides)
            {
                foreach (var seq in pep.Sequence.Split(";"))
                {
                    foreach (var m in sequences.Where(s => s.Value.Contains(seq)))
                    {
                        int lastPos = 0;

                        do
                        {
                            var pos = m.Value.IndexOf(seq, lastPos);

                            if (pos != -1)
                            {
                                mappings.Add(new() 
                                {
                                    PeptideId = pep.Id, FeatureId = m.Key, Position = pos
                                });
                                lastPos = pos + 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                        while (lastPos <= m.Value.Length);
                    }
                }
                
                if (iter++ % 1000 == 0)
                {
                    Console.WriteLine($"Iteration {iter}.");
                }
            }

            if (mappings.Any())
            {
                var distinctMaps = mappings.DistinctBy(m => new { m.PeptideId, m.FeatureId, m.Position }).ToList();

                Console.WriteLine($"Mapping {mappings.Select(m => m.PeptideId).Distinct().Count()} peptides to {mappings.Select(m => m.FeatureId).Distinct().Count()} proteins ({distinctMaps.Count()} mappings).");

                await ctx.BulkInsertAsync(distinctMaps);
            }

            Console.WriteLine("Done");
        }
    }
    public record MappedPeptide(long FeatureId, int Position);
}
