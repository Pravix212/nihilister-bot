using Nihilister.Modules.Searches.Services;
using System.Diagnostics.CodeAnalysis;

namespace Nihilister.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class WikiCommands : NadekoModule<SearchesService>
    {
        private async Task<bool> ValidateQuery([MaybeNullWhen(false)] string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
                return true;

            await Response().Error(strs.specify_search_params).SendAsync();
            return false;
        }
        
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
        public async Task Wiki([Leftover] string query)
        {
            query = query.Trim();

            if (!await ValidateQuery(query))
                return;

            var maybeRes = await _service.GetWikipediaPageAsync(query);
            if (!maybeRes.TryPickT0(out var res, out var error))
            {
                await HandleErrorAsync(error);
                return;
            }

            var data = res.Data;
            await Response().Text(data.Url).SendAsync();
        }
        
        [Cmd]
        public async Task Wikia(string target, [Leftover] string query)
        {
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(query))
            {
                await Response().Error(strs.wikia_input_error).SendAsync();
                return;
            }

            var maybeRes = await _service.GetWikiaPageAsync(target, query);

            if (!maybeRes.TryPickT0(out var res, out var error))
            {
                await HandleErrorAsync(error);
                return;
            }

            var response = $"### {res.Title}\n{res.Url}";
            await Response().Text(response).Sanitize().SendAsync();
        }
    }
}
