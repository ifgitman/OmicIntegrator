using OmicIntegrator.Data.Datasets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmicIntegrator.Data.Standardization
{
    public class TreatmentValue
    {
        public long Id {  get; set; }
        public int StandardizationId { get; set; }
        public TreatmentsStandardization Standardization { get; set; }
        public Treatment Treatment { get; set; }
        public int TreatmentId { get; set; }
        public Feature Feature { get; set; }
        public long FeatureId { get; set; }
        public decimal? SValue { get; set; }
        public bool? WithExclusivity { get; set; }
    }
}
