using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OmicIntegrator.Data
{
    [Index(nameof(PValue))]
    public class FeatureMotif
    {
        public long Id { get; set; }
        public long FeatureId { get; set; }
        public Feature Feature { get; set; }
        public long MotifId { get; set; }
        public Motif Motif { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public char Strand { get; set; }
        public double PValue { get; set; }
    }
    class FeatureMotivoConfig : IEntityTypeConfiguration<FeatureMotif>
    {
        public void Configure(EntityTypeBuilder<FeatureMotif> builder)
        {
            builder.Property(m => m.Strand).HasDefaultValue('+');
        }
    }
}
