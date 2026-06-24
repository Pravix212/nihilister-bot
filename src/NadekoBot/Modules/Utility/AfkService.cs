using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Modules.Utility;

public sealed class AfkService : INService, IReadyExecutor
{
    private readonly IBotCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly MessageSenderService _mss;

    private static readonly TimeSpan _maxAfkDuration = 8.Hours();

    public AfkService(IBotCache cache, DiscordSocketClient client, MessageSenderService mss)
    {
        _cache = cache;
        _client = client;
        _mss = mss;
    }

    private static TypedKey<string> GetKey(ulong userId)
        => new($"afk:msg:{userId}");

    private static TypedKey<bool> GetRecentlySentKey(ulong userId, ulong channelId)
        => new($"afk:recent:{userId}:{channelId}");

    // Accepts IGuildUser to immediately apply the [AFK] prefix to the nickname
    public async Task<bool> SetAfkAsync(IGuildUser user, string text)
    {
        var success = await _cache.AddAsync(GetKey(user.Id), text, _maxAfkDuration, overwrite: true);
        if (success)
        {
            _ = Task.Run(async () => await TryChangeNicknameAsync(user, true));
        }
        return success;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceivedAsync;
        return Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(SocketMessage sm)
    {
        if (sm.Author.IsBot || sm.Author.IsWebhook)
            return Task.CompletedTask;

        if (sm is not IUserMessage uMsg || uMsg.Channel is not ITextChannel tc)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            if (sm.Author is IGuildUser gUser)
            {
                await TryClearSelfAfkInternalAsync(gUser, tc);
            }
            await TryReplyAfkOnMentionInternalAsync(sm, uMsg, tc);
        });

        return Task.CompletedTask;
    }

    private async Task TryClearSelfAfkInternalAsync(IGuildUser user, ITextChannel tc)
    {
        try
        {
            var key = GetKey(user.Id);
            var result = await _cache.GetAsync(key);
            if (!result.TryPickT0(out _, out _))
                return;

            await _cache.RemoveAsync(key);

            // Revert nickname back to normal asynchronously
            _ = Task.Run(async () => await TryChangeNicknameAsync(user, false));

            var msg = await _mss.Response(tc).Confirm("AFK message cleared!").SendAsync();
            msg.DeleteAfter(5);
        }
        catch (Exception ex)
        {
            Log.Warning("Unexpected error clearing afk: {Message}", ex.Message);
        }
    }

    // Safely changes or updates nicknames with Discord boundary conditions handled
    // FIXED: Re-fetches user from guild cache to ensure fresh nickname data
    private async Task TryChangeNicknameAsync(IGuildUser user, bool isGoingAfk)
    {
        try
        {
            // Re-fetch from guild cache to get the latest nickname
            // This fixes the bug where stale cached user data caused [AFK] removal to fail
            var gUser = await user.Guild.GetUserAsync(user.Id) as IGuildUser;
            var targetUser = gUser ?? user;
            var currentNickname = gUser?.Nickname ?? user.Nickname ?? user.GlobalName ?? user.Username;

            if (isGoingAfk)
            {
                if (currentNickname.StartsWith("[AFK] ", StringComparison.OrdinalIgnoreCase))
                    return;

                string newNick = $"[AFK] {currentNickname}";
                if (newNick.Length > 32)
                    newNick = newNick[..32];

                await targetUser.ModifyAsync(properties => properties.Nickname = newNick);
            }
            else
            {
                if (currentNickname.StartsWith("[AFK] ", StringComparison.OrdinalIgnoreCase))
                {
                    string originalNick = currentNickname["[AFK] ".Length..];

                    if (originalNick == (user.GlobalName ?? user.Username))
                        await targetUser.ModifyAsync(properties => properties.Nickname = null);
                    else
                        await targetUser.ModifyAsync(properties => properties.Nickname = originalNick);
                }
            }
        }
        catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
        {
            Log.Warning("Failed to update nickname for {User} due to Discord hierarchy or missing permissions.", user.Username);
        }
        catch (Exception ex)
        {
            Log.Warning("Unexpected error modifying nickname: {Message}", ex.Message);
        }
    }

    private async Task TryReplyAfkOnMentionInternalAsync(SocketMessage sm, IUserMessage uMsg, ITextChannel tc)
    {
        if ((sm.MentionedUsers.Count is 0 or > 3) && uMsg.ReferencedMessage is null)
            return;

        ulong mentionedUserId = 0;

        if (sm.MentionedUsers.Count <= 3)
        {
            foreach (var uid in uMsg.MentionedUserIds)
            {
                if (uid == sm.Author.Id)
                    continue;

                if (sm.Content.StartsWith($"<@{uid}>") || sm.Content.StartsWith($"<@!{uid}>"))
                {
                    mentionedUserId = uid;
                    break;
                }
            }
        }

        if (mentionedUserId == 0)
        {
            if (uMsg.ReferencedMessage?.Author?.Id is not ulong repliedUserId)
                return;

            mentionedUserId = repliedUserId;
        }

        try
        {
            var result = await _cache.GetAsync(GetKey(mentionedUserId));
            if (result.TryPickT0(out var afkMsg, out _))
            {
                var st = SmartText.CreateFrom(afkMsg);

                st = $"The user you've pinged (<#{mentionedUserId}>) is AFK: " + st;

                var toDelete = await _mss.Response(sm.Channel)
                                         .User(sm.Author)
                                         .Message(uMsg)
                                         .Text(st)
                                         .SendAsync();

                toDelete.DeleteAfter(30);

                var botUser = await tc.Guild.GetCurrentUserAsync();
                var perms = botUser.GetPermissions(tc);
                if (!perms.SendMessages)
                    return;

                var key = GetRecentlySentKey(mentionedUserId, sm.Channel.Id);
                var recent = await _cache.GetAsync(key);

                if (!recent.TryPickT0(out _, out _))
                {
                    var chMsg = await _mss.Response(sm.Channel)
                                          .Message(uMsg)
                                          .Pending(strs.user_afk($"<@{mentionedUserId}>"))
                                          .SendAsync();

                    chMsg.DeleteAfter(5);
                    await _cache.AddAsync(key, true, expiry: TimeSpan.FromMinutes(5));
                }
            }
        }
        catch (HttpException ex)
        {
            Log.Warning("Error in afk service: {Message}", ex.Message);
        }
    }
}
