namespace OmicIntegrator.Data.Datasets
{
    public class Treatment
    {
        public int Id { get; set; }
        public Dataset Dataset { get; set; }
        public int DatasetId { get; set; }
        public string Description { get; set; }
        public List<Sample> Samples { get; set; } = new();
    }
}
