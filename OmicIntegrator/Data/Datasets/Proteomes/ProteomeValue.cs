namespace OmicIntegrator.Data.Datasets.Proteomes
{
    public class ProteomeValue
    {
        public long Id { get; set; }
        public int SampleId { get; set; }
        public Sample Sample { get; set; }
        public long PeptideId { get; set; }
        public Peptide Peptide { get; set; }
        public decimal Intensity { get; set; }
    }
}
