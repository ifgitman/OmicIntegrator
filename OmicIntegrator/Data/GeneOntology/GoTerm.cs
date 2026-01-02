using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OmicIntegrator.Data
{
    public class GoTerm
    {
        public required string Id { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public List<GoTermsRelationship> References { get; set; } = new();
        public List<GoTermsRelationship> Referred { get; set; } = new();
    }
    public class GoTermConfiguracion : IEntityTypeConfiguration<GoTerm>
    {
        public void Configure(EntityTypeBuilder<GoTerm> builder)
        {
            builder.HasMany(t => t.Referred)
                   .WithOne(r => r.Reference);

            builder.HasMany(t => t.References)
                .WithOne(r => r.Referred);
        }
    }
}
