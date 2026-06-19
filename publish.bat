@echo off
cd /d "C:\Users\Prav2\AppData\Roaming\NadekoHub\Bots\Nihilister Clone"
echo Publishing Nihilister bot for Linux...
"C:\Program Files\dotnet\dotnet.exe" publish "src\NadekoBot\NadekoBot.csproj" -c Release -r linux-x64 --self-contained false -o "C:\Users\Prav2\Publish\Nihilister-Linux"
echo Exit code: %ERRORLEVEL%
