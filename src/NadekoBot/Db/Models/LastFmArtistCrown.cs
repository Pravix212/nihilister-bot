using System;
using System.ComponentModel.DataAnnotations;

namespace NadekoBot.Db.Models;

public class LastFmArtistCrown
{
    [Key]
    public int Id { get; set; }
    
    public ulong GuildId { get; set; }
    
    public string ArtistName { get; set; }
    
    public ulong DiscordUserId { get; set; }
    
    public int Playcount { get; set; }
    
    public DateTime ClaimedAt { get; set; }
}
