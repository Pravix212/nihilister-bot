using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    public partial class VideoDownloadCommands : NadekoModule
    {
        private const string DOWNLOAD_DIR = "/tmp/nihilister-videos";
        private const string COOKIES_PATH = "/root/bot-dev/output/data/youtube-cookies.txt";

        private static async Task SafeDeleteAsync(IUserMessage? msg)
        {
            if (msg == null) return;
            try { await msg.DeleteAsync(); } catch { /* message already deleted */ }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Mp4([Leftover] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await Response().Error("Please provide a video URL. Example: .mp4 https://youtube.com/watch?v=... or .mp4 https://tiktok.com/...").SendAsync();
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                await Response().Error("That doesn't look like a valid URL.").SendAsync();
                return;
            }

            var statusMsg = await Response().Pending("Downloading video... This may take a moment.").SendAsync();

            try
            {
                Directory.CreateDirectory(DOWNLOAD_DIR);
                var fileId = Guid.NewGuid().ToString("N")[..8];
                var outputPath = Path.Combine(DOWNLOAD_DIR, $"{fileId}.mp4");

                var (success, ytError) = await DownloadYouTubeVideo(url, outputPath);
                if (!success)
                {
                    await SafeDeleteAsync(statusMsg);
                    
                    if (ytError.Contains("Sign in to confirm") || ytError.Contains("bot"))
                    {
                        await Response().Error("YouTube is blocking this video (IP-based bot detection). This is a known limitation — the droplet IP is blocked by YouTube. Try a different platform (TikTok, X/Twitter, Reddit, etc.).").SendAsync();
                    }
                    else
                    {
                        await Response().Error("Failed to download the video. The URL might be invalid, the video is unavailable, or the platform isn't supported.").SendAsync();
                    }
                    return;
                }

                var fileInfo = new FileInfo(outputPath);
                if (!fileInfo.Exists)
                {
                    await SafeDeleteAsync(statusMsg);
                    await Response().Error("Download failed — no file was created.").SendAsync();
                    return;
                }

                await SafeDeleteAsync(statusMsg);
                
                try
                {
                    using var fileStream = File.OpenRead(outputPath);
                    await ctx.Channel.SendFileAsync(fileStream, $"{fileId}.mp4");
                }
                catch (Exception uploadEx) when (uploadEx.Message.Contains("too large") || uploadEx.Message.Contains("Request entity too large") || uploadEx.Message.Contains("413") || uploadEx.Message.Contains("size"))
                {
                    await Response().Error($"The video is too large ({fileInfo.Length / 1024 / 1024}MB). Discord rejected the upload. Check your server boost settings for the current upload limit.").SendAsync();
                }
                catch (Exception uploadEx)
                {
                    Log.Warning("Video upload failed: {Message}", uploadEx.Message);
                    await Response().Error($"Video downloaded but upload failed: {uploadEx.Message}").SendAsync();
                }
                finally
                {
                    try { File.Delete(outputPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                await SafeDeleteAsync(statusMsg);
                await Response().Error($"Something went wrong: {ex.Message}").SendAsync();
                Log.Warning("Video download failed: {Message}", ex.Message);
            }
        }

        private static async Task<(bool Success, string Error)> DownloadYouTubeVideo(string url, string outputPath)
        {
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            
            var cookiesArg = File.Exists(COOKIES_PATH) ? $"--cookies \"{COOKIES_PATH}\" " : "";
            
            // Node.js v22+ is required for yt-dlp's JS challenge solver (YouTube)
            var jsRtsArg = IsNodeAvailable() ? "--js-runtimes node " : "";
            
            // Impersonation helps bypass bot detection on TikTok, X, Instagram, etc.
            var impersonateArg = IsCurlCffiAvailable() ? "--impersonate chrome " : "";
            
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"{impersonateArg}{jsRtsArg}--no-playlist --user-agent \"{userAgent}\" {cookiesArg}-o \"{outputPath}\" \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return (false, "Failed to start yt-dlp process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Log.Warning("yt-dlp failed: {Error}", error);
                return (false, error);
            }

            return (File.Exists(outputPath), error);
        }

        private static bool IsNodeAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                proc.WaitForExit(5000);
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCurlCffiAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = "-c \"import curl_cffi; print(curl_cffi.__version__)\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                proc.WaitForExit(5000);
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
