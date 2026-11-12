using System.Collections.Generic;

namespace SwampFrog;

/// <summary>
/// Показатели профиля, по которым проверяются условия достижений.
/// Рекорды берутся извне: они лежат в сохранении `score/high` и `suika/high`.
/// </summary>
public readonly record struct AchievementContext(
	int Runs,
	int Fruits,
	int Golden,
	int Merges,
	int BestClassic,
	int BestSuika,
	int Skins,
	int Upgrades)
{
	/// <summary>Собирает показатели из профиля и рекордов.</summary>
	public static AchievementContext From(MetaProfile profile, int bestClassic, int bestSuika)
	{
		return new AchievementContext(
			profile.Stats.RunsPlayed,
			profile.Stats.FruitsCaught,
			profile.Stats.GoldenCaught,
			profile.Stats.SuikaMerges,
			bestClassic,
			bestSuika,
			profile.OwnedSkinCount,
			profile.TotalUpgradeLevels);
	}
}

/// <summary>
/// Проверка выполнения достижений по статистике профиля. Чистые функции, чтобы
/// условия было легко тестировать и расширять (см. docs/META.md).
/// </summary>
public static class AchievementTracker
{
	/// <summary>Текущее значение показателя достижения.</summary>
	public static int Progress(AchievementDefinition def, in AchievementContext ctx) => def.Kind switch
	{
		AchievementKind.Runs => ctx.Runs,
		AchievementKind.Fruits => ctx.Fruits,
		AchievementKind.Golden => ctx.Golden,
		AchievementKind.Merges => ctx.Merges,
		AchievementKind.BestClassic => ctx.BestClassic,
		AchievementKind.BestSuika => ctx.BestSuika,
		AchievementKind.Skins => ctx.Skins,
		AchievementKind.Upgrades => ctx.Upgrades,
		_ => 0,
	};

	/// <summary>Условие достижения выполнено.</summary>
	public static bool IsMet(AchievementDefinition def, in AchievementContext ctx) =>
		Progress(def, ctx) >= def.Target;

	/// <summary>
	/// Достижения, условия которых выполнены, но ещё не открыты. По этому списку
	/// начисляется награда — каждое достижение выдаётся ровно один раз.
	/// </summary>
	public static AchievementId[] NewlyUnlocked(MetaProfile profile, in AchievementContext ctx)
	{
		var result = new List<AchievementId>();
		foreach (AchievementDefinition def in AchievementCatalog.All)
		{
			if (!profile.HasAchievement(def.Id) && IsMet(def, ctx))
				result.Add(def.Id);
		}
		return result.ToArray();
	}
}
