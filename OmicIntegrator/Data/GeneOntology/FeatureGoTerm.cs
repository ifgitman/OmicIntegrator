using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    [Index(nameof(GoTermId))]
    [Index(nameof(Source))]
    public class FeatureGoTerm
    {
        public long Id { get; set; }
        public long FeatureId { get; set; }
        public Feature Feature { get; set; }
        public GoTerm GoTerm { get; set; }
        public string GoTermId { get; set; }
        public required string Relationship { get; set; }
        public required string Source { get; set; }
    }
}
