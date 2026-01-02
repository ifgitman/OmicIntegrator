using Microsoft.EntityFrameworkCore;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Data.Datasets.Transcriptomes;

namespace OmicIntegrator.Data
{
    partial class BaseCtx
    {
        public DbSet<Dataset> Datasets { get; set; }
        public DbSet<Sample> Samples { get; set; }
        public DbSet<Treatment> Treatments { get; set; }
        public DbSet<RnaValue> RnaValues { get; set; }

    }
}
