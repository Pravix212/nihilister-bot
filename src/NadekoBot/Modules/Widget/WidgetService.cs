using System.Net.Http.Headers;
using System.Text.Json;
using NadekoBot.Services;

namespace NadekoBot.Modules.Widget;

public class WidgetService : INService
{
    private readonly DiscordSocketClient _client;
    private readonly IStatsService _stats;
    private readonly CommandService _cmds;
    private readonly IBotCreds _creds;
    private readonly IHttpClientFactory _httpFactory;

    private CancellationTokenSource? _autoRefreshCts;
    private Task? _autoRefreshTask;
    private ulong _autoRefreshUserId;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public bool IsAutoRefreshRunning => _autoRefreshTask is { IsCompleted: false };

    private double _grokBalance = 5.88;
    private double _grokLimit = 20.00;

    public WidgetService(
        DiscordSocketClient client,
        IStatsService stats,
        CommandService cmds,
        IBotCreds creds,
        IHttpClientFactory httpFactory)
    {
        _client = client;
        _stats = stats;
        _cmds = cmds;
        _creds = creds;
        _httpFactory = httpFactory;
    }

    private long _savedCommandsRan = 0;
    private bool _isSavedLoaded = false;
    private readonly object _saveLock = new();

    private void LoadSavedStats()
    {
        lock (_saveLock)
        {
            if (_isSavedLoaded) return;
            try
            {
                if (File.Exists("data/widget_stats.json"))
                {
                    var json = File.ReadAllText("data/widget_stats.json");
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("commandsRan", out var prop))
                    {
                        _savedCommandsRan = prop.GetInt64();
                    }
                    if (doc.RootElement.TryGetProperty("grokBalance", out var balProp))
                    {
                        _grokBalance = balProp.GetDouble();
                    }
                    if (doc.RootElement.TryGetProperty("grokLimit", out var limProp))
                    {
                        _grokLimit = limProp.GetDouble();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load widget stats from file");
            }
            _isSavedLoaded = true;
        }
    }

    private void SaveStats(long newTotal)
    {
        lock (_saveLock)
        {
            try
            {
                var data = new
                {
                    commandsRan = newTotal,
                    grokBalance = _grokBalance,
                    grokLimit = _grokLimit
                };
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText("data/widget_stats.json", json);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to save widget stats to file");
            }
        }
    }

    public double GetGrokBalance()
    {
        LoadSavedStats();
        return _grokBalance;
    }

    public double GetGrokLimit()
    {
        LoadSavedStats();
        return _grokLimit;
    }

    public void SetGrokBalance(double balance)
    {
        LoadSavedStats();
        _grokBalance = balance;
        SaveStats(_savedCommandsRan + _stats.CommandsRan);
    }

    public void SetGrokLimit(double limit)
    {
        LoadSavedStats();
        _grokLimit = limit;
        SaveStats(_savedCommandsRan + _stats.CommandsRan);
    }

    public void DeductGrokCost(double cost)
    {
        LoadSavedStats();
        _grokBalance = Math.Max(0.0, _grokBalance - cost);
        SaveStats(_savedCommandsRan + _stats.CommandsRan);
        Log.Information("Deducted Grok cost of ${Cost:F6}. New balance: ${NewBalance:F4}", cost, _grokBalance);
    }


    /// <summary>
    /// Gets the OAuth2 authorization URL for the Social Layer scope.
    /// </summary>
    public string GetOAuth2Url()
    {
        var appId = _client.CurrentUser.Id;
        return $"https://discord.com/oauth2/authorize?client_id={appId}&response_type=token&scope=openid+sdk.social_layer";
    }

    /// <summary>
    /// Refreshes the Discord profile widget for a given user with live bot stats.
    /// </summary>
    public async Task<(bool Success, string? Error)> RefreshWidgetAsync(ulong userId)
    {
        var appId = _client.CurrentUser.Id;
        var payload = BuildPayload();

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var url = $"https://discord.com/api/v9/applications/{appId}/users/{userId}/identities/0/profile";

        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _creds.Token);
        http.DefaultRequestHeaders.Add("User-Agent",
            "DiscordBot (https://github.com/Pravix212/nihilister-bot, 1.0.0)");

        var response = await http.PatchAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            Log.Warning("Widget refresh failed ({StatusCode}): {Error}", response.StatusCode, errorText);
            return (false, $"{response.StatusCode}: {errorText}");
        }

