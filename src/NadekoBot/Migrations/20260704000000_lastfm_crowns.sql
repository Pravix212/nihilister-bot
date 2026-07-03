CREATE TABLE IF NOT EXISTS "LastFmArtistCrowns" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LastFmArtistCrowns" PRIMARY KEY AUTOINCREMENT,
    "GuildId" INTEGER NOT NULL,
    "ArtistName" TEXT NOT NULL,
    "DiscordUserId" INTEGER NOT NULL,
    "Playcount" INTEGER NOT NULL,
    "ClaimedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_LastFmArtistCrowns_GuildId_ArtistName"
    ON "LastFmArtistCrowns" ("GuildId", "ArtistName");
