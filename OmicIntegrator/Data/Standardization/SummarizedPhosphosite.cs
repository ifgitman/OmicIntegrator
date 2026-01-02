using OmicIntegrator.Data.Datasets;

namespace OmicIntegrator.Data.Standardization
{
    public class SummarizedPhosphosite
    {
        public long Id { get; set; }
        public TreatmentsStandardization Standardization { get; set; }
        public int StandardizationId { get; set; }
        public Feature Feature { get; set; }
        public long FeatureId { get; set; }
        public int ResiduePosition { get; set; }
        public char Residue { get; set; }
        public Treatment Treatment { get; set; }
        public int TreatmentId { get; set; }
        public int ExclusivePeptides { get; set; }
    }
}
