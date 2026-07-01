using System;
using System.ComponentModel.DataAnnotations;

namespace NadekoBot.Db.Models;

public class MarriageInfo
{
    [Key]
    public int Id { get; set; }
    public ulong User1 { get; set; }
    public ulong User2 { get; set; }
    public DateTime MarriedAt { get; set; }
    public ulong ProposedBy { get; set; }
}