        Log.Information("Widget refreshed for user {UserId}", userId);
        return (true, null);
    }

    /// <summary>
    /// Builds the JSON payload with live bot stats for the widget.
    /// Dynamic field names must match what you configured in the Discord Developer Portal widget editor.
    /// </summary>
    private object BuildPayload()
    {
        LoadSavedStats();

        // Calculate persistent total commands ran
        var sessionCommandsRan = _stats.CommandsRan;
        var totalCommandsRan = _savedCommandsRan + sessionCommandsRan;
        SaveStats(totalCommandsRan);

        var uptime = _stats.GetUptimeString();
        var totalCommands = _cmds.Commands.Select(c => c.Aliases[0]).Distinct().Count();
        
        // Sum total user count across all guilds
        var totalUsers = _client.Guilds.Sum(g => g.MemberCount);

        // Calculate Grok budget percentage remaining
        var grokPercent = 0;
        if (_grokLimit > 0.0)
        {
            grokPercent = (int)Math.Clamp(Math.Round((_grokBalance / _grokLimit) * 100.0), 0, 100);
        }

        // type 1 = string, type 2 = number, type 3 = image
        // Change total_commands to type = 1 string so it displays correctly on a text/custom string element
        var dynamicData = new List<object>
        {
            new { type = 1, name = "commands_ran", value = $"{totalCommandsRan:N0}" },
            new { type = 1, name = "uptime", value = uptime },
            new { type = 1, name = "total_commands", value = $"{totalCommands:N0}" },
            new { type = 1, name = "messages_seen", value = $"{totalUsers:N0}" }, // Expose user count under messages_seen so existing layouts automatically show it
            new { type = 1, name = "users", value = $"{totalUsers:N0}" },
            new { type = 1, name = "grok_balance", value = $"${_grokBalance:F2}" },
            new { type = 1, name = "grok_limit", value = $"${_grokLimit:F2}" },
            new { type = 2, name = "grok_percent", value = grokPercent },
            new { type = 1, name = "grok_percent_str", value = $"{grokPercent}%" },
            new { type = 1, name = "grok_tokens_left", value = $"{grokPercent}%" },
            new { type = 1, name = "grok_usage_left", value = $"{grokPercent}%" }
        };

        return new
        {
            username = _client.CurrentUser.Username,
            data = new
            {
                dynamic = dynamicData
            }
        };
    }

    /// <summary>
    /// Starts a background loop that auto-refreshes the widget every 5 minutes.
    /// </summary>
    public void StartAutoRefresh(ulong userId)
    {
        StopAutoRefresh();
        _autoRefreshUserId = userId;
        _autoRefreshCts = new CancellationTokenSource();
        var ct = _autoRefreshCts.Token;

        _autoRefreshTask = Task.Run(async () =>
        {
            Log.Information("Widget auto-refresh started for user {UserId} (every 5 min)", userId);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var (success, error) = await RefreshWidgetAsync(userId);
                    if (success)
                        Log.Information("Widget auto-refreshed successfully");
                    else
                        Log.Warning("Widget auto-refresh failed: {Error}", error);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Widget auto-refresh error");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            Log.Information("Widget auto-refresh stopped");
        }, ct);
    }

    /// <summary>
    /// Stops the background auto-refresh loop.
    /// </summary>
    public void StopAutoRefresh()
    {
        if (_autoRefreshCts is not null)
        {
            _autoRefreshCts.Cancel();
            _autoRefreshCts.Dispose();
            _autoRefreshCts = null;
        }

        _autoRefreshTask = null;
    }
}
