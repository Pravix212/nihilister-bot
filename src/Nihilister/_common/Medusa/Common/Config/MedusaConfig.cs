#nullable enable
using Nihilister.Common.Yml;

namespace Nihilister.Medusa;

public sealed class MedusaConfig
{
    [Comment("DO NOT CHANGE THE VERSION MANUALLY")]
    public int Version { get; set; } = 1;
    
    [Comment("""List of medusae automatically loaded at startup""")]
    public List<string>? Loaded { get; set; }

    public MedusaConfig()
    {
        Loaded = new();
    }
}