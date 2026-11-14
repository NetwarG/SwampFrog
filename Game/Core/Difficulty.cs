using Godot;

namespace SwampFrog;

/// <summary>
/// Модель сложности классического режима. Чистый C#-класс без Godot-нод,
/// все балансные константы собраны здесь, чтобы их было легко менять.
/// Падение предметов усложняется со временем, движение лягушки — с ростом счёта.
/// Очки дают бесконечный квадратичный множитель сложности (x2 к 100 000).
/// </summary>
public sealed class Difficulty
{
	/// <summary>За сколько секунд сложность достигает максимума.</summary>
	public const float RampUpSeconds = 180f;

	/// <summary>Интервал спавна, секунды: начальный → минимальный.</summary>
	public const float InitialSpawnInterval = 1.05f;
	public const float MinSpawnInterval = 0.55f;

	/// <summary>Скорость падения, px/сек: начальная → максимальная.</summary>
	public const float InitialFallSpeed = 150f;
	public const float MaxFallSpeed = 260f;

	/// <summary>Шанс выпадения мусора, 0..1: начальный → максимальный.</summary>
	public const float InitialTrashChance = 0.10f;
	public const float MaxTrashChance = 0.22f;

	/// <summary>Пороги движения лягушки: по осям → в произвольном направлении.</summary>
	public const int FrogMovementScore = 100;
	public const int FrogRandomMovementScore = 500;

	/// <summary>Скорость лягушки в базовых единицах/сек и счёт достижения максимума.</summary>
	public const float InitialFrogSpeed = 45f;
	public const float MaxFrogSpeed = 180f;
	public const int FrogMaxSpeedScore = 3000;

	/// <summary>Интервалы смены направления лягушки, секунды.</summary>
	public const float FrogAxisTurnInterval = 2.4f;
	public const float FrogRandomTurnInterval = 1.6f;

	// ---------- Бесконечный рост сложности от очков ----------

	/// <summary>Очков в «квадрате»: множитель x2 при 100 000 и x5 при 200 000.</summary>
	public const float EndlessScoreScale = 100_000f;

	/// <summary>Абсолютный минимум интервала спавна, сек: иначе поле захлёбывается потоком.</summary>
	public const float SpawnIntervalFloor = 0.2f;

	/// <summary>Потолок шанса выпадения мусора, 0..1.</summary>
	public const float TrashChanceCap = 0.5f;

	/// <summary>Множитель сложности от очков: 1 в начале, x2 к 100 000 и резкий рост дальше.</summary>
	public static float ScoreFactor(int score)
	{
		float s = Mathf.Max(0, score) / EndlessScoreScale;
		return 1f + s * s;
	}

	public static float FrogSpeed(int score)
	{
		if (score < FrogMovementScore)
		{
			return 0f;
		}
		float t = Mathf.Clamp((score - FrogMovementScore) / (float)(FrogMaxSpeedScore - FrogMovementScore), 0f, 1f);
		return Mathf.Lerp(InitialFrogSpeed, MaxFrogSpeed, t);
	}

	/// <summary>Прогресс сложности по времени партии, 0..1.</summary>
	public static float Progress(float runTime)
	{
		return Mathf.Clamp(runTime / RampUpSeconds, 0f, 1f);
	}

	/// <summary>Текущий интервал спавна со случайным разбросом ±20% и полом для позднего роста.</summary>
	public static float SpawnInterval(float runTime, int score, RandomNumberGenerator rng)
	{
		float t = Progress(runTime);
		float interval = Mathf.Lerp(InitialSpawnInterval, MinSpawnInterval, t) / ScoreFactor(score);
		return Mathf.Max(interval, SpawnIntervalFloor) * rng.RandfRange(0.8f, 1.2f);
	}

	/// <summary>Текущая базовая скорость падения (разброс применяет спавн).</summary>
	public static float FallSpeed(float runTime, int score)
	{
		return Mathf.Lerp(InitialFallSpeed, MaxFallSpeed, Progress(runTime)) * ScoreFactor(score);
	}

	/// <summary>Текущий шанс выпадения мусора, 0..1, с потолком для позднего роста.</summary>
	public static float TrashChance(float runTime, int score)
	{
		return Mathf.Min(Mathf.Lerp(InitialTrashChance, MaxTrashChance, Progress(runTime)) * ScoreFactor(score), TrashChanceCap);
	}
}
