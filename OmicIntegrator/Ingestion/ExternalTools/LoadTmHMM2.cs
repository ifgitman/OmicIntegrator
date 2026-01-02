using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.ExternalTools
{
    public static class LoadTmHMM2
    {
        public const string ProgramName = "TM_HMM2";
        public const string MotifCode = "TMhelix";
        public static async Task Program()
        {
            Console.WriteLine("Load output from https://services.healthtech.dtu.dk/services/TMHMM-2.0/. Use 'Output format' = 'Extensive, no graphics' and save it to a plain text file.");
            using var file = ConsoleInput.ReadFile("TM HMM 2.0 file:");

            FeatureTitleParser titleParser = new();

            BaseCtx ctx = new();

            var MotifId = await ctx.Motifs
                .Where(m => m.Program == ProgramName
                            && m.Code == MotifCode)
                .Select(m => (long?)m.Id)
                .FirstOrDefaultAsync();

            if (!MotifId.HasValue)
            {
                Motif mot = new()
                {
                    Program = ProgramName,
                    Code = MotifCode,
                    Description = "TMhelix"
                };
                ctx.Motifs.Add(mot);
                await ctx.SaveChangesAsync();
                MotifId = mot.Id;
            }

            List<FeatureMotif> addFeatureMotifs = new();

            while (!file.EndOfStream)
            {
                var line = await file.ReadLineAsync();

                if (line.StartsWith("#"))
                    continue;

                var fields = line.Split("\t");

                if (fields.Length >= 4 &&
                    fields[2] == "TMhelix")
                {
                    var positions = fields[3].Split(" ").Where(c => !string.IsNullOrWhiteSpace(c)).Select(p => int.Parse(p)).ToList();

                    addFeatureMotifs.Add(new()
                    {
                        FeatureId = await titleParser.Parse(fields[0]),
                        MotifId = MotifId.Value,
                        Start = positions[0],
                        End = positions[1]
                    });
                }
            }

            await ctx.BulkInsertAsync(addFeatureMotifs);

            Console.WriteLine("Done");
        }
    }
}
