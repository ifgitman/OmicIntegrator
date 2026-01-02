namespace OmicIntegrator.Data.Datasets.Transcriptomes
{
    public class RnaValue
    {
        public int Id { get; set; }
        public Sample Sample { get; set; }
        public int SampleId { get; set; }
        public Feature Feature { get; set; }
        public long FeatureId { get; set; }
        public decimal Tpm { get; set; }
    }
}
