namespace NadekoBot.Modules.Widget;

public partial class Widget : NadekoModule<WidgetService>
{
    private readonly DiscordSocketClient _client;

    public Widget(DiscordSocketClient client)
    {
        _client = client;
    }

    [Cmd]
    [OwnerOnly]
    public async Task WidgetSetup()
    {
        var oauthUrl = _service.GetOAuth2Url();

        var authorizeButton = new ButtonBuilder()
        {
            Style = ButtonStyle.Link,
            Label = "🔗 Authorize Widget",
            Url = oauthUrl
        };

        var components = new ComponentBuilder()
            .WithButton(authorizeButton)
            .Build();

        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle("🧩 Nihilister Widget Setup")
            .WithDescription("""
                **Step 1:** Click the button below to authorize the Nihilister widget on your profile.
                **Step 2:** Close the Discord page that opens after authorizing.
                **Step 3:** Run `.widgetrefresh` to push live stats to your widget.
                **Step 4:** Optionally, run `.widgetauto` to auto-refresh every 5 minutes.

                -# The authorization modal will show many permissions — this is normal for the Social Layer scope.
                -# Nihilister does not store the token and will never act on those permissions.
                """)
            .WithFooter("Discord Profile Widget v2");

        await ctx.Channel.SendMessageAsync(
            embed: eb.Build(),
            components: components);
    }

    [Cmd]
    [OwnerOnly]
    public async Task WidgetRefresh()
    {
        var msg = await Response()
            .Pending("⏳ Refreshing widget...")
            .SendAsync();

        var (success, error) = await _service.RefreshWidgetAsync(ctx.User.Id);

        if (success)
        {
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("✅ Widget Refreshed")
                .WithDescription("Your Discord profile widget has been updated with live Nihilister stats!")
                .WithCurrentTimestamp();

            await msg.ModifyAsync(m =>
            {
                m.Content = "";
                m.Embed = eb.Build();
            });
        }
        else
        {
            var eb = CreateEmbed()
                .WithErrorColor()
                .WithTitle("❌ Widget Refresh Failed")
                .WithDescription($"```\n{error}\n```\nMake sure you've authorized the widget first with `.widgetsetup`.");

            await msg.ModifyAsync(m =>
            {
                m.Content = "";
                m.Embed = eb.Build();
            });
        }
    }

    [Cmd]
    [OwnerOnly]
    public async Task WidgetAuto()
    {
        if (_service.IsAutoRefreshRunning)
        {
            _service.StopAutoRefresh();

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("⏹️ Widget Auto-Refresh Stopped")
                .WithDescription("The widget will no longer update automatically.");

            await Response().Embed(eb).SendAsync();
        }
        else
        {
            _service.StartAutoRefresh(ctx.User.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("▶️ Widget Auto-Refresh Started")
                .WithDescription("Your widget will now refresh automatically every **5 minutes** with live bot stats.")
                .AddField("To stop", "Run `.widgetauto` again to toggle off.", false);

            await Response().Embed(eb).SendAsync();
        }
    }
}
