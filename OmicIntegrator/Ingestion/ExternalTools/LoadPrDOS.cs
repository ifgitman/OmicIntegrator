using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.ExternalTools
{
    public static class LoadPrDOS
    {
        public const string ProgramName = "PrDOS";
        public const string MotifCode = "PrDOS";

        public static async Task Program()
        {
            Console.WriteLine("Load output from https://prdos.hgc.jp/cgi-bin/top.cgi. Paste results sent by e-mail into a plain text file. Results for multiple proteins can be just concatenated.");
            using var prDosFile = ConsoleInput.ReadFile("PrDOS file:");

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
                    Description = "Disordered region"
                };
                ctx.Motifs.Add(mot);
                await ctx.SaveChangesAsync();
                MotifId = mot.Id;
            }

            const string FeatureLineBegining = "Results of disorderd region prediction for \"";
            const string AminoacidsHeader = " No  AA Pred Probability";

            long currFeatureId = 0;
            bool InAminoacids = false;
            int currResidue = 0;
            FeatureMotif currRegion = null;

            List<FeatureMotif> addRegions = new();

            while (!prDosFile.EndOfStream)
            {
                var line = await prDosFile.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith(FeatureLineBegining))
                {
                    currFeatureId = await titleParser.Parse(line.Substring(FeatureLineBegining.Length, line.Length - FeatureLineBegining.Length - 1));
                    InAminoacids = false;
                }
                else if (line == AminoacidsHeader)
                {
                    InAminoacids = true;
                    currResidue = 0;
                }
                else if (InAminoacids)
                {
                    if (System.Char.IsDigit(line.TrimStart()[0]))
                    {
                        currResidue++;

                        List<string> fields = new();

                        string currField = "";
                        for (int i = 0; i < line.Length; i++)
                        {
                            if (line[i] == ' ')
                            {
                                if (!string.IsNullOrWhiteSpace(currField))
                                {
                                    fields.Add(currField);
                                    currField = "";
                                }
                            }
                            else
                            {
                                currField += line[i];
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(currField))
                            fields.Add(currField);

                        if (fields.Count == 4 && fields[2] == "*")
                        {
                            if (currRegion == null)
                            {
                                currRegion = new FeatureMotif()
                                {
                                    FeatureId = currFeatureId,
                                    MotifId = MotifId.Value,
                                    Start = currResidue,
                                    PValue = 0
                                };
                            }
                        }
                        else if (fields.Count == 3 && currRegion != null)
                        {
                            currRegion.End = currResidue - 1;
                            addRegions.Add(currRegion);
                            currRegion = null;
                        }
                    }
                    else if (currRegion != null)
                    {
                        currRegion.End = currResidue;
                        addRegions.Add(currRegion);
                        currRegion = null;
                    }
                }
            }

            Console.WriteLine($"{addRegions.Count} regions in {addRegions.Select(r => r.FeatureId).Distinct().Count()} different genes.");

            await ctx.BulkInsertAsync(addRegions);

            prDosFile.Close();

            Console.WriteLine("Done");
        }
    }
}
