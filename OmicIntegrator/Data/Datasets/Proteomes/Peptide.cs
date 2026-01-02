namespace OmicIntegrator.Data.Datasets.Proteomes
{
    public class Peptide
    {
        public long Id { get; set; }
        public int DatasetId { get; set; }
        public Dataset Dataset { get; set; }
        public string? Sequence { get; set; }
        public string? IdInDataSet { get; set; }

        public List<PeptideModification> Modifications { get; set; } = new();

    }
}
