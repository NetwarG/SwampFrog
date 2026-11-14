using Godot;

namespace SwampFrog;

/// <summary>
/// Баланс мух классического режима: чистые константы и формулы без Godot-нод,
/// чтобы частоту, скорость и лимит мух можно было менять в одном месте
/// (см. <see cref="Difficulty"/> — тот же приём).
/// </summary>
public static class FlyTuning
{
	/// <summary>Первые секунды партии мух нет: раннее поле остаётся спокойным.</summary>
	public const float FirstFlyDelay = 12f;

	/// <summary>Интервал выпуска мух, секунды: базовый → минимальный.</summary>
	public const float BaseSpawnInterval = 9f;
	public const float MinSpawnInterval = 4f;

	/// <summary>Сколько мух одновременно держит поле: базово и жёсткий потолок.</summary>
	public const int BaseMaxFlies = 1;
	public const int MaxFliesCap = 4;

	/// <summary>Скорости мухи, px/сек: блуждание и подлёт к фрукту.</summary>
	public const float WanderSpeed = 95f;
	public const float ApproachSpeed = 168f;

	/// <summary>Ускорение доворота к цели, px/сек².</summary>
	public const float ArriveAcceleration = 340f;

	/// <summary>Разброс времени между сменами курса при блуждании, секунды.</summary>
	public const float WanderTurnMin = 0.6f;
	public const float WanderTurnMax = 1.6f;

	/// <summary>Дистанция, с которой муха считается севшей на фрукт.</summary>
	public const float LandDistance = 18f;

	/// <summary>Сколько муха сидит на фрукте до порчи, секунды.</summary>
	public const float SitTime = 0.9f;

	/// <summary>Поля полёта: муха не уходит в HUD и за края поля.</summary>
	public const float SideMargin = 30f;
	public const float TopMargin = 120f;
	public const float BottomMargin = 60f;

	/// <summary>Скорость взмаха крыльев в отрисовке.</summary>
	public const float WingFlapSpeed = 46f;

	/// <summary>Очки за поимку мухи.</summary>
	public const int CatchScore = 25;

	/// <summary>Потолок выпуска мух: растёт с множителем сложности от очков.</summary>
	public static int MaxFlies(int score)
	{
		int extra = (int)Mathf.Round((Difficulty.ScoreFactor(score) - 1f) * 3f);
		return Mathf.Clamp(BaseMaxFlies + extra, BaseMaxFlies, MaxFliesCap);
	}

	/// <summary>Текущий интервал выпуска мух со случайным разбросом ±15%.</summary>
	public static float SpawnInterval(int score, RandomNumberGenerator rng)
	{
		float interval = BaseSpawnInterval / Difficulty.ScoreFactor(score);
		return Mathf.Max(interval, MinSpawnInterval) * rng.RandfRange(0.85f, 1.15f);
	}
}
