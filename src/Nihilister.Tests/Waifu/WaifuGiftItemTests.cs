#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Nihilister.Modules.Waifus.Waifu;
using NUnit.Framework;

namespace Nihilister.Tests.Waifu;

[TestFixture]
public class WaifuGiftItemTests
{
    private static readonly IReadOnlyList<WaifuGiftItem> _items =
        WaifuGiftItems.FromConfig(new WaifuConfig().Items);

    [Test]
    public void AllItems_CorrectStructure_24PurchasableItems_6Tiers()
    {
        var purchasable = _items.Where(x => x.Price > 0).ToList();
        Assert.That(purchasable, Has.Count.EqualTo(24));
        Assert.That(purchasable.Count(x => x.Type == GiftItemType.Food), Is.EqualTo(12));
        Assert.That(purchasable.Count(x => x.Type == GiftItemType.Mood), Is.EqualTo(12));

        long[] tiers = [10, 50, 250, 1000, 2500, 5000];
        var distinctPrices = purchasable.Select(x => x.Price).Distinct().OrderBy(x => x).ToList();
        Assert.That(distinctPrices, Is.EqualTo(tiers));

        foreach (var price in tiers)
        {
            var items = purchasable.Where(x => x.Price == price).ToList();
            Assert.That(items, Has.Count.EqualTo(4), $"Expected 4 items at tier {price}");
        }

        // Legacy item exists
        var legacy = WaifuGiftItems.LegacyItem;
        Assert.That(legacy, Is.Not.Null);
        Assert.That(legacy.Name, Is.EqualTo("Legacy"));
    }

    [Test]
    public void GetTodaysItems_6Items_3FoodAnd3Mood_AllTiers()
    {
        var today = WaifuGiftItems.GetTodaysItems(_items);
        Assert.That(today, Has.Count.EqualTo(6));
        Assert.That(today.Count(x => x.Type == GiftItemType.Food), Is.EqualTo(3));
        Assert.That(today.Count(x => x.Type == GiftItemType.Mood), Is.EqualTo(3));

        var prices = today.Select(x => x.Price).OrderBy(x => x).ToList();
        Assert.That(prices, Is.EqualTo(new long[] { 10, 50, 250, 1000, 2500, 5000 }));

        // Deterministic
        var second = WaifuGiftItems.GetTodaysItems(_items);
        Assert.That(today.Select(x => x.Id), Is.EqualTo(second.Select(x => x.Id)));
    }

    [Test]
    public void FindTodaysItem_CaseInsensitive_ReturnsNullForMissing()
    {
        var first = WaifuGiftItems.GetTodaysItems(_items)[0];

        Assert.That(WaifuGiftItems.FindTodaysItem(_items, first.Name.ToUpperInvariant())?.Id, Is.EqualTo(first.Id));
        Assert.That(WaifuGiftItems.FindTodaysItem(_items, "NonExistentItemXYZ"), Is.Null);
        Assert.That(WaifuGiftItems.FindItemById(_items, Guid.NewGuid()), Is.Null);
    }
}
