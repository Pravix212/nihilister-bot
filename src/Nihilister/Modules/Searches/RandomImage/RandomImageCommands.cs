using Nihilister.Modules.Searches.Services;

namespace Nihilister.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class RandomImageCommands : NadekoModule<SearchesService>
    {
        [Cmd]
        public Task RandomCat()
            => InternalRandomImage(SearchesService.ImageTag.Cats);

        [Cmd]
        public Task RandomDog()
            => InternalRandomImage(SearchesService.ImageTag.Dogs);

        [Cmd]
        public Task RandomFood()
            => InternalRandomImage(SearchesService.ImageTag.Food);

        [Cmd]
        public Task RandomBird()
            => InternalRandomImage(SearchesService.ImageTag.Birds);

        private Task InternalRandomImage(SearchesService.ImageTag tag)
        {
            var url = _service.GetRandomImageUrl(tag);
            return Response().Embed(CreateEmbed().WithOkColor().WithImageUrl(url)).SendAsync();
        }
    }
}
