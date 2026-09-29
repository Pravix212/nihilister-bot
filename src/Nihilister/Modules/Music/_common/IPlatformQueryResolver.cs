namespace Nihilister.Modules.Music;

public interface IPlatformQueryResolver
{
    Task<ITrackInfo?> ResolveByQueryAsync(string query);
}