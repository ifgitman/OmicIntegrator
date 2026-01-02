namespace OmicIntegrator.Data.Datasets.Proteomes
{
    public class PeptideFeature
    {
        public long Id { get; set; }
        public long PeptideId { get; set; }
        public long FeatureId { get; set; }
        public Peptide Peptide { get; set; }
        public Feature Feature { get; set; }
        public int? Position { get; set; }
    }
}
