using Library;
using Library.SystemModels;
using MirDB;
using System;
using System.Linq;

namespace Server.Envir;

public static class SystemDataBootstrapper
{
    public static bool Apply(Session session)
    {
        bool changed = EnsureCurrencies(session);
        changed |= MigrateMapStats(session);
        if (changed) session.Save(true);
        return changed;
    }

    private static bool EnsureCurrencies(Session session)
    {
        bool changed = false;
        DBCollection<ItemInfo> items = session.GetCollection<ItemInfo>();
        DBCollection<CurrencyInfo> currencies = session.GetCollection<CurrencyInfo>();

        ItemInfo EnsureCurrencyItem(CurrencyType currencyType, string name, int image, bool canDrop)
        {
            ItemInfo item = currencies.Binding.FirstOrDefault(value => value.Type == currencyType)?.DropItem
                            ?? items.Binding.FirstOrDefault(value => value.ItemName == name);
            if (item is null)
            {
                item = items.CreateNewObject();
                item.ItemName = name;
                item.StackSize = 25000;
                item.Image = image;
                item.SellRate = 0;
                item.CanDrop = canDrop;
                changed = true;
            }
            if (item.ItemType != ItemType.Currency) { item.ItemType = ItemType.Currency; changed = true; }
            return item;
        }

        CurrencyInfo EnsureCurrency(CurrencyType type, string name, string abbreviation, CurrencyCategory category, ItemInfo dropItem = null)
        {
            CurrencyInfo currency = currencies.Binding.FirstOrDefault(value => value.Type == type);
            if (currency is null)
            {
                currency = currencies.CreateNewObject();
                currency.Type = type;
                currency.Name = name;
                currency.Category = category;
                changed = true;
            }
            if (string.IsNullOrEmpty(currency.Abbreviation)) { currency.Abbreviation = abbreviation; changed = true; }
            if (dropItem is not null && currency.DropItem is null) { currency.DropItem = dropItem; changed = true; }
            return currency;
        }

        ItemInfo goldItem = EnsureCurrencyItem(CurrencyType.Gold, "Gold", 121, true);
        CurrencyInfo gold = EnsureCurrency(CurrencyType.Gold, "Gold", "Gold", CurrencyCategory.Basic, goldItem);
        if (gold.Images.Count == 0)
        {
            int[] amounts = [0, 100, 200, 500, 1000, 1000000, 5000000, 10000000];
            for (int index = 0; index < amounts.Length; index++)
            {
                CurrencyInfoImage image = session.GetCollection<CurrencyInfoImage>().CreateNewObject();
                image.Image = 120 + index;
                image.Amount = amounts[index];
                gold.Images.Add(image);
            }
            changed = true;
        }

        EnsureCurrency(CurrencyType.GameGold, "Game Gold", "GG", CurrencyCategory.Other);
        EnsureCurrency(CurrencyType.HuntGold, "Hunt Gold", "HG", CurrencyCategory.Other);
        EnsureCurrency(CurrencyType.FP, "Fame Point", "FP", CurrencyCategory.Player,
            EnsureCurrencyItem(CurrencyType.FP, "Fame Point", 4010, false));
        EnsureCurrency(CurrencyType.CP, "Contribution Point", "CP", CurrencyCategory.Player,
            EnsureCurrencyItem(CurrencyType.CP, "Contribution Point", 4012, false));

        foreach (CurrencyInfo currency in currencies.Binding)
            if (currency.DropItem is not null && currency.DropItem.ItemType != ItemType.Currency)
            {
                currency.DropItem.ItemType = ItemType.Currency;
                changed = true;
            }
        return changed;
    }

    private static bool MigrateMapStats(Session session)
    {
        bool changed = false;
        foreach (MapInfo map in session.GetCollection<MapInfo>().Binding)
        {
            changed |= Migrate(map, nameof(MapInfo.MonsterHealth), map.MonsterHealth, Stat.MonsterHealth, value => map.MonsterHealth = value);
            changed |= Migrate(map, nameof(MapInfo.MonsterDamage), map.MonsterDamage, Stat.MonsterDamage, value => map.MonsterDamage = value);
            changed |= Migrate(map, nameof(MapInfo.DropRate), map.DropRate, Stat.MonsterDrop, value => map.DropRate = value);
            changed |= Migrate(map, nameof(MapInfo.ExperienceRate), map.ExperienceRate, Stat.MonsterExperience, value => map.ExperienceRate = value);
            changed |= Migrate(map, nameof(MapInfo.GoldRate), map.GoldRate, Stat.MonsterGold, value => map.GoldRate = value);
            changed |= Migrate(map, nameof(MapInfo.MaxMonsterHealth), map.MaxMonsterHealth, Stat.MaxMonsterHealth, value => map.MaxMonsterHealth = value);
            changed |= Migrate(map, nameof(MapInfo.MaxMonsterDamage), map.MaxMonsterDamage, Stat.MaxMonsterDamage, value => map.MaxMonsterDamage = value);
            changed |= Migrate(map, nameof(MapInfo.MaxDropRate), map.MaxDropRate, Stat.MaxMonsterDrop, value => map.MaxDropRate = value);
            changed |= Migrate(map, nameof(MapInfo.MaxExperienceRate), map.MaxExperienceRate, Stat.MaxMonsterExperience, value => map.MaxExperienceRate = value);
            changed |= Migrate(map, nameof(MapInfo.MaxGoldRate), map.MaxGoldRate, Stat.MaxMonsterGold, value => map.MaxGoldRate = value);
        }
        return changed;

        bool Migrate(MapInfo map, string _, int amount, Stat stat, Action<int> clear)
        {
            if (amount == 0) return false;
            MapInfoStat value = session.GetCollection<MapInfoStat>().CreateNewObject();
            value.Stat = stat;
            value.Amount = amount;
            map.BuffStats.Add(value);
            clear(0);
            return true;
        }
    }
}
