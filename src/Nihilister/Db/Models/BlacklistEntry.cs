#nullable disable
namespace Nihilister.Db.Models;

public class BlacklistEntry : DbEntity
{
    public ulong ItemId { get; set; }
    public BlacklistType Type { get; set; }
}

public enum BlacklistType
{
    Server,
    Channel,
    User
}