using Godot;

namespace SwampFrog;

/// <summary>
/// Единая точка, где субперки превращаются в игровые модификаторы.
/// Игровой код не знает про конкретные субперки — он спрашивает методы этого класса.
/// Эффекты слабые (1–5% за взятие) и накопительные, поэтому возвращаются коэффициентами.
/// </summary>
public sealed class SubPerkEffects
{
	/// <summary>Минимальный случайный прирост субперка за одно взятие, проценты.</summary>
	public const int MinGainPercent = 1;

	/// <summary>Максимальный случайный прирост субперка за одно взятие, проценты.</summary>
	public const int MaxGainPercent = 5;

	/// <summary>Случайный прирост за взятие: 1–5%.</summary>
	public static int RollGain(RandomNumberGenerator rng)
	{
		return rng.RandiRange(MinGainPercent, MaxGainPercent);
	}

	// ---------- Опыт (Мудрость / Бессонница) ----------

	/// <summary>Множитель получаемого опыта (1 — без изменений, не ниже нуля).</summary>
	public static float XpMultiplier(SubPerkState s)
	{
		float percent = s.PercentOf(SubPerkId.Wisdom) - s.PercentOf(SubPerkId.Insomnia);
		return Mathf.Max(0f, 1f + percent / 100f);
	}

	// ---------- Очки (Азарт / Голод, утешительная половина процента) ----------

	/// <summary>Множитель очков: Азарт и Голод дают половину своего процента к счёту.</summary>
	public static float ScoreMultiplier(SubPerkState s)
	{
		float percent = 0.5f * (s.PercentOf(SubPerkId.Gambling) + s.PercentOf(SubPerkId.Hunger));
		return 1f + percent / 100f;
	}

	// ---------- Запас Suika (Запасливость) ----------

	/// <summary>Шанс, что пойманный фрукт попадёт в запас дважды, 0..1.</summary>
	public static float StockDoubleChance(SubPerkState s)
	{
		return Mathf.Clamp(s.PercentOf(SubPerkId.Hoarding) / 100f, 0f, 1f);
	}

	// ---------- Хилка (Витамины) ----------

	/// <summary>Множитель шанса выпадения хилки.</summary>
	public static float HealChanceMultiplier(SubPerkState s)
	{
		return 1f + s.PercentOf(SubPerkId.Vitamins) / 100f;
	}

	// ---------- Мусор (Чистота / Теснота) ----------

	/// <summary>Множитель шанса выпадения мусора (1 — без изменений, не ниже нуля).</summary>
	public static float TrashChanceMultiplier(SubPerkState s)
	{
		float percent = s.PercentOf(SubPerkId.Crowding) - s.PercentOf(SubPerkId.CleanPond);
		return Mathf.Max(0f, 1f + percent / 100f);
	}

	// ---------- Скорость спавна (Азарт) ----------

	/// <summary>Множитель скорости появления предметов: выше 1 — падают чаще.</summary>
	public static float SpawnRateMultiplier(SubPerkState s)
	{
		return 1f + s.PercentOf(SubPerkId.Gambling) / 100f;
	}

	// ---------- Скорость падения (Голод) ----------

	/// <summary>Множитель скорости падения предметов.</summary>
	public static float FallSpeedMultiplier(SubPerkState s)
	{
		return 1f + s.PercentOf(SubPerkId.Hunger) / 100f;
	}
}
