using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Пункт достижения для сетки профиля: иконка, прогресс к цели и признак
/// «открыто». Подпись плитка берёт из каталога по id, поэтому смена языка
/// обновляет её без пересборки.
/// </summary>
public sealed record AchievementEntry(
	AchievementId Id,
	string IconPath,
	int Target,
	int Progress,
	bool Unlocked);

/// <summary>Собирает пункты достижений из каталога и профиля.</summary>
public static class AchievementState
{
	public static AchievementEntry[] Build(MetaProfile profile, in AchievementContext ctx)
	{
		var result = new List<AchievementEntry>();
		foreach (AchievementDefinition def in AchievementCatalog.All)
		{
			int progress = Mathf.Min(AchievementTracker.Progress(def, ctx), def.Target);
			result.Add(new AchievementEntry(def.Id, def.IconPath, def.Target, progress,
				profile.HasAchievement(def.Id)));
		}
		return result.ToArray();
	}
}
