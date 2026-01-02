namespace OmicIntegrator.Data
{
    public class Genome
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<Sequence> Sequences { get; set; } = new();
    }
}
