#nullable disable
using Nihilister.Common.Pokemon;
using Nihilister.Modules.Games.Common.Trivia;

namespace Nihilister.Services;

public interface ILocalDataCache
{
    Task<IReadOnlyDictionary<string, SearchPokemon>> GetPokemonsAsync();
    Task<IReadOnlyDictionary<string, SearchPokemonAbility>> GetPokemonAbilitiesAsync();
    Task<TriviaQuestionModel[]> GetTriviaQuestionsAsync();
    Task<IReadOnlyDictionary<int, string>> GetPokemonMapAsync();
}