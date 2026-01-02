using Microsoft.EntityFrameworkCore;

namespace OmicIntegrator.Data
{
    [Index(nameof(Program), nameof(Code), IsUnique = true)]
    public class Motif
    {
        public long Id { get; set; }
        public string? Program { get; set; }
        public string? Code { get; set; }
        public string? OriginalSpecies { get; set; }
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public string? Sequence { get; set; }
        public string? Group { get; set; }
    }
}
