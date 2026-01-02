using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Araport
{
    public static class LoadTairGoTerms
    {
        public static async Task Program()
        {
            using var AnnotationFile = ConsoleInput.ReadFile("TAIR GO annotation file:");

            var GenomeId = await ConsoleInput.PickGenomeId();

            BaseCtx ctx = new();

            var Features = (await ctx.Features
                .Where(f => f.Sequence.Genome.Id == GenomeId &&
                            !string.IsNullOrWhiteSpace(f.Code))
                .Select(f => new { f.Code, f.Id })
                .ToListAsync())
                .GroupBy(f => f.Code)
                .ToDictionary(g => g.Key ?? "",
                              g => g.First().Id);

            List<FeatureGoTerm> Adding = new();

            while (!AnnotationFile.EndOfStream)
            {
                var Line = await AnnotationFile.ReadLineAsync();

                if (Line.StartsWith("!"))
                    continue;

                var Fields = Line.Split("\t");

                if (!Features.TryGetValue(Fields[2], out var FeatId))
                {
                    Console.WriteLine($"Gene not found: {Fields[2]}.");
                    continue;
                }

                Adding.Add(new FeatureGoTerm()
                {
                    FeatureId = FeatId,
                    GoTermId = Fields[5],
                    Relationship = Fields[3],
                    Source = "TAIR"
                });
            }

            Adding = Adding.Distinct().ToList();

            if (!ConsoleInput.AskBool($"Save {Adding.Count()} relationships?"))
                return;

            await ctx.BulkInsertAsync(Adding);

            Console.WriteLine("Done");
        }
    }
}
