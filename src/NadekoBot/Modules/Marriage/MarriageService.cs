using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Marriage;

public record ExpiredProposal(ulong ProposerId, ulong TargetId, string Type);

public class MarriageService
{
    private readonly DbService _db;
    private static readonly TimeSpan PROPOSAL_TIMEOUT = TimeSpan.FromMinutes(1);

    public MarriageService(DbService db)
    {
        _db = db;
    }

    public async Task<List<ExpiredProposal>> CleanupExpiredProposals()
    {
        var result = new List<ExpiredProposal>();
        
        await using var ctx = _db.GetDbContext();
        var cutoff = DateTime.UtcNow - PROPOSAL_TIMEOUT;
        var cutoffStr = cutoff.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Use raw SQL for SQLite datetime comparison
        var expiredMarriage = await ctx.MarriageProposals
            .FromSqlRaw("SELECT * FROM \"MarriageProposals\" WHERE datetime(\"CreatedAt\") < datetime({0})", cutoffStr)
            .ToListAsync();
        foreach (var p in expiredMarriage)
            result.Add(new ExpiredProposal(p.ProposerId, p.TargetId, "marriage"));
        ctx.MarriageProposals.RemoveRange(expiredMarriage);
        
        var expiredAdoption = await ctx.AdoptionProposals
            .FromSqlRaw("SELECT * FROM \"AdoptionProposals\" WHERE datetime(\"CreatedAt\") < datetime({0})", cutoffStr)
            .ToListAsync();
        foreach (var p in expiredAdoption)
            result.Add(new ExpiredProposal(p.ProposerId, p.TargetId, "adoption"));
        ctx.AdoptionProposals.RemoveRange(expiredAdoption);
        
        await ctx.SaveChangesAsync();
        return result;
    }

    public async Task<bool> IsMarriedAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Marriages.AnyAsync(m => m.User1 == userId || m.User2 == userId);
    }

    public async Task<MarriageInfo?> GetMarriageAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Marriages
            .FirstOrDefaultAsync(m => m.User1 == userId || m.User2 == userId);
    }

    public async Task<ulong?> GetSpouseAsync(ulong userId)
    {
        var marriage = await GetMarriageAsync(userId);
        if (marriage == null) return null;
        return marriage.User1 == userId ? marriage.User2 : marriage.User1;
    }

    public async Task<ulong?> GetPendingProposalAsync(ulong userId)
    {
        await CleanupExpiredProposals();
        await using var ctx = _db.GetDbContext();
        var proposal = await ctx.MarriageProposals.FirstOrDefaultAsync(p => p.TargetId == userId);
        return proposal?.ProposerId;
    }

    public async Task ProposeAsync(ulong proposer, ulong target)
    {
        await CleanupExpiredProposals();
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.MarriageProposals.FirstOrDefaultAsync(p => p.TargetId == target);
        if (existing != null)
        {
            existing.ProposerId = proposer;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            ctx.MarriageProposals.Add(new MarriagePending { TargetId = target, ProposerId = proposer, CreatedAt = DateTime.UtcNow });
        }
        await ctx.SaveChangesAsync();
    }

    public async Task<bool> TryAcceptAsync(ulong target, ulong proposer)
    {
        await CleanupExpiredProposals();
        await using var ctx = _db.GetDbContext();
        var proposal = await ctx.MarriageProposals.FirstOrDefaultAsync(p => p.TargetId == target && p.ProposerId == proposer);
        if (proposal == null) return false;

        ctx.MarriageProposals.Remove(proposal);

        var marriage = new MarriageInfo
        {
            User1 = proposer,
            User2 = target,
            MarriedAt = DateTime.UtcNow,
            ProposedBy = proposer
        };
        ctx.Marriages.Add(marriage);
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DivorceAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var marriage = await ctx.Marriages.FirstOrDefaultAsync(m => m.User1 == userId || m.User2 == userId);
        if (marriage == null) return false;

        ctx.Marriages.Remove(marriage);
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<TimeSpan?> GetDurationAsync(ulong userId)
    {
        var marriage = await GetMarriageAsync(userId);
        return marriage == null ? null : DateTime.UtcNow - marriage.MarriedAt;
    }

    // Adoption methods
    public async Task<bool> IsAdoptedAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Adoptions.AnyAsync(a => a.UserId == userId);
    }

    public async Task<AdoptionInfo?> GetAdoptionAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Adoptions.FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<List<ulong>> GetChildrenAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var adoptions = await ctx.Adoptions.Where(a => a.Parent1Id == userId || a.Parent2Id == userId).ToListAsync();
        return adoptions.Select(a => a.UserId).ToList();
    }

    public async Task<List<ulong>> GetSiblingsAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var myAdoption = await ctx.Adoptions.FirstOrDefaultAsync(a => a.UserId == userId);
        if (myAdoption == null) return new List<ulong>();

        var siblings = await ctx.Adoptions
            .Where(a => a.Parent1Id == myAdoption.Parent1Id && a.UserId != userId)
            .Select(a => a.UserId)
            .ToListAsync();
        return siblings;
    }

    public async Task<ulong?> GetAdoptionProposalAsync(ulong userId)
    {
        await CleanupExpiredProposals();
        await using var ctx = _db.GetDbContext();
        var proposal = await ctx.AdoptionProposals.FirstOrDefaultAsync(p => p.TargetId == userId);
        return proposal?.ProposerId;
    }

    public async Task AdoptAsync(ulong parent, ulong child)
    {
        await CleanupExpiredProposals();
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.AdoptionProposals.FirstOrDefaultAsync(p => p.TargetId == child);
        if (existing != null)
        {
            existing.ProposerId = parent;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            ctx.AdoptionProposals.Add(new AdoptionPending { TargetId = child, ProposerId = parent, CreatedAt = DateTime.UtcNow });
        }
        await ctx.SaveChangesAsync();
    }

    public async Task<bool> AcceptAdoptionAsync(ulong adopter, ulong target)
    {
        await CleanupExpiredProposals();
        await using var ctx = _db.GetDbContext();
        var proposal = await ctx.AdoptionProposals.FirstOrDefaultAsync(p => p.TargetId == target && p.ProposerId == adopter);
        if (proposal == null) return false;

        ctx.AdoptionProposals.Remove(proposal);

        // Check if adopter is married - if so, the spouse is also a parent
        var adopterMarriage = await ctx.Marriages.FirstOrDefaultAsync(m => m.User1 == adopter || m.User2 == adopter);
        ulong parent2Id = 0;
        if (adopterMarriage != null)
        {
            parent2Id = adopterMarriage.User1 == adopter ? adopterMarriage.User2 : adopterMarriage.User1;
        }

        var adoption = new AdoptionInfo
        {
            UserId = target,
            Parent1Id = adopter,
            Parent2Id = parent2Id,
            AdoptedAt = DateTime.UtcNow
        };
        ctx.Adoptions.Add(adoption);
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisownAsync(ulong parent, ulong child)
    {
        await using var ctx = _db.GetDbContext();
        var adoption = await ctx.Adoptions.FirstOrDefaultAsync(a => a.UserId == child && (a.Parent1Id == parent || a.Parent2Id == parent));
        if (adoption == null) return false;

        ctx.Adoptions.Remove(adoption);
        await ctx.SaveChangesAsync();
        return true;
    }
}
