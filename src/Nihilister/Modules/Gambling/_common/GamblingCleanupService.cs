using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Nihilister.Db.Models;
using Nihilister.Modules.Waifus.Waifu.Db;

namespace Nihilister.Modules.Gambling;

public class GamblingCleanupService : IGamblingCleanupService, INService
{
    private readonly DbService _db;

    public GamblingCleanupService(DbService db)
    {
        _db = db;
    }
    
    public async Task DeleteWaifus()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<WaifuCycleSnapshot>().DeleteAsync();
        await ctx.GetTable<WaifuCycle>().DeleteAsync();
        await ctx.GetTable<WaifuFan>().DeleteAsync();
        await ctx.GetTable<WaifuInfo>().DeleteAsync();
    }

    public async Task DeleteWaifu(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        
        // Remove fans of this waifu
        await ctx.GetTable<WaifuFan>()
            .Where(x => x.WaifuUserId == userId)
            .DeleteAsync();
        
        // Remove this user's backing
        await ctx.GetTable<WaifuFan>()
            .Where(x => x.UserId == userId)
            .DeleteAsync();
        
        // Remove cycle snapshots
        await ctx.GetTable<WaifuCycleSnapshot>()
            .Where(x => x.WaifuUserId == userId)
            .DeleteAsync();
        
        // Remove cycles
        await ctx.GetTable<WaifuCycle>()
            .Where(x => x.WaifuUserId == userId)
            .DeleteAsync();
        
        // Remove waifu
        await ctx.GetTable<WaifuInfo>()
            .Where(x => x.UserId == userId)
            .DeleteAsync();
    }
    
    public async Task DeleteCurrency()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<DiscordUser>().UpdateAsync(_ => new DiscordUser()
        {
            CurrencyAmount = 0
        });

        await ctx.GetTable<CurrencyTransaction>().DeleteAsync();
        await ctx.GetTable<PlantedCurrency>().DeleteAsync();
        await ctx.GetTable<BankUser>().DeleteAsync();
    }
}
