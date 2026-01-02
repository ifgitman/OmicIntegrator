using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Functions;
using OmicIntegrator.Helpers;
using System.Text.RegularExpressions;

namespace OmicIntegrator.Utilities
{
    public static class ProteinMotifFinder
    {
        public static async Task Program()
        {
            var GenomeId = await ConsoleInput.PickGenomeId();

            BaseCtx ctx = new();

            var Programs = await ctx.Motifs.Where(m => !string.IsNullOrWhiteSpace(m.Program)).Select(m => m.Program).Distinct().ToListAsync();

            var program = ConsoleInput.PickItemOptional(Programs, EmptyPrompt: "(load new motifs)");

            List<Motif> Motifs;

            if (!string.IsNullOrWhiteSpace(program))
            {
                Motifs = await ctx.Motifs
                    .Where(m => m.Program == program)
                    .ToListAsync();
            }
            else
            {
                program = ConsoleInput.AskString("Program name (e.g. Wang et al. 2013):");

                Motifs = [];

                var motifsFile = ConsoleInput.AskFileName("Motifs Excel file (*.xlsx):");

                using ExcelReader reader = new(motifsFile, true);

                while (reader.ReadNextRow(out var row))
                {
                    Motifs.Add(new()
                    {
                        Program = program,
                        Code = row.GetCellByHeader<int>("Code").ToString(),
                        Description = row.GetCellByHeader<string>("Description"),
                        Sequence = row.GetCellByHeader<string>("Pattern"),
                        Group = row.GetCellByHeader<string>("Group")
                    });
                }

                await ctx.BulkInsertAsync(Motifs,
                    c =>
                    {
                        c.SetOutputIdentity = true;
                        c.PreserveInsertOrder = true;
                    });

                Console.WriteLine($"{Motifs.Count} motifs added");
            }

            var FeatureIds = await ctx.Features
                .Where(f => f.Sequence.GenomeId == GenomeId
                            && f.Type == "gene")
                .Select(f => f.Id)
                .ToListAsync();

            Console.WriteLine($"{FeatureIds.Count} features");

            var sequences = await FeatureExtractor.ProteinSequence(FeatureIds);

            Console.WriteLine($"{sequences.Count} sequences");

            List<FeatureMotif> Matches = new();

            foreach (var mot in Motifs)
            {
                foreach (var seq in sequences)
                {
                    var match = Regex.Match(seq.Value, mot.Sequence);

                    while (match.Success)
                    {
                        Matches.Add(new()
                        {
                            MotifId = mot.Id,
                            FeatureId = seq.Key,
                            Start = match.Index + 8,
                            End = match.Index + 8
                        });

                        match = match.NextMatch();
                    }
                }
            }

            Console.WriteLine($"{Matches.Count} matches");

            await ctx.BulkInsertAsync(Matches);

            Console.WriteLine("Done");
        }
    }
}
