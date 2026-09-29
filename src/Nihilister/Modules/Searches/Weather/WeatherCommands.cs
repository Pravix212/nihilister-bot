using Nihilister.Modules.Searches.Services;
using System.Diagnostics.CodeAnalysis;

namespace Nihilister.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class WeatherCommands : NadekoModule<SearchesService>
    {
        private readonly ITimezoneService _tzSvc;

        public WeatherCommands(ITimezoneService tzSvc)
        {
            _tzSvc = tzSvc;
        }

        private async Task<bool> ValidateQuery([MaybeNullWhen(false)] string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
                return true;

            await Response().Error(strs.specify_search_params).SendAsync();
            return false;
        }

        [Cmd]
        public async Task Weather([Leftover] string query)
        {
            if (!await ValidateQuery(query))
                return;

            var embed = CreateEmbed();
            var data = await _service.GetWeatherDataAsync(query);

            if (data is null)
                embed.WithDescription(GetText(strs.city_not_found)).WithErrorColor();
            else
            {
                var f = StandardConversions.CelsiusToFahrenheit;

                var tz = _tzSvc.GetTimeZoneOrUtc(ctx.Guild?.Id);
                var sunrise = data.Sys.Sunrise.ToUnixTimestamp();
                var sunset = data.Sys.Sunset.ToUnixTimestamp();
                sunrise = sunrise.ToOffset(tz.GetUtcOffset(sunrise));
                sunset = sunset.ToOffset(tz.GetUtcOffset(sunset));
                var timezone = $"UTC{sunrise:zzz}";

                embed
                    .AddField("🌍 " + Format.Bold(GetText(strs.location)),
                        $"[{data.Name + ", " + data.Sys.Country}](https://openweathermap.org/city/{data.Id})",
                        true)
                    .AddField("📏 " + Format.Bold(GetText(strs.latlong)), $"{data.Coord.Lat}, {data.Coord.Lon}", true)
                    .AddField("☁ " + Format.Bold(GetText(strs.condition)),
                        string.Join(", ", data.Weather.Select(w => w.Main)),
                        true)
                    .AddField("😓 " + Format.Bold(GetText(strs.humidity)), $"{data.Main.Humidity}%", true)
                    .AddField("💨 " + Format.Bold(GetText(strs.wind_speed)), data.Wind.Speed + " m/s", true)
                    .AddField("🌡 " + Format.Bold(GetText(strs.temperature)),
                        $"{data.Main.Temp:F1}°C / {f(data.Main.Temp):F1}°F",
                        true)
                    .AddField("🔆 " + Format.Bold(GetText(strs.min_max)),
                        $"{data.Main.TempMin:F1}°C - {data.Main.TempMax:F1}°C\n{f(data.Main.TempMin):F1}°F - {f(data.Main.TempMax):F1}°F",
                        true)
                    .AddField("🌄 " + Format.Bold(GetText(strs.sunrise)), $"{sunrise:HH:mm} {timezone}", true)
                    .AddField("🌇 " + Format.Bold(GetText(strs.sunset)), $"{sunset:HH:mm} {timezone}", true)
                    .WithOkColor()
                    .WithFooter("Powered by openweathermap.org",
                        $"https://openweathermap.org/img/w/{data.Weather[0].Icon}.png");
            }

            await Response().Embed(embed).SendAsync();
        }
    }
}
