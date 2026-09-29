namespace Nihilister.Common;

public interface ITimezoneService
{
    TimeZoneInfo GetTimeZoneOrUtc(ulong? guildId);
}