# === CONFIG ===
$publishPath = "C:\Users\Prav2\Publish\Nihilister-Linux"
$dropletIp = "YOUR_DROPLET_IP"
$botUser = "root"
$remotePath = "/root/bot-publish"
# ==============

# 1. Publish for Linux
Write-Host "Publishing for Linux..."
dotnet publish src/NadekoBot/NadekoBot.csproj -c Release -r linux-x64 --self-contained false -o $publishPath

# 2. Copy files to droplet
Write-Host "Uploading to droplet..."
scp -r "$publishPath\*" "$botUser@${dropletIp}:$remotePath"

# 3. Restart the bot on the server
Write-Host "Restarting bot..."
ssh $botUser@$dropletIp "systemctl restart nihilister-bot && systemctl status nihilister-bot --no-pager"

Write-Host "Done! Bot deployed."
