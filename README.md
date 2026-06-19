# Nihilister

[![CI/CD](https://github.com/Pravix212/nihilister-bot/actions/workflows/ci.yml/badge.svg)](https://github.com/Pravix212/nihilister-bot/actions/workflows/ci.yml)

Nihilister is a powerful Discord bot forked from [NadekoBot](https://github.com/nadeko-bot/nadekobot). Built on .NET 9 with a custom AI agent powered by Grok, advanced gambling systems, moderation tools, and more.

This is a personal fork maintained by Pravix212, with custom features, branding, and improvements.

## Features

- **AI Agent** — Powered by Grok for intelligent Discord conversations
- **Gambling & Economy** — Custom currency system with slots, betting, leaderboards
- **Moderation** — Advanced admin tools, auto-moderation, greet/bye messages
- **Music** — YouTube playback with queue management
- **Utility** — Custom commands, aliases, XP system, and more
- **Uptime Display** — Live status showing bot uptime

## Installation

### Self-Hosting

1. Clone the repo:
   ```bash
   git clone https://github.com/Pravix212/nihilister-bot.git
   cd nihilister-bot
   ```

2. Install .NET 9 SDK

3. Configure `src/NadekoBot/data/creds.yml` and `src/NadekoBot/data/ai-agent.yml`

4. Build and run:
   ```bash
   cd src/NadekoBot
   dotnet run
   ```

### Linux VPS (Recommended for 24/7)

See [NadekoBot Linux Guide](https://docs.nadeko.bot/guides/linux-guide) for server setup instructions.

### Docker

```bash
docker run -d --name nihilister ghcr.io/Pravix212/nihilister-bot:v6   -e bot_token=YOUR_TOKEN_HERE   -v "./data:/app/data"   && docker logs -f --tail 500 nihilister
```

## Original Project

Nihilister is a fork of [NadekoBot](https://github.com/nadeko-bot/nadekobot) by Kwoth. The original project is licensed under AGPL-3.0. This fork maintains compliance with that license while adding custom features and branding.

## Support

For issues, feature requests, or donations, visit [nihilister.bot](https://nihilister.bot) (coming soon).

---

**Nihilister** — *Not just a bot. An experience.*
