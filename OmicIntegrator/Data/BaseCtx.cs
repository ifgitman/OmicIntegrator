using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    public partial class BaseCtx : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder o)
        {
            o.UseSqlite($"Data Source={Settings.Current?.DatabaseFile}");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Feature>(b =>
            {
                b
                .HasMany(f => f.Parents)
                .WithMany(f => f.Features)
                .UsingEntity<FeatureParent>(j => j.HasOne(fp => fp.Parent).WithMany(f => f.FeaturesFP),
                                            j => j.HasOne(fp => fp.Feature).WithMany(f => f.ParentsFP));
            });

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseCtx).Assembly);
        }
    }
}
