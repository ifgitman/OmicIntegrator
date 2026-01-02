using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

namespace OmicIntegrator
{
    public class Settings
    {
        public static Settings Current;
        public string? DatabaseFile { get; set; }
    }
}
