using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>Пункт экрана улучшений: что показывает карточка.</summary>
public sealed record UpgradeEntry(
	UpgradeId Id,
	string Name,
	string Effect,
	int Level,
	int MaxLevel,
	string IconPath,
	int Price,
	bool Affordable,
	bool Maxed);

/// <summary>Собирает пункты экрана улучшений из каталога и профиля.</summary>
public static class UpgradeState
{
	public static UpgradeEntry[] Build(MetaProfile profile)
	{
		var result = new List<UpgradeEntry>();
		int coins = profile.Wallet.Balance;
		foreach (UpgradeDefinition def in UpgradeCatalog.All)
		{
			int level = profile.UpgradeLevel(def.Id);
			bool maxed = level >= def.MaxLevel;
			int price = maxed ? 0 : def.PriceFor(level + 1);
			result.Add(new UpgradeEntry(def.Id, def.Name, def.EffectText(maxed ? level : level + 1),
				level, def.MaxLevel, def.IconPath, price, !maxed && coins >= price, maxed));
		}
		return result.ToArray();
	}
}
