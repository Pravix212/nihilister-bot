using System;
using System.ComponentModel.DataAnnotations;

namespace NadekoBot.Db.Models;

public class AdoptionInfo
{
    [Key]
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public ulong Parent1Id { get; set; }
    public ulong Parent2Id { get; set; }
    public DateTime AdoptedAt { get; set; }
}
