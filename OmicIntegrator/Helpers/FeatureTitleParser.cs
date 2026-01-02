using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers.Enums;

namespace OmicIntegrator.Helpers
{
    public class FeatureTitleParser
    {
        private readonly FeatureTitleFormats Format;
        private readonly BaseCtx ctx = null;

        public FeatureTitleParser() : 
            this(ConsoleInput.PickItem(new[] 
                                        {
                                            FeatureTitleFormats.OmicIntegratorId, 
                                            FeatureTitleFormats.Code 
                                        }
                                        .ToDictionary(f => f, f => f.ToString()),
                                       "Feature titles format:"))
        { }
        public FeatureTitleParser(FeatureTitleFormats Format) 
        {
            this.Format = Format;

            if (Format == FeatureTitleFormats.Code)
                ctx = new();
        }
        private int? GenomeId = null;

        public async Task<long> Parse(string FeatureTitle)
        {
            switch (Format)
            {
                case FeatureTitleFormats.OmicIntegratorId:
                    return long.Parse(FeatureTitle);
                case FeatureTitleFormats.Code:
                    if (!GenomeId.HasValue)
                        GenomeId = await ConsoleInput.PickGenomeId();

                    return await ctx.Features
                        .Where(f => f.Sequence.GenomeId == GenomeId.Value
                                    && f.Code.ToUpper() == FeatureTitle.ToUpper())
                        .Select(f => f.Id)
                        .SingleAsync();

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
