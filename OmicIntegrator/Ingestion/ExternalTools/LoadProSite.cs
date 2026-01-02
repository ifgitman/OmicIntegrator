using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;
using OmicIntegrator.Helpers.Enums;
using Org.BouncyCastle.Asn1.Crmf;

namespace OmicIntegrator.Ingestion.ExternalTools
{
    public static class LoadProSite
    {
        public const string ProgramName = "ProSite";
        public static async Task Program()
        {
            Console.WriteLine("Load output from https://prosite.expasy.org/scanprosite/. Use 'Output format' = 'Table' and save it to a plain text file.");
            using var ProSiteFile = ConsoleInput.ReadFile("ProSite file:");

            FeatureTitleParser titleParser = new();

            List<ProSiteLine> lines = [];

            do
            {
                var line = await ProSiteFile.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = line.Split("\t");

                var motifFields = fields[3].Split("|");

                lines.Add(new ProSiteLine()
                {
                    FeatureId = await titleParser.Parse(fields[0]),
                    Start = int.Parse(fields[1]),
                    End = int.Parse(fields[2]),
                    MotifCode = motifFields[0],
                    MotifDescription = motifFields.Length > 1 ? motifFields[1] : ""
                });
            }
            while (!ProSiteFile.EndOfStream);

            BaseCtx ctx = new();

            var ProSiteMotifs = await ctx.Motifs
                .Where(m => m.Program == ProgramName)
                .ToListAsync();

            var motifsInFile = lines
                .GroupBy(m => m.MotifCode)
                .Select(g => new { Code = g.Key, Description = g.First().MotifDescription })
                .ToList();

            var addMotifs = motifsInFile
                .Where(a => !ProSiteMotifs.Any(p => p.Code == a.Code))
                .Select(a => new Motif()
                {
                    Program = "ProSite",
                    Code = a.Code,
                    Description = a.Description
                })
                .ToList();

            ctx.Motifs.AddRange(addMotifs);
            await ctx.SaveChangesAsync();

            ProSiteMotifs.AddRange(addMotifs);

            var addFeatureMotifs = lines.Join(ProSiteMotifs,
                                                     l => l.MotifCode,
                                                     m => m.Code,
                                                     (line, m) => new FeatureMotif()
                                                     {
                                                         FeatureId = line.FeatureId,
                                                         MotifId = m.Id,
                                                         Start = line.Start,
                                                         End = line.End
                                                     })
                .ToList();

            await ctx.BulkInsertAsync(addFeatureMotifs);

            Console.WriteLine("Done");
        }

        class ProSiteLine
        {
            public long FeatureId { get; set; }
            public int Start { get; set; }
            public int End { get; set; }
            public string MotifCode { get; set; }
            public string MotifDescription { get; set; }
        }
    }
}
