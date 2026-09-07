#!/bin/bash
set -e

cd /root/nihilister-bot

# Clean any temporary editor swap files
rm -f src/NadekoBot/data/.*.sw* 2>/dev/null || true

echo "==> 1. Fetching latest from GitHub branch v6..."
git fetch origin
git reset --hard origin/v6

echo "==> 2. Publishing to /root/bot-publish..."
dotnet publish src/NadekoBot/NadekoBot.csproj -c Release -r linux-x64 --self-contained false -o /root/bot-publish -p:UseSharedCompilation=false

echo "==> 3. Restarting Nihilister Bot service..."
systemctl restart nihilister-bot

sleep 3
echo "==> 4. Status:"
systemctl status nihilister-bot --no-pager | head -n 12
