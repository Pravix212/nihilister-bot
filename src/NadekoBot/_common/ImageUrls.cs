#nullable disable
using NadekoBot.Common.Yml;

namespace NadekoBot.Common;

public sealed class ImageUrls
{
    public int Version { get; set; } = 12;

    public CoinData Coins { get; set; }
    public Uri[] Currency { get; set; }
    public Uri[] Dice { get; set; }
    public XpData Xp { get; set; }

    public SlotData Slots { get; set; }

    public class SlotData
    {
        public Uri[] Emojis { get; set; }
        public Uri Bg { get; set; }
    }

    public class CoinData
    {
        public Uri[] Heads { get; set; }
        public Uri[] Tails { get; set; }
    }

    public class XpData
    {
        public Uri Bg { get; set; }
    }

    public WaifuActionData Waifu { get; set; }

    public class WaifuActionData
    {
        public Uri[] Hug { get; set; }
        public Uri[] Kiss { get; set; }
        public Uri[] Pat { get; set; }
        public Uri[] Nom { get; set; }
        public Uri[] Spit { get; set; }
        public Uri[] Explode { get; set; }
    }
}