using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using System.Text;

namespace OmicIntegrator.Functions
{
    public static class FeatureExtractor
    {
        static Dictionary<string, char> GeneticCode = new()
        {
            { "AAA", 'K' }, { "AAC", 'N' }, { "AAG", 'K' }, { "AAT", 'N' },
            { "ACA", 'T' }, { "ACC", 'T' }, { "ACG", 'T' }, { "ACT", 'T' },
            { "AGA", 'R' }, { "AGC", 'S' }, { "AGG", 'R' }, { "AGT", 'S' },
            { "ATA", 'I' }, { "ATC", 'I' }, { "ATG", 'M' }, { "ATT", 'I' },
            { "CAA", 'Q' }, { "CAC", 'H' }, { "CAG", 'Q' }, { "CAT", 'H' },
            { "CCA", 'P' }, { "CCC", 'P' }, { "CCG", 'P' }, { "CCT", 'P' },
            { "CGA", 'R' }, { "CGC", 'R' }, { "CGG", 'R' }, { "CGT", 'R' },
            { "CTA", 'L' }, { "CTC", 'L' }, { "CTG", 'L' }, { "CTT", 'L' },
            { "GAA", 'E' }, { "GAC", 'D' }, { "GAG", 'E' }, { "GAT", 'D' },
            { "GCA", 'A' }, { "GCC", 'A' }, { "GCG", 'A' }, { "GCT", 'A' },
            { "GGA", 'G' }, { "GGC", 'G' }, { "GGG", 'G' }, { "GGT", 'G' },
            { "GTA", 'V' }, { "GTC", 'V' }, { "GTG", 'V' }, { "GTT", 'V' },
            { "TAA", '*' }, { "TAC", 'Y' }, { "TAG", '*' }, { "TAT", 'Y' },
            { "TCA", 'S' }, { "TCC", 'S' }, { "TCG", 'S' }, { "TCT", 'S' },
            { "TGA", '*' }, { "TGC", 'C' }, { "TGG", 'W' }, { "TGT", 'C' },
            { "TTA", 'L' }, { "TTC", 'F' }, { "TTG", 'L' }, { "TTT", 'F' }
        };

        public static async Task<Dictionary<long, string>> ProteinSequence(IEnumerable<long> FeatureIds)
        {
            var Cdss = await SubFeaturesSecuence(FeatureIds, "CDS");

            StringBuilder SeqAa = new();

            Dictionary<long, string> Devolver = new();
            foreach (var f in Cdss)
            {
                for (int i = 0; i < f.Value.Length - 3; i += 3)
                {
                    var Triplete = f.Value.Substring(i, 3);

                    if (!GeneticCode.TryGetValue(Triplete, out var Aa))
                        continue;

                    if (Aa != '*')
                        SeqAa.Append(Aa);
                    else
                        break;
                }

                Devolver.Add(f.Key, SeqAa.ToString());

                SeqAa.Clear();
            }

            return Devolver;
        }
        public static async Task<Dictionary<long, string>> Promoters(IEnumerable<long> FeatureIds, int UpstreamTss, int DownstreamTss)
        {
            BaseCtx ctx = new();

            var FeatFragments = await ctx.Features
                .Where(f => FeatureIds.Contains(f.Id))
                .Select(f => new
                {
                    f.Id,
                    Fragments = (IEnumerable<GenomeExtractor.Fragment>)
                                 new[] { (f.Strand ?? '+') == '+' ?
                                         new GenomeExtractor.Fragment(f.Start- UpstreamTss, f.Start + DownstreamTss - 1) :
                                         new GenomeExtractor.Fragment(f.End - DownstreamTss + 1, f.End + UpstreamTss) }
                })
                .ToListAsync();

            return await GenomeExtractor.Extract(FeatFragments.ToDictionary(f => f.Id, f => f.Fragments));
        }
        public static async Task<Dictionary<long, string>> SubFeaturesSecuence(IEnumerable<long> FeatureIds,
                                                                                string SubFeatType)
        {
            var FeatFragments = await FindSubFeatures(new BaseCtx(), FeatureIds, SubFeatType);

            return await GenomeExtractor
                .Extract(FeatFragments.ToDictionary(f => f.Key,
                                                     f => (IEnumerable<GenomeExtractor.Fragment>)f.Value.Values));

        }
        private static async Task<Dictionary<long, Dictionary<long, GenomeExtractor.Fragment>>> FindSubFeatures(BaseCtx ctx, IEnumerable<long> FeatureIds, string Type)
        {
            if (!FeatureIds.Any())
                return new Dictionary<long, Dictionary<long, GenomeExtractor.Fragment>>();

            var SubFeatures = await ctx.FeaturesParents
                .Where(p => FeatureIds.Contains(p.ParentId)
                            && (p.Feature.Type != "mRNA"
                                || p.Feature.IsGeneRepresentative == true))
                .OrderBy(f => f.ParentId)
                    .ThenBy(f => f.Feature.Start)
                .Select(p => new
                {
                    p.Feature.Type,
                    p.ParentId,
                    p.FeatureId,
                    Fragment = new GenomeExtractor.Fragment(p.Feature.Start, p.Feature.End)
                })
                .ToListAsync();

            var Parents = SubFeatures
                .GroupBy(s => s.ParentId)
                .Select(g => new
                {
                    ParentId = g.Key,
                    SubFeats = g,
                    SubFeatsType = g.Where(f => f.Type == Type)
                                    .ToDictionary(f => f.FeatureId, f => f.Fragment)
                })
                .ToList();

            var Missing = Parents
                .Where(g => !g.SubFeatsType.Any())
                .SelectMany(g => g.SubFeats.Select(f => new
                {
                    f.FeatureId,
                    g.ParentId
                }).ToList())
                .ToList();

            var SubSubs = await FindSubFeatures(ctx, Missing.Select(f => f.FeatureId).ToList(), Type);

            var Found = Parents
                .Where(g => g.SubFeatsType.Any())
                .Select(g => (g.ParentId, g.SubFeatsType))
                .ToList();

            var FoundSub = Missing.Join(SubSubs,
                                             f => f.FeatureId,
                                             s => s.Key,
                                             (f, s) => (f.ParentId, s.Value))
                                        .ToList();

            List<(long, Dictionary<long, GenomeExtractor.Fragment>)> rtr = new();

            if (Found.Any())
                rtr.AddRange(Found);

            if (FoundSub.Any())
                rtr.AddRange(FoundSub);

            return rtr.ToDictionary(i => i.Item1, i => i.Item2);
        }
    }
}
