using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    partial class BaseCtx
    {
        public DbSet<Motif> Motifs { get; set; }
        public DbSet<FeatureMotif> FeaturesMotifs { get; set; }

    }
}
