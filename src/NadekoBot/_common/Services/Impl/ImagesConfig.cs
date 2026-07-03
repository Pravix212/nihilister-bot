using NadekoBot.Common.Configs;

namespace NadekoBot.Services;

public sealed class ImagesConfig : ConfigServiceBase<ImageUrls>
{
    private const string PATH = "data/images.yml";

    private static readonly TypedKey<ImageUrls> _changeKey =
        new("config.images.updated");
    
    public override string Name
        => "images";

    public ImagesConfig(IConfigSeria serializer, IPubSub pubSub)
        : base(PATH, serializer, pubSub, _changeKey)
    {
        Migrate();
    }

    private void Migrate()
    {
        if (Data.Version < 10)
        {
            ModifyConfig(c =>
            {
                if(c.Xp.Bg.ToString().Contains("cdn.nadeko.bot"))
                    c.Xp.Bg = new("https://cdn.nadeko.bot/xp/bgs/v6.png");
                c.Version = 10;
            });
        }

        if (Data.Version < 11)
        {
            ModifyConfig(c =>
            {
                c.Waifu = CreateDefaultWaifuActions();
                c.Version = 11;
            });
        }

        if (Data.Version < 12)
        {
            ModifyConfig(c =>
            {
                c.Waifu ??= new ImageUrls.WaifuActionData();
                c.Waifu.Spit = [new Uri("https://media.tenor.com/aKSY2XuSpcwAAAAd/anime-spit.gif")];
                c.Version = 12;
            });
        }

        if (Data.Version < 13)
        {
            ModifyConfig(c =>
            {
                c.Waifu ??= new ImageUrls.WaifuActionData();
                c.Waifu.Explode =
                [
                    new Uri("https://media.discordapp.net/attachments/1516821409941426289/1522613583668707561/house-explosion.gif?ex=6a491be7&is=6a47ca67&hm=d085388ee88d7eb616e5ccc964e722dcccce74cf5e727931032b971c8c774f23&="),
                    new Uri("https://media.discordapp.net/attachments/1516821409941426289/1522613584167833752/explosion-explode.gif?ex=6a491be7&is=6a47ca67&hm=687bb726b7609467d149ba841adea34fecb94582dc2e7f6abb3a227804048254&="),
                    new Uri("https://i.imgur.com/eVfpSFf.gif")
                    ];
                c.Version = 13;
            });
        }

    }

    private static ImageUrls.WaifuActionData CreateDefaultWaifuActions()
        => new()
        {
            Hug = Enumerable.Range(0, 20).Select(i => new Uri($"https://cdn.nadeko.bot/w/hug/hug_{i}.gif")).ToArray(),
            Kiss = Enumerable.Range(0, 20).Select(i => new Uri($"https://cdn.nadeko.bot/w/kiss/kiss_{i}.gif")).ToArray(),
            Pat = Enumerable.Range(0, 20).Select(i => new Uri($"https://cdn.nadeko.bot/w/pat/pat_{i}.gif")).ToArray(),
            Nom = Enumerable.Range(0, 20).Select(i => new Uri($"https://cdn.nadeko.bot/w/nom/nom_{i}.gif")).ToArray(),
            Spit = Enumerable.Range(0, 20).Select(i => new Uri($"https://cdn.nadeko.bot/w/spit/spit_{i}.gif")).ToArray(),
            Explode = Enumerable.Range(0, 20).Select(i => new Uri($"https://cdn.nadeko.bot/w/explode/explode_{i}.gif")).ToArray()
        };
}