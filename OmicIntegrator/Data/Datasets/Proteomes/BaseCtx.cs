using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data.Datasets.Proteomes;

namespace OmicIntegrator.Data
{
    partial class BaseCtx
    {
        public DbSet<Peptide> ProteomesPeptides { get; set; }
        public DbSet<PeptideModification> ProteomesPeptidesModifications { get; set; }
        public DbSet<PeptideFeature> ProteomesPeptidesFeatures { get; set; }
        public DbSet<ProteomeValue> ProteomesValues { get; set; }
    }
}
