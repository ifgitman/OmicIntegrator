using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using NPOI.SS.Formula.Functions;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Datasets.Transcriptomes;
using OmicIntegrator.Helpers;
using System.Security.Cryptography.Xml;

namespace OmicIntegrator.Ingestion.Datasets.Transcriptomes
{
    public static class LoadRnaSample
    {
        public enum FeatureTypes
        {
            Genes,
            Transcripts
        }
        public enum ValueTypes
        {
            Counts,
            RPKM,
            TPM
        }
        public enum TranscriptSelectionCriteria
        {
            Representative,
            Longest
        }
        public static async Task Program()
        {
            var GenomeId = await ConsoleInput.PickGenomeId();

            var DatasetId = await ConsoleInput.PickTableIdInt<Dataset>("Select dataset:", d => d.Id, d => d.Description, d => d.GenomeId == GenomeId);

            var TreatmentId = await ConsoleInput.PickTableIdInt<Treatment>
                ("Select treatment:", t => t.Id, t => t.Description, t => t.DatasetId == DatasetId);

            using var RnaFile = ConsoleInput.ReadFile("RNAseq file:");

            var FeatType = ConsoleInput.PickEnum<FeatureTypes>("Type of annotated features:");

            var Separator = ConsoleInput.AskString("Columns separator (press TAB key for tsv files):");

            var WithHeaders = ConsoleInput.AskBool("With column headers?");

            if (WithHeaders)
            {
                var header = await RnaFile.ReadLineAsync();

                header = header.Replace("\"", "");

                var fields = header.Split(Separator);

                for (var x = 0; x < fields.Length; x++)
                {
                    Console.WriteLine($"{x} - {fields[x]}");
                }
            }

            var CodesCol = ConsoleInput.AskInteger("Codes column:");

            var ValuesCol = ConsoleInput.AskInteger("Values column:");
            var ValuesType = ConsoleInput.PickEnum<ValueTypes>("Select values type:");

            List<FileRow> Rows = new();

            while (!RnaFile.EndOfStream)
            {
                var Line = await RnaFile.ReadLineAsync();

                Line = Line.Replace("\"", "");//for Yu 2019

                var Fields = Line.Split(Separator);

                if (Fields.Length <= Math.Max(CodesCol, ValuesCol))
                    continue;

                if (double.TryParse(Fields[ValuesCol], out var val))
                {
                    Rows.Add(new(Fields[CodesCol],//.Split("|").First()
                             val));
                }
            }

            RnaFile.Close();

            BaseCtx ctx = new();

            List<FeatureLength> FeaturesLength;

            if (FeatType == FeatureTypes.Genes)
            {
                var selCriteria = ConsoleInput.PickEnum<TranscriptSelectionCriteria>("Gene transcript selection criteria:");

                List<long> ExcludedFeatureIds = [];
                int? chrExcludeId = null;

                var includeTranscriptsWithoutExons = ConsoleInput.AskBool("Include transcripts without exons?");

                if (ConsoleInput.AskBool("Exclude features?"))
                {
                    ExcludedFeatureIds.AddRange(await ConsoleInput.AskFeatureIds(GenomeId: GenomeId));
                }
                
                var exons = await ctx.FeaturesParents
                    .Where(f => f.Parent.Sequence.GenomeId == GenomeId
                                && f.Feature.Type == "exon")
                    .Select(p => new
                    {
                        TranscriptId = p.ParentId,
                        ExonId = p.FeatureId,
                        p.Feature.Start,
                        p.Feature.End
                    })
                    .ToListAsync();

                var splicedTranscripts = exons
                    .GroupBy(e => e.TranscriptId)
                    .Select(es => new
                    {
                        TranscriptId = es.Key,
                        Length = es.Sum(e => Math.Abs(e.End - e.Start) + 1)
                    })
                    .ToList();

                var qGenes = ctx.FeaturesParents
                    .Where(f => f.Parent.Sequence.GenomeId == GenomeId
                                && f.Parent.Type == "gene");

                if (ExcludedFeatureIds.Any())
                {
                    qGenes = qGenes.Where(g => !ExcludedFeatureIds.Contains(g.ParentId));
                }

                if (chrExcludeId.HasValue)
                {
                    qGenes = qGenes.Where(g => g.Parent.SequenceId != chrExcludeId.Value);
                }

                var genes = await  qGenes
                    .Select(p => new
                    {
                        GeneId = p.ParentId,
                        GeneCode = p.Parent.Code,
                        TranscriptId = p.FeatureId,
                        p.Feature.IsGeneRepresentative,
                        PrimaryTranscriptStart = p.Feature.Start,
                        PrimaryTranscriptEnd = p.Feature.End,
                    })
                    .ToListAsync();

                FeaturesLength = genes
                    .GroupJoin(splicedTranscripts,
                               g => g.TranscriptId,
                               s => s.TranscriptId,
                               (g, ss) => new
                               {
                                   g.GeneCode,
                                   g.GeneId,
                                   g.TranscriptId,
                                   g.IsGeneRepresentative,
                                   Length = ss.Any() ? ss.Single().Length : Math.Abs(g.PrimaryTranscriptEnd - g.PrimaryTranscriptStart) + 1,
                                   WithExons = ss.Any()
                               })
                    .Where(g => includeTranscriptsWithoutExons | g.WithExons)
                    .GroupBy(e => e.GeneId)
                    .Select(g => new FeatureLength(FeatureCode: g.First().GeneCode,
                                                   FeatureId: g.Key,
                                                   Length: g.OrderByDescending(t => selCriteria == TranscriptSelectionCriteria.Representative ? 
                                                                                    t.IsGeneRepresentative :
                                                                                    false)
                                                            .ThenByDescending(t => t.Length)
                                                            .First()
                                                            .Length))
                    .ToList();
            }
            else
            {
                FeaturesLength = await ctx.Features
                    .Where(f => f.Sequence.GenomeId == GenomeId
                                && f.Type == "mRNA")
                    .Select(f => new FeatureLength(f.Code,
                                                   f.Id,
                                                   f.Features
                                                   .Where(f => f.Type == "exon")
                                                   .Sum(f => Math.Abs(f.End - f.Start) + 1)))
                    .ToListAsync();
            }

            var LengthRows = Rows
                .Join(FeaturesLength,
                      f => f.FeatureCode,
                      f => f.FeatureCode,
                      (f, l) => new
                      {
                          f.FeatureCode,
                          f.Value,
                          l.FeatureId,
                          l.Length
                      })
                .ToList();

            Console.WriteLine($"{Rows.Count - LengthRows.Count} missing features.");

            Sample sample = new()
            {
                TreatmentId = TreatmentId,
                Description = ConsoleInput.AskString("Sample description (e.g. replicate 1):"),
                Type = SampleType.Transcriptome
            };
            ctx.Samples.Add(sample);
            await ctx.SaveChangesAsync();

            List<RnaValue> adding = null;

            switch (ValuesType)
            {
                case ValueTypes.Counts:
                    var TpmsA = LengthRows
                        .Where(f => f.Length != 0)
                        .Select(f => new { f.FeatureId, A = f.Value * Math.Pow(10, 3) / f.Length })
                        .ToList();

                    var ATotal = TpmsA.Sum(t => t.A);
                    adding = TpmsA
                        .Select(f => new RnaValue()

                        {
                            SampleId = sample.Id,
                            FeatureId = f.FeatureId,
                            Tpm = (decimal)(f.A * Math.Pow(10, 6) / ATotal)
                        })
                        .ToList();

                    break;
                case ValueTypes.RPKM:
                    var RTotal = LengthRows.Sum(r => r.Value);

                    adding = LengthRows
                        .Select(r => new RnaValue()
                        {
                            SampleId = sample.Id,
                            FeatureId = r.FeatureId,
                            Tpm = (decimal)(r.Value * Math.Pow(10, 6) / RTotal)
                        })
                        .ToList();

                    break;
                case ValueTypes.TPM:
                    adding = LengthRows
                        .Select(f => new RnaValue()
                        {
                            SampleId = sample.Id,
                            FeatureId = f.FeatureId,
                            Tpm = (decimal)f.Value
                        })
                        .ToList();

                    break;
            }

            Console.WriteLine($"Adding {adding.Count} values.");

            await ctx.BulkInsertAsync(adding);

            Console.WriteLine("Done");
        }
        record FeatureLength(string FeatureCode, long FeatureId, long Length) { }
        record FileRow(string FeatureCode, double Value) { }
    }
}
