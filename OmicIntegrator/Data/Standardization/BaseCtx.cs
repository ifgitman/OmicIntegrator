using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data.Standardization;

namespace OmicIntegrator.Data
{
    partial class BaseCtx
    {
        public DbSet<TreatmentsStandardization> TreatmentsStandardizations { get; set; }
        public DbSet<TreatmentValue> S_TreatmentValues { get; set; }
        public DbSet<SampleTypeValue> SA_SampleTypeValues { get; set; }
        public DbSet<SummarizedPhosphosite> SummarizedPhosphosites { get; set; }
    }
}
