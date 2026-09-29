namespace Nihilister.Modules.Searches;

public interface ISearchResultInformation
{
    string TotalResults { get; }
    string SearchTime { get; }
}