using System.Linq;

namespace SwampFrog;

/// <summary>
/// Каталог достижений (данные). Пороги подобраны под реальную шкалу забега
/// (успешный забег — десятки тысяч очков), новый пункт добавляется записью сюда.
/// </summary>
public sealed class AchievementCatalog
{
	public static AchievementDefinition[] All =
	{
		new(AchievementId.FirstRun, AchievementKind.Runs, 1, 5, "ach_first_run",
			"Первый забег", "First run"),
		new(AchievementId.Veteran, AchievementKind.Runs, 25, 15, "ach_veteran",
			"Ветеран", "Veteran"),

		new(AchievementId.HundredFruits, AchievementKind.Fruits, 1000, 10, "ach_fruits",
			"Садовник", "Orchardist"),
		new(AchievementId.ThousandFruits, AchievementKind.Fruits, 10000, 25, "ach_fruits_big",
			"Фруктовый сад", "Fruit orchard"),

		new(AchievementId.GoldenHoard, AchievementKind.Golden, 100, 15, "ach_golden",
			"Золотая жила", "Gold vein"),
		new(AchievementId.GoldenMaster, AchievementKind.Golden, 500, 30, "ach_golden_big",
			"Золотой мастер", "Golden master"),

		new(AchievementId.ScoreFirst, AchievementKind.BestClassic, 20000, 15, "ach_score1",
			"Первый рубеж", "First milestone"),
		new(AchievementId.ScoreFifty, AchievementKind.BestClassic, 50000, 25, "ach_score2",
			"Полсотни", "Fifty thousand"),
		new(AchievementId.ScoreHundred, AchievementKind.BestClassic, 100000, 40, "ach_score3",
			"Сотня", "The hundred"),

		new(AchievementId.MergeAdept, AchievementKind.Merges, 500, 15, "ach_merge",
			"Ученик слияний", "Merge adept"),
		new(AchievementId.MergeMaster, AchievementKind.Merges, 2000, 30, "ach_merge_big",
			"Мастер слияний", "Merge master"),

		new(AchievementId.SuikaChampion, AchievementKind.BestSuika, 1500, 25, "ach_suika",
			"Suika-чемпион", "Suika champion"),

		new(AchievementId.Fashionista, AchievementKind.Skins, 5, 20, "ach_fashion",
			"Модник", "Fashionista"),
		new(AchievementId.Upgrader, AchievementKind.Upgrades, 10, 20, "ach_upgrader",
			"Прокачка", "Fully loaded"),
	};

	/// <summary>Достижение по идентификатору.</summary>
	public static AchievementDefinition Get(AchievementId id) => All.First(a => a.Id == id);
}
