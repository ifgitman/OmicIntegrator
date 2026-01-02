using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;

namespace OmicIntegrator.Functions
{
    public static class GoFindTerms
    {
        public static async Task<IEnumerable<string>> FindChildren(IEnumerable<string> GoTermIds, List<string> PreviouslyFound = null)
        {
            if (!GoTermIds.Any())
                return Array.Empty<string>();

            BaseCtx ctx = new();

            if (PreviouslyFound == null)
                PreviouslyFound = new();

            var Children = (await ctx.GoTermsRelactinships
                .Where(r => r.Relationship == "is_a"
                            && GoTermIds.Contains(r.ReferredId))
                .Select(r => r.ReferenceId)
                .ToListAsync())
                .Where(h => !PreviouslyFound.Contains(h))
                .Distinct()
                .ToList();

            PreviouslyFound = PreviouslyFound.Concat(Children).Distinct().ToList();

            var rtr = new List<string>(Children);

            rtr.AddRange(await FindChildren(Children, PreviouslyFound));

            return rtr.Distinct().ToList();
        }
        public static Task<IEnumerable<string>> FindChildren(string GoTermId)
        {
            return FindChildren([GoTermId]);
        }
        public static async Task<IEnumerable<string>> FindParents(IEnumerable<string> GoTermIds, List<string> PreviouslyFound = null)
        {
            if (!GoTermIds.Any())
                return Array.Empty<string>();

            BaseCtx ctx = new();

            if (PreviouslyFound == null)
                PreviouslyFound = new();

            var Parents = (await ctx.GoTermsRelactinships
                .Where(r => r.Relationship == "is_a"
                            && GoTermIds.Contains(r.ReferenceId))
                .Select(r => r.ReferredId)
                .ToListAsync())
                .Where(h => !PreviouslyFound.Contains(h))
                .Distinct()
                .ToList();

            PreviouslyFound = PreviouslyFound.Concat(Parents).Distinct().ToList();

            var rtr = new List<string>(Parents);

            rtr.AddRange(await FindParents(Parents, PreviouslyFound));

            return rtr.Distinct().ToList();
        }
    }
}
