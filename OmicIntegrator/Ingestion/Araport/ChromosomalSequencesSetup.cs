using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Wordprocessing;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Araport
{
    public static class ChromosomalSequencesSetup
    {
        public static async Task Program()
        {
            var GenomeId = await ConsoleInput.PickGenomeId();

            BaseCtx ctx = new();

            var chrs = await ctx.Sequences.Where(s => s.GenomeId == GenomeId).ToListAsync();

            foreach (var chr in chrs)
            {
                using var fasta = ConsoleInput.ReadFile($"FASTA file path with sequence for chromosome {chr.Name}.");
                chr.FilePath = ConsoleInput.LastFileName;

                bool started = false;

                var Line = await fasta.ReadLineAsync();

                while (Line != null)
                {
                    if (Line.StartsWith(">"))
                    {
                        if (started) break;

                        chr.Length = 0;
                    }
                    else
                    {
                        if (!started)
                        {
                            started = true;

                            chr.Start = fasta.GetPosition() - Line.Length - 1;
                            chr.Width = Line.Length;
                        }

                        chr.Length += Line.Length;
                    }

                    Line = await fasta.ReadLineAsync();
                }

                fasta.Close();

                await ctx.SaveChangesAsync();

                Console.WriteLine("Done");
            }
        }
    }
}
