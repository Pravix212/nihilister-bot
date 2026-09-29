#!/bin/bash
set -e

cd /root/nihilister-bot

# Clean any temporary editor swap files
rm -f src/Nihilister/data/.*.sw* 2>/dev/null || true

echo "==> 1. Fetching latest from GitHub branch v6..."
git fetch origin
git reset --hard origin/v6

echo "==> 2. Building and publishing to staging directory..."
rm -rf /root/bot-staging
dotnet publish src/Nihilister/Nihilister.csproj -c Release -r linux-x64 --self-contained false -o /root/bot-staging -p:UseSharedCompilation=false

echo "==> 3. Stopping service and updating binaries..."
systemctl stop nihilister-bot || true
cp -r /root/bot-staging/* /root/bot-publish/
rm -rf /root/bot-staging

echo "==> 4. Starting Nihilister Bot service..."
systemctl start nihilister-bot

sleep 3
echo "==> 5. Service Status:"
systemctl is-active nihilister-bot
echo "==> Deploy Complete!"
