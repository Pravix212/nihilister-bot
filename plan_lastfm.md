# Last.fm Integration — Implementation Plan (Nihilister Bot)

## Goal
Port the core fmbot experience into Nihilister Bot: `.login` → `.fm` → `.topartists`/`.topalbums`/`.toptracks` → `.whoknows` → image charts → crown system.

## Architecture
- **New module:** `src/NadekoBot/Modules/LastFm/`
- **Database:** SQLite via EF Core + custom SQL migrations
- **HTTP:** `IHttpClientFactory` (existing bot pattern)
- **Caching:** `IBotCache` (existing bot pattern)
- **Strings:** `names.yml`, `cmds.yml`, `res.yml`, `res.i18n.yml` per module
- **DI:** Services implement `INService` → auto-registered by `AddLifetimeServices()`

## API Keys Needed
- `LastFmApiKey` — added to `IBotCreds` / `data/credentials.json`
- `LastFmApiSecret` — for auth token signing (optional for MVP, needed for `.login` flow)

---

## Phase 1: Foundation (Database + Service + Login + Now Playing)
**Goal:** `.login` and `.fm` work end-to-end.

### 1.1 Database Model
File: `src/NadekoBot/Db/Models/LastFmUser.cs`
- `ulong DiscordUserId`
- `string LastFmUsername`
- `string? SessionKey` (for auth actions later)
- `DateTime LinkedAt`

### 1.2 DbContext Update
File: `src/NadekoBot/Db/NadekoContext.cs`
- Add `DbSet<LastFmUser> LastFmUsers`
- Configure index on `DiscordUserId` (unique)

### 1.3 Migration SQL
File: `src/NadekoBot/Migrations/20250629000000_lastfm.sql`
```sql
CREATE TABLE IF NOT EXISTS "LastFmUsers" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DiscordUserId" INTEGER NOT NULL,
    "LastFmUsername" TEXT NOT NULL,
    "SessionKey" TEXT,
    "LinkedAt" TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_LastFmUsers_DiscordUserId" ON "LastFmUsers"("DiscordUserId");
```

### 1.4 Bot Credentials
File: `src/NadekoBot/Services/Impl/BotCreds.cs` + `IBotCreds.cs`
- Add `string LastFmApiKey { get; }`
- Add `string LastFmApiSecret { get; }`
- Update `data/credentials.json` schema + default values

### 1.5 Last.fm API Service
File: `src/NadekoBot/Modules/LastFm/LastFmService.cs`
- Implements `INService`
- Methods:
  - `GetUserInfoAsync(string username)` → `user.getInfo`
  - `GetRecentTracksAsync(string username, int limit = 2)` → `user.getRecentTracks`
  - `GetTopArtistsAsync(string username, string period = "overall", int limit = 10)` → `user.getTopArtists`
  - `GetTopAlbumsAsync(...)` → `user.getTopAlbums`
  - `GetTopTracksAsync(...)` → `user.getTopTracks`
  - `GetArtistInfoAsync(string artist)` → `artist.getInfo`
  - `GetAlbumInfoAsync(string artist, string album)` → `album.getInfo`
  - `GetTrackInfoAsync(string artist, string track)` → `track.getInfo`
- Response DTOs: `LastFmUserInfo`, `LastFmTrack`, `LastFmArtist`, `LastFmAlbum`, etc.

### 1.6 Commands
File: `src/NadekoBot/Modules/LastFm/LastFmCommands.cs`
```
.login [lastfm_username]   — Link your Last.fm account to your Discord ID
.fm [user]                 — Show your now-playing / last scrobbled track
```

### 1.7 Strings
- `src/NadekoBot/Modules/LastFm/strings/names.yml`
- `src/NadekoBot/Modules/LastFm/strings/cmds.yml`
- `src/NadekoBot/Modules/LastFm/strings/res.yml`
- `src/NadekoBot/Modules/LastFm/strings/res.i18n.yml` (all 16 locales)

### 1.8 Build & Deploy
- `dotnet build` → fix any errors → push to `v6` → deploy to droplet

---

## Phase 2: Core Stats (Top Artists, Albums, Tracks + Artist/Album/Track Info)
**Goal:** `.topartists`, `.topalbums`, `.toptracks`, `.artist`, `.album`, `.track`, `.recent`

