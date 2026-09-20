using Godot;

namespace SwampFrog;

/// <summary>
/// Модель сложности классического режима. Чистый C#-класс без Godot-нод,
/// все балансные константы собраны здесь, чтобы их было легко менять.
/// Прогресс считается от времени партии, а не от очков, чтобы хорошая игра
/// не наказывалась ростом сложности.
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

	/// <summary>Прогресс сложности по времени партии, 0..1.</summary>
	public static float Progress(float runTime)
	{
		return Mathf.Clamp(runTime / RampUpSeconds, 0f, 1f);
	}

	/// <summary>Текущий интервал спавна со случайным разбросом ±20%.</summary>
	public static float SpawnInterval(float runTime, RandomNumberGenerator rng)
	{
		float t = Progress(runTime);
		return Mathf.Lerp(InitialSpawnInterval, MinSpawnInterval, t) * rng.RandfRange(0.8f, 1.2f);
	}

	/// <summary>Текущая базовая скорость падения (разброс применяет спавн).</summary>
	public static float FallSpeed(float runTime)
	{
		return Mathf.Lerp(InitialFallSpeed, MaxFallSpeed, Progress(runTime));
	}

	/// <summary>Текущий шанс выпадения мусора, 0..1.</summary>
	public static float TrashChance(float runTime)
	{
		return Mathf.Lerp(InitialTrashChance, MaxTrashChance, Progress(runTime));
	}
}