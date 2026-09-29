using Nihilister.Modules.Searches.Services;

namespace Nihilister.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class FactsCommands : NadekoModule<SearchesService>
    {
        public Task<IUserMessage> HandleErrorAsync(ErrorType error)
        {
            var errorKey = error switch
            {
                ErrorType.ApiKeyMissing => strs.api_key_missing,
                ErrorType.InvalidInput => strs.invalid_input,
                ErrorType.NotFound => strs.not_found,
                ErrorType.Unknown => strs.error_occured,
                _ => strs.error_occured,
            };

            return Response().Error(errorKey).SendAsync();
        }
        
        [Cmd]
        public async Task Catfact()
        {
            var maybeFact = await _service.GetCatFactAsync();

            if (!maybeFact.TryPickT0(out var fact, out var error))
            {
                await HandleErrorAsync(error);
                return;
            }

            await Response().Confirm("🐈" + GetText(strs.catfact), fact).SendAsync();
        }
    }
}
