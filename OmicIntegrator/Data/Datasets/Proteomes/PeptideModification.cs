namespace OmicIntegrator.Data.Datasets.Proteomes
{
    public class PeptideModification
    {
        public long Id { get; set; }
        public long PeptideId { get; set; }
        public required Peptide Peptide { get; set; }
        public required string ModificationType { get; set; }
        public int? ResiduePosition { get; set; }
    }
}
