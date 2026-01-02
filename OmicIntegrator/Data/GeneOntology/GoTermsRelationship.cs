using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    [Index(nameof(Relationship))]
    public class GoTermsRelationship
    {
        public long Id { get; set; }
        public string ReferenceId { get; set; }
        public GoTerm Reference { get; set; }
        public string ReferredId { get; set; }
        public GoTerm Referred { get; set; }
        public string Relationship { get; set; }
    }
}
