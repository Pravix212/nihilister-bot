#nullable disable
using Nihilister.Modules.Administration._common.results;

namespace Nihilister.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class ServerAppearanceCommands : NadekoModule<AdministrationService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPermission.ManageGuild)]
        [BotPerm(GuildPermission.ManageGuild)]
        public async Task SetServerBanner([Leftover] string img = null)
        {
            // Tier2 or higher is required to set a banner.
            if (ctx.Guild.PremiumTier is PremiumTier.Tier1 or PremiumTier.None)
                return;

            var result = await _service.SetServerBannerAsync(ctx.Guild, img);

            switch (result)
            {
                case SetServerBannerResult.Success:
                    await Response().Confirm(strs.set_srvr_banner).SendAsync();
                    break;
                case SetServerBannerResult.InvalidFileType:
                    await Response().Error(strs.srvr_banner_invalid).SendAsync();
                    break;
                case SetServerBannerResult.Toolarge:
                    await Response().Error(strs.srvr_banner_too_large).SendAsync();
                    break;
                case SetServerBannerResult.InvalidURL:
                    await Response().Error(strs.srvr_banner_invalid_url).SendAsync();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPermission.ManageGuild)]
        [BotPerm(GuildPermission.ManageGuild)]
        public async Task SetServerIcon([Leftover] string img = null)
        {
            var result = await _service.SetServerIconAsync(ctx.Guild, img);

            switch (result)
            {
                case SetServerIconResult.Success:
                    await Response().Confirm(strs.set_srvr_icon).SendAsync();
                    break;
                case SetServerIconResult.InvalidFileType:
                    await Response().Error(strs.srvr_banner_invalid).SendAsync();
                    break;
                case SetServerIconResult.InvalidURL:
                    await Response().Error(strs.srvr_banner_invalid_url).SendAsync();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
