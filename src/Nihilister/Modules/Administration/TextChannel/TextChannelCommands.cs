namespace Nihilister.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class TextChannelCommands : NadekoModule<AdministrationService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task DelTxtChanl([Leftover] ITextChannel toDelete)
        {
            await toDelete.DeleteAsync(new RequestOptions()
            {
                AuditLogReason = $"Deleted by {ctx.User.Username}"
            });
            await Response().Confirm(strs.deltextchan(Format.Bold(toDelete.Name))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task CreaTxtChanl([Leftover] string channelName)
        {
            var txtCh = await ctx.Guild.CreateTextChannelAsync(channelName);
            await Response().Confirm(strs.createtextchan(Format.Bold(txtCh.Name))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task SetTopic([Leftover] string? topic = null)
        {
            var channel = (ITextChannel)ctx.Channel;
            topic ??= "";
            await channel.ModifyAsync(c => c.Topic = topic);
            await Response().Confirm(strs.set_topic).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task SetChanlName([Leftover] string name)
        {
            var channel = (ITextChannel)ctx.Channel;
            await channel.ModifyAsync(c => c.Name = name);
            await Response().Confirm(strs.set_channel_name).SendAsync();
        }
    }
}
