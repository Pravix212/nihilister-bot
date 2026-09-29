using Nihilister.Common.Yml;

namespace Nihilister.Modules.Music;

public sealed class AudioFileCacheConfig
{
    [Comment("DO NOT CHANGE THE VERSION MANUALLY")]
    public int Version { get; set; } = 1;

    [Comment("Maximum total cache size in gigabytes. Minimum 1. Default 10")]
    public int MaxCacheSizeGb { get; set; } = 10;
}
