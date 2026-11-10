using System.Linq;

namespace SwampFrog;

/// <summary>Каталог постоянных улучшений (данные).</summary>
public sealed class UpgradeCatalog
{
	public static UpgradeDefinition[] All =
	{
		new(UpgradeId.Wallet, UpgradeKind.SlimeGain, "Копилка", "Savings", 20, "upgrade_wallet",
			new[] { "+5% слизи за забег и Suika", "+10% слизи", "+15% слизи", "+20% слизи" },
			new[] { "+5% slime per run and Suika", "+10% slime", "+15% slime", "+20% slime" }),

		new(UpgradeId.Supply, UpgradeKind.SuikaStock, "Запасливость", "Supply", 20, "upgrade_supply",
			new[] { "+5% шанс двойного фрукта в запас", "+10% шанс двойного фрукта", "+15% шанс двойного фрукта" },
			new[] { "+5% chance of a double fruit into stock", "+10% double fruit chance", "+15% double fruit chance" }),

		new(UpgradeId.Merchant, UpgradeKind.ShopDiscount, "Торговец", "Merchant", 25, "upgrade_merchant",
			new[] { "−5% к цене скинов", "−10% к цене скинов" },
			new[] { "−5% on skin prices", "−10% on skin prices" }),

		new(UpgradeId.Slow, UpgradeKind.SlowFalling, "Замедление", "Slow-mo", 30, "upgrade_slow",
			new[] { "Предметы падают на 3% медленнее", "…на 6% медленнее", "…на 9% медленнее" },
			new[] { "Items fall 3% slower", "Items fall 6% slower", "Items fall 9% slower" }),

		new(UpgradeId.Vitality, UpgradeKind.StartLives, "Запас сил", "Vitality", 60, "upgrade_vitality",
			new[] { "+1 жизнь на старте забега" },
			new[] { "+1 life at the start of a run" }),

		new(UpgradeId.Reroll, UpgradeKind.PerkReroll, "Реролл", "Reroll", 40, "upgrade_reroll",
			new[] { "+1 реролл предложения перков за забег", "+2 реролла предложения перков за забег" },
			new[] { "+1 perk offer reroll per run", "+2 perk offer rerolls per run" }),
	};

	/// <summary>Улучшение по идентификатору.</summary>
	public static UpgradeDefinition Get(UpgradeId id) => All.First(u => u.Id == id);
}
