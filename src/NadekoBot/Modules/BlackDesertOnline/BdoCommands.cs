using NadekoBot.Modules.BlackDesertOnline.Models;

namespace NadekoBot.Modules.BlackDesertOnline;

// NadekoModule<BdoGearService> provides:
//   _service   => BdoGearService
//   _inter     => INadekoInteractionService  (from base NadekoModule)
// BdoMarketService is injected via the primary constructor.
public partial class Bdo(BdoMarketService marketService) : NadekoModule<BdoGearService>
{
    protected readonly BdoMarketService _mkt = marketService;

    /// <summary>
    /// Custom GetUserInputAsync with a configurable timeout (default 60 s).
    /// The base class version hard-codes a 10-second timeout.
    /// </summary>
    private async Task<string?> GetUserInputWithTimeoutAsync(ulong userId, ulong channelId, int timeoutMs = 60_000)
    {
        var tcs = new TaskCompletionSource<string>();
        var dsc = (Discord.WebSocket.DiscordSocketClient)ctx.Client;
        try
        {
            dsc.MessageReceived += OnMessage;
            if (await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs)) != tcs.Task)
                return null;
            return await tcs.Task;
        }
        finally
        {
            dsc.MessageReceived -= OnMessage;
        }

        Task OnMessage(Discord.WebSocket.SocketMessage arg)
        {
            _ = Task.Run(() =>
            {
                if (arg is not Discord.WebSocket.SocketUserMessage userMsg
                    || userMsg.Channel is not Discord.ITextChannel
                    || userMsg.Author.Id != userId
                    || userMsg.Channel.Id != channelId)
                    return Task.CompletedTask;

                if (tcs.TrySetResult(arg.Content))
                    userMsg.DeleteAfter(1);

                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        }
    }
}