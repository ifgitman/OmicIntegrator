using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    [Index(nameof(Code))]
    [Index(nameof(Start))]
    [Index(nameof(Type))]
    public class Feature
    {
        public long Id { get; set; }
        public string? Code { get; set; }
        public Sequence? Sequence { get; set; }
        public int SequenceId { get; set; }
        public required string Type { get; set; }
        public string? ShortName { get; set; }
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public char? Strand { get; set; }
        public int? Phase { get; set; }
        public bool IsGeneRepresentative { get; set; }

        public List<Feature> Parents { get; set; } = new();
        public List<FeatureParent> ParentsFP { get; set; } = new();
        public List<Feature> Features { get; set; } = new();
        public List<FeatureParent> FeaturesFP { get; set; } = new();

        public List<FeatureMotif> MotifMatches { get; set; } = new();
    }
}