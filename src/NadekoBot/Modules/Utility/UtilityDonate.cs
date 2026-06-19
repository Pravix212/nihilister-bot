using Discord;

namespace NadekoBot.Modules.Utility;

public partial class Utility : NadekoModule
{
    [Cmd]
    public async Task Donate()
    {
        var eb = CreateEmbed()
            .WithTitle("Support Nihilister")
            .WithDescription("Keeping the heretic alive costs real money.\\n\\n"
                + "**Monthly Costs:**\\n"
                + "• DigitalOcean VPS — $6-12/month\\n"
                + "• Grok AI API — $15-30/month\\n"
                + "• Domain & SSL — $10/year\\n\\n"
                + "Every donation helps keep the bot running 24/7.")
            .WithOkColor()
            .WithFooter("Thank you for supporting the Heathen's Garden ❤️");

        var button = new ButtonBuilder(
            label: "💰 Donate via PayPal",
            url: "https://prav.lol/nihilister/donate.html",
            style: ButtonStyle.Link
        );

        var components = new ComponentBuilder()
            .WithButton(button)
            .Build();

        await ctx.Channel.SendMessageAsync(embed: eb.Build(), components: components);
    }
}
