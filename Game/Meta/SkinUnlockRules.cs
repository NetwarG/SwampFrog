namespace SwampFrog;

/// <summary>
/// Правила доступности скинов: открытие по рекорду и покупка за слизь.
/// Чистые функции без привязки к Godot-нодам (см. docs/META.md).
/// </summary>
public static class SkinUnlockRules
{
	/// <summary>Скин доступен: куплен, бесплатный по умолчанию или открылся по рекорду.</summary>
	public static bool IsUnlocked(SkinDefinition def, bool owned, int bestClassic, int bestSuika)
	{
		if (owned || IsFree(def))
			return true;
		// Платный скин открывается только покупкой.
		if (def.Price > 0)
			return false;
		if (def.RequiredBestClassic > 0 && bestClassic < def.RequiredBestClassic)
			return false;
		if (def.RequiredBestSuika > 0 && bestSuika < def.RequiredBestSuika)
			return false;
		return true;
	}

	/// <summary>Скин бесплатный по умолчанию: без цены и без условий.</summary>
	public static bool IsFree(SkinDefinition def) =>
		def.Price == 0 && def.RequiredBestClassic == 0 && def.RequiredBestSuika == 0;

	/// <summary>Скин можно купить по базовой цене.</summary>
	public static bool CanBuy(SkinDefinition def, int coins) => CanBuy(def, coins, def.Price);

	/// <summary>Скин можно купить по указанной цене (условие покупки обязано быть выполнимо).</summary>
	public static bool CanBuy(SkinDefinition def, int coins, int price)
	{
		return price > 0 && def.RequiredBestClassic == 0 && def.RequiredBestSuika == 0 && coins >= price;
	}
}
