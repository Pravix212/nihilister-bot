# === CONFIG ===
$publishPath = "C:\Users\Prav2\Publish\Nihilister-Linux"
$dropletIp = "157.230.120.222"
$botUser = "root"
$remotePath = "/root/bot-publish"
# ==============

# NOTE: We build ON the droplet because the Windows .NET 10 SDK has a NuGet bug.
# This script pushes your code to GitHub, then triggers a rebuild on the server.

# 1. Stage and push local changes
Write-Host "Committing and pushing changes..."
Set-Location "C:\Users\Prav2\AppData\Roaming\NadekoHub\Bots\Nihilister Clone"
$msg = Read-Host "Enter commit message (or press Enter for 'Update bot')"
if ([string]::IsNullOrWhiteSpace($msg)) { $msg = "Update bot" }

git add .
git commit -m "$msg" 2>$null
git push origin v6

# 2. Trigger remote build + restart
Write-Host "Pulling latest code and rebuilding on droplet..."
$sshCommand = @"
cd /root/nihilister-bot && git pull origin v6 && dotnet publish src/NadekoBot/NadekoBot.csproj -c Release -r linux-x64 --self-contained false -o /root/bot-publish -p:UseSharedCompilation=false && systemctl restart nihilister-bot && systemctl status nihilister-bot --no-pager
"@

ssh "${botUser}@${dropletIp}" "$sshCommand"

Write-Host "Done! Bot deployed and restarted."
Write-Host "Tip: If you only changed config files (creds.yml, ai-agent.yml), just SCP them instead of a full rebuild."
