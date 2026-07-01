using System;
using System.ComponentModel.DataAnnotations;

namespace NadekoBot.Db.Models;

public class MarriagePending
{
    [Key]
    public int Id { get; set; }
    public ulong TargetId { get; set; }
    public ulong ProposerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
