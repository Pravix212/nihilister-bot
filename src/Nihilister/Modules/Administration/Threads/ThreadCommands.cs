#nullable disable
namespace Nihilister.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class ThreadCommands : NadekoModule
    {
        [Cmd]
        [BotPerm(ChannelPermission.CreatePublicThreads)]
        [UserPerm(ChannelPermission.CreatePublicThreads)]
        public async Task ThreadCreate([Leftover] string name)
        {
            if (ctx.Channel is not SocketTextChannel stc)
                return;

            await stc.CreateThreadAsync(name, message: ctx.Message.ReferencedMessage);
            await ctx.OkAsync();
        }

        [Cmd]
        [BotPerm(ChannelPermission.ManageThreads)]
        [UserPerm(ChannelPermission.ManageThreads)]
        public async Task ThreadDelete([Leftover] string name)
        {
            if (ctx.Channel is not SocketTextChannel stc)
                return;

            var t = stc.Threads.FirstOrDefault(
                x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

            if (t is null)
            {
                await Response().Error(strs.not_found).SendAsync();
                return;
            }

            await t.DeleteAsync();
            await ctx.OkAsync();
        }
    }
}
