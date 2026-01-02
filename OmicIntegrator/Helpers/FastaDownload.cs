using LinqKit;
using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using System.Linq.Expressions;

namespace OmicIntegrator.Helpers
{
    public static class FastaDownload
    {
        const int Width = 60;
        public static async Task Download(Dictionary<long, string> Features, string OutputFile, Enums.FeatureTitleFormats titleFormat)
        {
            BaseCtx ctx = new();

            var FeatIds = Features.Keys.ToList();

            Expression<Func<Feature, string>> TitleFunc;

            switch (titleFormat)
            {
                case Enums.FeatureTitleFormats.OmicIntegratorId:
                    TitleFunc = f => f.Id.ToString();
                    break;
                case Enums.FeatureTitleFormats.Code:
                    TitleFunc = f => f.Code;
                    break;
                case Enums.FeatureTitleFormats.Alias:
                    TitleFunc = f => !string.IsNullOrWhiteSpace(f.Alias) ? f.Alias :
                                          !string.IsNullOrWhiteSpace(f.ShortName) ?
                                            f.ShortName :
                                            (f.Code ?? f.Id.ToString());
                    break;
                case Enums.FeatureTitleFormats.AliasNameCode:
                    TitleFunc = f => !string.IsNullOrWhiteSpace(f.Alias) ?
                        $"{f.Alias} ({f.Code})" :
                        !string.IsNullOrWhiteSpace(f.ShortName) ?
                            $"{f.ShortName} ({f.Code})" :
                            (f.Code ?? f.Id.ToString());
                    break;
                default:
                    throw new Exception("Title format not implemented.");
            }

            var FeatCodes = await ctx.Features
                .AsExpandable()
                .Where(f => FeatIds.Contains(f.Id))
                .Select(f => new
                {
                    f.Id,
                    Title = TitleFunc.Invoke(f)
                })
                .ToListAsync();

            using var Archivo = File.CreateText(OutputFile);

            foreach (var f in Features.Join(FeatCodes,
                                            f => f.Key,
                                            f => f.Id,
                                            (s, f) => new { f.Title, Sequence = s.Value }))
            {
                await Archivo.WriteLineAsync($">{f.Title}");

                var Faltan = f.Sequence.Length;
                for (var i = 0; i < f.Sequence.Length; i += Width)
                {
                    await Archivo.WriteLineAsync(f.Sequence.Substring(i, Math.Min(Width, Faltan)));

                    Faltan -= Width;
                }
            }

            Archivo.Close();
        }
    }
}
