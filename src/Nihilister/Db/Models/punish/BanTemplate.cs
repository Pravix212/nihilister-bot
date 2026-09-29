#nullable disable
namespace Nihilister.Db.Models;

public class BanTemplate : DbEntity
{
    public ulong GuildId { get; set; }
    public string Text { get; set; }
    public int? PruneDays { get; set; }

    /// <summary>
    /// When true, the .unban command is disabled and timed bans will not auto-unban.
    /// </summary>
    public bool DisableUnban { get; set; }
}