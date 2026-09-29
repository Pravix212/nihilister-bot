using System.ComponentModel.DataAnnotations;

namespace Nihilister.Modules.Owner.Dangerous;

/// <summary>
/// Temporary table model for cleanup operations.
/// </summary>
public sealed class CleanupId
{
    /// <summary>
    /// The guild ID to keep.
    /// </summary>
    [Key]
    public ulong GuildId { get; set; }
}
