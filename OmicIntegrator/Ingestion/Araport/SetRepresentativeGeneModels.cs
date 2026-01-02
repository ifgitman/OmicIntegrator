using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmicIntegrator.Ingestion.Araport
{
    public static class SetRepresentativeGeneModels
    {
        public static async Task Program()
        {
            var fasta = ConsoleInput.ReadFile("Representative gene models FASTA file:");

            var GenomeId = await ConsoleInput.PickGenomeId();

            List<string> representatives = [];

            var Line = await fasta.ReadLineAsync();

            while (Line != null)
            {
                if (Line.StartsWith(">"))
                {
                    var code = Line.Substring(1).Split("|").First().Trim();

                    representatives.Add(code);
                }

                Line = await fasta.ReadLineAsync();
            }
            fasta.Close();

            Console.WriteLine($"{representatives.Count} representative genes.");

            BaseCtx ctx = new();

            var feats = await ctx.Features
                .Where(f => f.Sequence.GenomeId == GenomeId &
                            representatives.Contains(f.Code))
                .ToListAsync();

            foreach (var f in feats) f.IsGeneRepresentative = true;

            await ctx.SaveChangesAsync();

            Console.WriteLine("Done");
        }
    }
}
