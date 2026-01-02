namespace OmicIntegrator.Data
{
    public class Sequence
    {
        public int Id { get; set; }
        public Genome Genome { get; set; }
        public int GenomeId { get; set; }
        public string? Name { get; set; }
        public string? FilePath { get; set; }
        public long Start { get; set; }
        public int Width { get; set; }
        public long Length { get; set; }
        public bool IsChromosome { get; set; }
    }
}
