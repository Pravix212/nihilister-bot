using NadekoBot.Common.Configs;

namespace NadekoBot.Modules.SampleFinder;

public sealed class SampleFinderConfig : ConfigServiceBase<SampleFinderConfigData>
{
    private const string FILE_PATH = "data/samplefinder.yml";
    private static readonly TypedKey<SampleFinderConfigData> _changeKey = new("config.samplefinder.updated");

    public override string Name => "samplefinder";

    public SampleFinderConfig(IConfigSeria serializer, IPubSub pubSub)
        : base(FILE_PATH, serializer, pubSub, _changeKey)
    {
        AddParsedProp("apikey",
            static c => c.ApiKey,
            static (c, v) => c.ApiKey = v,
            ConfigParsers.String,
            ConfigPrinters.ToString,
            "Freesound.org API key. Get one at https://freesound.org/apiv2/apply/");
    }
}

public class SampleFinderConfigData
{
    public int Version { get; set; } = 1;
    public string ApiKey { get; set; } = string.Empty;
}