### 2.1 Commands
Add methods to `LastFmCommands.cs`:
```
.topartists [period] [user]    — Top artists (overall | 7day | 1month | 3month | 6month | 12month)
.topalbums [period] [user]     — Top albums
.toptracks [period] [user]     — Top tracks
.artist [artist_name]           — Artist info (listeners, playcount, bio, tags)
.album [album] [artist]          — Album info
.track [track] [artist]          — Track info
.recent [user]                   — Recent scrobbles (last 10)
```

### 2.2 Service Updates
- Add parsing for all new Last.fm XML responses
- Cache results via `IBotCache` (e.g., 5 min for user stats, 1 hour for artist info)

### 2.3 Strings
- Add all new command names, descriptions, and response strings

---

## Phase 3: Social (WhoKnows + Taste + Friends)
**Goal:** Server leaderboards and taste comparison

### 3.1 Database Model
File: `src/NadekoBot/Db/Models/LastFmFriend.cs`
- `ulong UserId`
- `ulong FriendId`
- `DateTime AddedAt`

### 3.2 Commands
```
.whoknows [artist]             — Server leaderboard for an artist
.whoknowsalbum [album] [artist] — Server leaderboard for an album
.whoknowstrack [track] [artist] — Server leaderboard for a track
.taste [user]                   — Compare your top artists with another user
.friends                        — Show your friends' recent plays
.friendadd [user]               — Add a friend
.friendremove [user]            — Remove a friend
```

### 3.3 Service Updates
- For `.whoknows`: iterate over all linked users in the guild, fetch their playcount for the artist, rank them
- For `.taste`: fetch both users' top artists, calculate intersection percentage
- For `.friends`: store friend list in DB, fetch recent tracks for each

### 3.4 Migration SQL
```sql
CREATE TABLE IF NOT EXISTS "LastFmFriends" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "FriendId" INTEGER NOT NULL,
    "AddedAt" TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_LastFmFriends_UserId_FriendId" ON "LastFmFriends"("UserId", "FriendId");
```

---

## Phase 4: Visual Charts (Image Charts + Receipts + Iceberg)
**Goal:** `.chart`, `.receipt`, `.iceberg`

### 4.1 Image Generation Service
File: `src/NadekoBot/Modules/LastFm/LastFmChartService.cs`
- Uses `SkiaSharp` or `ImageSharp` (already in bot dependencies? check)
- Generates:
  - 3×3 / 5×5 album art grids from top albums
  - Receipt-style image from top tracks
  - Iceberg-style image from top artists (obscurity tiered)

### 4.2 Commands
```
.chart [size] [period]          — Top albums image chart (3x3, 4x4, 5x5)
.receipt [period]               — Receiptify-style top tracks image
.iceberg [period]                — Artist popularity iceberg image
```

---

## Phase 5: Crown System
**Goal:** Gamification — users compete for "crown" of an artist in a server

### 5.1 Database Model
File: `src/NadekoBot/Db/Models/LastFmCrown.cs`
- `ulong GuildId`
- `string ArtistName`
- `ulong CurrentHolderId`
- `int Playcount`
- `DateTime CrownedAt`

### 5.2 Commands
```
.crown [artist]                 — Show current crown holder for an artist
.crowns [user]                   — Show all crowns a user holds
.crownleaderboard                — Server crown count leaderboard
```

### 5.3 Logic
- On `.whoknows`, if the top listener has > second place by some margin, they get the crown
- Store crown in DB; if someone overtakes, crown transfers

---

## Phase 6: Server Stats + Spotify Links + Games
**Goal:** `.serverartists`, `.servertracks`, `.spotify`, `.jumble`, `.pixel`

### 6.1 Server Aggregated Stats
- Aggregate top artists/albums/tracks across all linked users in a guild

### 6.2 Spotify Links
- `.spotify [track]` — Search Spotify API for track URI
- `.youtube [track]` — Search YouTube
- `.genius [track]` — Search Genius

### 6.3 Games
- `.jumble` — Guess jumbled artist names from your top artists
- `.pixel` — Guess pixelated album covers

---

## Timeline
| Phase | Estimated Time | Complexity |
|-------|--------------|------------|
| 1     | 1–2 sessions | Medium     |
| 2     | 1 session    | Medium     |
| 3     | 1–2 sessions | High       |
| 4     | 2 sessions   | High       |
| 5     | 1 session    | Medium     |
| 6     | 2+ sessions  | High       |

## Current Phase: **Phase 1 — Foundation**
We are starting here. I'll create the files, show you the code, and explain each piece.
