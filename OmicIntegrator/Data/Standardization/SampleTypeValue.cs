using OmicIntegrator.Data.Datasets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmicIntegrator.Data.Standardization
{
    public class SampleTypeValue
    {
        public long Id { get; set; }
        public int StandardizationId { get; set; }
        public TreatmentsStandardization Standardization {  get; set; }
        public SampleType SampleType { get; set; }
        public Feature Feature { get; set; }
        public long FeatureId { get; set; }
        public decimal? SAValue { get; set; }
        public decimal? SValue_SD { get; set; }
        public PresenceValue Presence {  get; set; }
        public bool HighAbundance { get; set; }
    }
    public enum PresenceValue
    {
        Present,
        Absent,
        Uncertain,
    }
}
