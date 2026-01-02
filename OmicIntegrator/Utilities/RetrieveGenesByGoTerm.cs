using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Operation.Distance;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;
using Org.BouncyCastle.Pqc.Crypto.Utilities;

namespace OmicIntegrator.Utilities
{
    public enum GenesOutputFormat
    {
        OmicIntegratorIds,
        GeneCodes,
        Excel
    }
    public static class RetrieveGenesByGoTerm
    {
        public static async Task Program()
        {
            var GenomeId = await ConsoleInput.PickGenomeId();
            var GoTermId = ConsoleInput.AskString("GoTerm ID:");

            var OutputType = ConsoleInput.PickEnum<GenesOutputFormat>();

            var OutputFile = ConsoleInput.AskFileName($"Output file name (*.{OutputType switch { GenesOutputFormat.OmicIntegratorIds => "ids", GenesOutputFormat.GeneCodes => "cod", GenesOutputFormat.Excel => "xlsx" }}):", false);

            var terms = (await Functions.GoFindTerms.FindChildren(GoTermId)).ToList();

            terms.Add(GoTermId);

            BaseCtx ctx = new();

            var Features = (await ctx.FeaturesGoTerms
                .Where(t => terms.Contains(t.GoTermId)
                            && t.Feature.Sequence.GenomeId == GenomeId)
                .Select(t => new
                {
                    t.FeatureId,
                    t.Feature.Code,
                    t.Feature.Alias,
                    t.Feature.Description,
                    t.Feature.ShortName,
                })
                .ToListAsync()).
                Distinct()
                .ToList();

            Console.WriteLine($"Found features: {Features.Count}");

            switch (OutputType)
            {
                case GenesOutputFormat.OmicIntegratorIds:
                case GenesOutputFormat.GeneCodes:
                    {
                        using var output = File.CreateText(OutputFile);

                        foreach (var f in Features)
                            await output.WriteLineAsync(OutputType == GenesOutputFormat.OmicIntegratorIds ? f.FeatureId.ToString() : f.Code);

                        output.Close();
                    }
                    break;
                case GenesOutputFormat.Excel:
                    {
                        ExcelWriter excel = new();

                        excel.Write(OutputFile, Features);
                    }
                    break;
            }

            Console.WriteLine("Done"); 
        }
    }
}
