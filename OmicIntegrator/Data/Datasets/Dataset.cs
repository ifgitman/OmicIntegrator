namespace OmicIntegrator.Data.Datasets
{
    public class Dataset
    {
        public int Id { get; set; }
        public Genome Genome { get; set; }
        public int GenomeId { get; set; }
        public string Description { get; set; }
        public string? Link { get; set; }
    }
}
