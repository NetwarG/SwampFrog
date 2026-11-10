using Godot;

namespace SwampFrog;

/// <summary>
/// Числовые эффекты постоянных улучшений: чистые функции от уровней профиля,
/// поэтому баланс правится в одном месте (см. docs/META.md).
/// </summary>
public static class MetaUpgradeEffects
{
	/// <summary>Слизь за забег и Suika: +5% за уровень «Копилки».</summary>
	public static float SlimeMultiplier(int level) => 1f + 0.05f * level;
	public static float SlimeMultiplier(MetaProfile profile) => SlimeMultiplier(profile.UpgradeLevel(UpgradeId.Wallet));

	/// <summary>Шанс двойного фрукта в запас: +5% за уровень «Запасливости».</summary>
	public static float StockDoubleChance(int level) => 0.05f * level;
	public static float StockDoubleChance(MetaProfile profile) => StockDoubleChance(profile.UpgradeLevel(UpgradeId.Supply));

	/// <summary>Скидка на скины: −5% за уровень «Торговца».</summary>
	public static float ShopDiscount(int level) => 0.05f * level;
	public static float ShopDiscount(MetaProfile profile) => ShopDiscount(profile.UpgradeLevel(UpgradeId.Merchant));

	/// <summary>Множитель скорости падения предметов: −3% за уровень «Замедления».</summary>
	public static float FallSpeedMultiplier(int level) => 1f - 0.03f * level;
	public static float FallSpeedMultiplier(MetaProfile profile) => FallSpeedMultiplier(profile.UpgradeLevel(UpgradeId.Slow));

	/// <summary>Дополнительные жизни на старте забега.</summary>
	public static int StartLivesBonus(int level) => level;
	public static int StartLivesBonus(MetaProfile profile) => StartLivesBonus(profile.UpgradeLevel(UpgradeId.Vitality));

	/// <summary>Рероллы предложения перков за забег.</summary>
	public static int PerkRerolls(int level) => level;
	public static int PerkRerolls(MetaProfile profile) => PerkRerolls(profile.UpgradeLevel(UpgradeId.Reroll));

	/// <summary>Цена скина с учётом скидки «Торговца».</summary>
	public static int SkinPrice(SkinDefinition def, MetaProfile profile)
	{
		if (def.Price <= 0)
			return def.Price;
		return Mathf.Max(1, (int)Mathf.Round(def.Price * (1f - ShopDiscount(profile))));
	}
}
