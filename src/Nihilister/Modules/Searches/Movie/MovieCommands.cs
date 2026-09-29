using Nihilister.Modules.Searches.Services;
using System.Diagnostics.CodeAnalysis;

namespace Nihilister.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class MovieCommands : NadekoModule<SearchesService>
    {
        private async Task<bool> ValidateQuery([MaybeNullWhen(false)] string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
                return true;

            await Response().Error(strs.specify_search_params).SendAsync();
            return false;
        }
        
        [Cmd]
        public async Task Movie([Leftover] string query)
        {
            if (!await ValidateQuery(query))
                return;

            await ctx.Channel.TriggerTypingAsync();

            var movie = await _service.GetMovieDataAsync(query);
            if (movie is null)
            {
                await Response().Error(strs.imdb_fail).SendAsync();
                return;
            }

            // Rating, Genre, and Year are used directly as strings here, not resource keys
            await Response()
                  .Embed(CreateEmbed()
                         .WithOkColor()
                         .WithTitle(movie.Title)
                         .WithUrl($"https://www.imdb.com/title/{movie.ImdbId}/")
                         .WithDescription(movie.Plot.TrimTo(1000))
                         .AddField("Rating", movie.ImdbRating, true) 
                         .AddField("Genre", movie.Genre, true)
                         .AddField("Year", movie.Year, true)
                         .WithImageUrl(Uri.IsWellFormedUriString(movie.Poster, UriKind.Absolute)
                             ? movie.Poster
                             : null))
                  .SendAsync();
        }
    }
}
