#nullable disable
namespace Nihilister.Common;

public interface ICloneable<T>
    where T : new()
{
    public T Clone();
}