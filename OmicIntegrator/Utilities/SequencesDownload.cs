using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Functions;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Utilities
{
    public static class SequencesDownload
    {
        public enum SequenceType
        {
            Protein,
            Promoter
        }
        public enum OutputFormat
        {
            Fasta,
            Excel
        }
        public static async Task Program()
        {
            var SeqType = ConsoleInput.PickEnum<SequenceType>("Sequence type");

            var outF = ConsoleInput.PickEnum<OutputFormat>("Output format:");

            var FileExt = outF switch
            {
                OutputFormat.Fasta => ".fasta",
                OutputFormat.Excel => ".xlsx"
            };

            string OutFile = null;

            List<long> FeatureIds;

            if (ConsoleInput.AskBool("Filter features?"))
            {
                FeatureIds = await ConsoleInput.AskFeatureIds("Feature IDs:");

                OutFile = Path.Combine(Path.GetDirectoryName(ConsoleInput.LastFileName),
                                             $"{Path.GetFileNameWithoutExtension(ConsoleInput.LastFileName)}_{SeqType}{FileExt}");

                Console.WriteLine($"Output file is {OutFile}");
            }
            else
            {
                BaseCtx ctx = new();

                FeatureIds = await ctx.Features
                    .Where(f => f.Type == "mRNA" && f.IsGeneRepresentative == true)
                    .Select(f => f.Id)
                    .ToListAsync();

                OutFile = ConsoleInput.AskFileName($"Output file ({FileExt}):", false);

                if (string.IsNullOrEmpty(OutFile)) return;
            }

            Dictionary<long, string> Sequences = null;

            switch (SeqType)
            {
                case SequenceType.Protein:
                    Sequences = await FeatureExtractor.ProteinSequence(FeatureIds);
                    break;
                case SequenceType.Promoter:
                    Sequences = await FeatureExtractor.Promoters(FeatureIds,
                                                                   ConsoleInput.AskInteger("Lenght upstream of TSS:"),
                                                                   ConsoleInput.AskInteger("Length downstream of TSS:"));
                    break;
            }

            if (outF == OutputFormat.Fasta)
            {
                await FastaDownload.Download(Sequences, OutFile, ConsoleInput.PickEnum<OmicIntegrator.Helpers.Enums.FeatureTitleFormats>());
            }
            else
            {
                BaseCtx ctx = new();

                var Features = await ctx.Features
                    .Where(f => Sequences.Keys.Contains(f.Id))
                    .Select(f => new
                    {
                        f.Id,
                        f.Code,
                        f.Alias,
                        f.ShortName,
                        f.Description,
                    })
                    .ToListAsync();

                var rtr = Features.Select(f => new
                {
                    f.Id,
                    f.Code,
                    f.Alias,
                    f.ShortName,
                    f.Description,
                    Secuencia = Sequences[f.Id],
                }).ToList();

                ExcelWriter excel = new();

                excel.Write(OutFile, rtr);
            }

            Console.WriteLine("Done");
        }
    }
}
