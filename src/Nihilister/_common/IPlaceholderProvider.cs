#nullable disable
namespace Nihilister.Common;

public interface IPlaceholderProvider
{
    public IEnumerable<(string Name, Func<string> Func)> GetPlaceholders();
}