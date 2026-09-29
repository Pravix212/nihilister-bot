using Nihilister.Modules.Searches.Services;

namespace Nihilister.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class ProfileCommands : NadekoModule<SearchesService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar([Leftover] IGuildUser? usr = null)
        {
            usr ??= (IGuildUser)ctx.User;

            var avatarUrl = usr.RealAvatarUrl(2048);

            await Response()
                  .Embed(
                      CreateEmbed()
                          .WithOkColor()
                          .AddField("Username", usr.ToString())
                          .AddField("Avatar Url", avatarUrl)
                          .WithThumbnailUrl(avatarUrl.ToString()))
                  .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Banner([Leftover] IGuildUser? usr = null)
        {
            usr ??= (IGuildUser)ctx.User;

            var bannerUrl = usr.GetGuildBannerUrl(size: 2048)
                            ?? (await ((DiscordSocketClient)ctx.Client).Rest.GetUserAsync(usr.Id))?.GetBannerUrl();

            if (bannerUrl is null)
            {
                await Response()
                      .Error(strs.no_banner)
                      .SendAsync();

                return;
            }

            await Response()
                  .Embed(
                      CreateEmbed()
                          .WithOkColor()
                          .AddField("Username", usr.ToString(), true)
                          .AddField("Banner Url", bannerUrl, true)
                          .WithImageUrl(bannerUrl))
                  .SendAsync();
        }
    }
}
