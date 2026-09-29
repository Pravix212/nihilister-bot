namespace Nihilister.Modules.Waifus.Waifu;

/// <summary>
/// Type of gift item effect.
/// </summary>
public enum GiftItemType
{
    /// <summary>
    /// Increases Food stat.
    /// </summary>
    Food,

    /// <summary>
    /// Increases Mood stat.
    /// </summary>
    Mood
}

/// <summary>
/// Represents a gift item that can be given to a waifu.
/// </summary>
/// <param name="Id">Unique identifier for the item.</param>
/// <param name="Emoji">Emoji representation of the item.</param>
/// <param name="Name">Display name of the item.</param>
/// <param name="Price">Cost in currency to purchase.</param>
/// <param name="Type">Whether this item affects Food or Mood.</param>
/// <param name="Effect">Amount to increase the stat by.</param>
public sealed record WaifuGiftItem(
    Guid Id,
    string Emoji,
    string Name,
    long Price,
    GiftItemType Type,
    int Effect
);

/// <summary>
/// Provides access to gift items and daily rotation logic.
/// </summary>
public static class WaifuGiftItems
{
    private static readonly DateTime Epoch = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Legacy item used for migration-only display. Not purchasable.
    /// </summary>
    public static readonly WaifuGiftItem LegacyItem =
        new(Guid.Parse("019479a1-ffff-7000-8000-ffffffffffff"), "📦", "Legacy", 0, GiftItemType.Mood, 0);

    /// <summary>
    /// Converts config items to WaifuGiftItem records.
    /// </summary>
    public static IReadOnlyList<WaifuGiftItem> FromConfig(IReadOnlyList<WaifuGiftItemConfig> configItems)
        => configItems.Select(c => new WaifuGiftItem(c.Id, c.Emoji, c.Name, c.Price, c.Type, c.Effect)).ToList();

    /// <summary>
    /// Gets the 6 items available today (3 food, 3 mood - one per price tier).
    /// </summary>
    public static IReadOnlyList<WaifuGiftItem> GetTodaysItems(IReadOnlyList<WaifuGiftItem> allItems)
    {
        var daysSinceEpoch = (int)(DateTime.UtcNow.Date - Epoch).TotalDays;
        var priceTiers = allItems
            .Where(x => x.Price > 0)
            .Select(x => x.Price)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var result = new List<WaifuGiftItem>(priceTiers.Count);

        for (var tierIndex = 0; tierIndex < priceTiers.Count; tierIndex++)
        {
            var price = priceTiers[tierIndex];

            var foodItems = allItems.Where(x => x.Price == price && x.Type == GiftItemType.Food).ToList();
            var moodItems = allItems.Where(x => x.Price == price && x.Type == GiftItemType.Mood).ToList();

            var isFood = (tierIndex + daysSinceEpoch) % 2 == 0;

            if (isFood && foodItems.Count > 0)
            {
                var variant = (daysSinceEpoch / priceTiers.Count) % foodItems.Count;
                result.Add(foodItems[variant]);
            }
            else if (moodItems.Count > 0)
            {
                var variant = (daysSinceEpoch / priceTiers.Count) % moodItems.Count;
                result.Add(moodItems[variant]);
            }
            else if (foodItems.Count > 0)
            {
                var variant = (daysSinceEpoch / priceTiers.Count) % foodItems.Count;
                result.Add(foodItems[variant]);
            }
        }

        return result.OrderBy(x => x.Price).ToList();
    }

    /// <summary>
    /// Gets the time remaining until the shop refreshes (next 00:00 UTC).
    /// </summary>
    public static TimeSpan GetTimeUntilRefresh()
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        return nextMidnight - now;
    }

    /// <summary>
    /// Finds an item by name from today's available items.
    /// </summary>
    public static WaifuGiftItem? FindTodaysItem(IReadOnlyList<WaifuGiftItem> allItems, string name)
    {
        var todaysItems = GetTodaysItems(allItems);
        return todaysItems.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds any item by ID across all items (for display purposes).
    /// </summary>
    public static WaifuGiftItem? FindItemById(IReadOnlyList<WaifuGiftItem> allItems, Guid id)
    {
        if (id == LegacyItem.Id)
            return LegacyItem;

        return allItems.FirstOrDefault(x => x.Id == id);
    }
}
