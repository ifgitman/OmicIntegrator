using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    partial class BaseCtx
    {
        public DbSet<GoTerm> GoTerms { get; set; }
        public DbSet<GoTermsRelationship> GoTermsRelactinships { get; set; }
        public DbSet<FeatureGoTerm> FeaturesGoTerms { get; set; }
    }
}
