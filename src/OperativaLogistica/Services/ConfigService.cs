using System.Collections.Generic;

namespace OperativaLogistica.Services
{
    public class ConfigService
    {
        public IReadOnlyList<string> Lados { get; } = new List<string>
        {
            "LADO 0", "LADO 1", "LADO 2", "LADO 3", "LADO 4"
        };
    }
}
