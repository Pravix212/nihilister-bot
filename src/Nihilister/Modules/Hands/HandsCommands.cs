namespace Nihilister.Modules.Utility;

public partial class Utility
{
    [Group]
    public sealed class HandsCommands : NadekoModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Hands(int page = 1)
        {
            if (--page < 0)
                return;

            if (((SocketGuildUser)ctx.User).VoiceChannel is not SocketStageChannel stageChannel)
            {
                await Response().Error(strs.not_in_voice).SendAsync();
                return;
            }

            await Response()
                .Paginated()
                .PageItems((p) => Task.FromResult<IReadOnlyCollection<SocketGuildUser>>(stageChannel.ConnectedUsers
                    .Where(x => x.RequestToSpeakTimestamp is not null)
                    .OrderBy(x => x.RequestToSpeakTimestamp)
                    .Skip(p * 10)
                    .Take(10)
                    .ToList()
                    .AsReadOnly()))
                .PageSize(10)
                .CurrentPage(page)
                .AddFooter(false)
                .Page((requestedUsers, curPage) =>
                {
                    var embed = CreateEmbed()
                        .WithTitle(GetText(strs.raised_hands))
                        .WithOkColor();

                    if (requestedUsers.Count == 0)
                    {
                        embed.WithDescription(GetText(strs.empty_page));
                        return embed;
                    }

                    for (var i = 0; i < requestedUsers.Count; i++)
                    {
                        var ru = requestedUsers[i];
                        var ts = ru.RequestToSpeakTimestamp;
                        if (ts is not DateTimeOffset dto)
                            continue;

                        embed.AddField($"#{curPage * 10 + i + 1} {ru.Username}",
                            TimestampTag.FromDateTimeOffset(dto, TimestampTagStyles.Relative));
                    }

                    return embed;
                })
                .SendAsync();
        }
    }
}
