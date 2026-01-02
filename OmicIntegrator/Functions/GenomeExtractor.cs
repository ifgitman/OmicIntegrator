using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using System.Text;

namespace OmicIntegrator.Functions
{
    public static class GenomeExtractor
    {
        static Dictionary<char, char> Complementaries = new() { { 'A', 'T' }, { 'T', 'A' }, { 'C', 'G' }, { 'G', 'C' }, { 'N', 'N' } };
        const int AnchoBajar = 60;
        public static async Task<Dictionary<long, string>> Extract(Dictionary<long, IEnumerable<Fragment>> FeatureFragments)
        {
            BaseCtx ctx = new();

            var FeatureIds = FeatureFragments
                .Where(f => f.Value.Any())
                .Select(f => f.Key)
                .ToList();

            var Features = await ctx.Features
                .Where(f => FeatureIds.Contains(f.Id))
                .Select(f => new
                {
                    f.Id,
                    f.SequenceId,
                    f.Strand,
                    Fragments = FeatureFragments[f.Id]
                })
                .ToListAsync();

            var SeqIds = Features.Select(f => f.SequenceId).Distinct().ToList();

            var Secuencias = await ctx.Sequences
                .Where(s => SeqIds.Contains(s.Id))
                .ToListAsync();

            Dictionary<long, string> rtr = new();

            foreach (var s in Secuencias
                .GroupJoin(Features,
                           s => s.Id,
                           f => f.SequenceId,
                           (seq, fs) => new { seq, feats = fs.OrderBy(f => f.Fragments.First().Start).ToList() }))
            {
                using var fastaFile = File.OpenRead(s.seq.FilePath);

                foreach (var f in s.feats)
                {
                    var sequence = await Extract(fastaFile, s.seq.Start, s.seq.Width, s.seq.Length,
                                                  f.Fragments,
                                                  f.Strand ?? '+');

                    rtr.Add(f.Id, sequence);
                }

                fastaFile.Close();
            }

            return rtr;

        }
        public static async Task<string> Extract
            (FileStream fastaFile,
             long seqStart,
             int seqWidth,
             long seqLength,
             IEnumerable<Fragment> Fragments,
             char Strand)
        {
            var extrSeq = new StringBuilder();
            byte[] buff = new byte[1024];

            foreach (var frg in Fragments.OrderBy(f => f.Start)
                .Select(f => new Fragment(Math.Max(f.Start - 1, 0),
                                          Math.Min(f.End, seqLength - 1))))
            {
                var posStart = seqStart + (long)Math.Truncate((decimal)frg.Start / seqWidth) + frg.Start;
                var posEnd = seqStart + frg.End + (long)Math.Truncate((decimal)frg.End / seqWidth);

                var remain = posEnd - posStart;

                fastaFile.Seek(posStart, SeekOrigin.Begin);
                while (fastaFile.Position < posEnd
                       && remain > 0)
                {
                    var read = await fastaFile.ReadAsync(buff, 0, (int)Math.Min(buff.Length, remain));

                    remain -= read;

                    extrSeq.Append(Encoding.ASCII.GetString(buff, 0, read));
                }

                if (remain != 0)
                    System.Diagnostics.Debugger.Break();
            }

            var SeqDna = extrSeq.ToString().Replace("\n", "").Replace("\r", "");

            if (Strand == '-')
            {
                StringBuilder Inverted = new();

                for (int x = SeqDna.Length - 1; x >= 0; x--)
                {
                    Inverted.Append(Complementaries[SeqDna[x]]);
                }

                SeqDna = Inverted.ToString();
            }

            var TotalLength = Fragments.Sum(f => (int)(f.End - f.Start + 1));

            if (SeqDna.Length != TotalLength)
                System.Diagnostics.Debugger.Break();

            return SeqDna;
        }
        public record Fragment(long Start, long End);
    }
}
