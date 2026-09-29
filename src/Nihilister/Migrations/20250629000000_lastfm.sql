CREATE TABLE IF NOT EXISTS "LastFmUsers" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "DiscordUserId" INTEGER NOT NULL,
    "LastFmUsername" TEXT NOT NULL,
    "SessionKey" TEXT,
    "LinkedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_LastFmUsers_DiscordUserId" ON "LastFmUsers"("DiscordUserId");
