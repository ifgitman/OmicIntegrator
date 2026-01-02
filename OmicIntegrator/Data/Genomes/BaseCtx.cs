using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    partial class BaseCtx
    {
        public DbSet<Genome> Genomes { get; set; }
        public DbSet<Sequence> Sequences { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<FeatureParent> FeaturesParents { get; set; }
    }
}
