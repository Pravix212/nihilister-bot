using System;
using System.ComponentModel.DataAnnotations;

namespace NadekoBot.Db.Models;

public class LastFmUser
{
    [Key]
    public int Id { get; set; }
    
    public ulong DiscordUserId { get; set; }
    
    public string LastFmUsername { get; set; }
    
    public string? SessionKey { get; set; }
    
    public DateTime LinkedAt { get; set; }
}
