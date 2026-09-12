using Godot;

namespace SwampFrog;

/// <summary>
/// Единая точка, где перки превращаются в игровые модификаторы.
/// Игровой код не знает про конкретные перки — он спрашивает методы этого класса.
/// Все балансные константы и «крутилки» собраны здесь.
/// </summary>
public sealed class PerkEffects
{
	/// <summary>Сколько игровых пикселей отводим на один «метр» (магнит, взрыв и т.п.).</summary>
	public const float MetersToPx = 55f;

	/// <summary>Неуязвимость «Панциря» (ур. 5) после урона, секунды.</summary>
	public const float ShellInvulnDuration = 2f;

	// ---------- Липкие ладони ----------

	/// <summary>Множитель радиуса ловли: 1.3 / 1.6 / 1.9, дальше — магнит.</summary>
	public static float CatchRadiusMultiplier(PerkState s)
	{
		int lvl = Mathf.Min(3, s.LevelOf(PerkId.StickyPaws));
		return 1f + 0.3f * lvl;
	}

	/// <summary>Радиус магнита к ладони (0 — выключен).</summary>
	public static float MagnetRadius(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.StickyPaws);
		return lvl >= 5 ? 4f * MetersToPx : lvl >= 4 ? 2f * MetersToPx : 0f;
	}

	/// <summary>Позволяет держать два предмета в одной ладони.</summary>
	public static bool MultiCatch(PerkState s) => s.LevelOf(PerkId.StickyPaws) >= 5;

	// ---------- Резиновый хват / Отбрасывание мусора ----------

	/// <summary>Бонус очков за каждый пойманный фрукт («Резиновый хват» ур. 1).</summary>
	public static int BonusScorePerFruit(PerkState s) => s.LevelOf(PerkId.RubberGrip) >= 1 ? 1 : 0;

	private static float RubberDeflectChance(int lvl)
	{
		if (lvl >= 3)
		{
			return 0.30f;
		}
		if (lvl == 2)
		{
			return 0.15f;
		}
		return 0f;
	}

	/// <summary>Суммарный шанс отскока мусора (оба перка складываются, кап 75%).</summary>
	public static float TrashDeflectChance(PerkState s)
	{
		float chance = RubberDeflectChance(s.LevelOf(PerkId.RubberGrip)) + 0.10f * s.LevelOf(PerkId.TrashBounce);
		return Mathf.Min(0.75f, chance);
	}

	// ---------- Скорострельность ----------

	/// <summary>Множитель скорости вытягивания рук.</summary>
	public static float ExtendSpeedMultiplier(PerkState s)
	{
		switch (Mathf.Min(5, s.LevelOf(PerkId.RapidFire)))
		{
			case 5:
				return 1.5f;
			case 4:
				return 1.4f;
			case 3:
				return 1.3f;
			case 2:
			case 1:
				return 1.2f;
			default:
				return 1f;
		}
	}

	/// <summary>Множитель скорости втягивания рук.</summary>
	public static float RetractSpeedMultiplier(PerkState s)
	{
		switch (Mathf.Min(5, s.LevelOf(PerkId.RapidFire)))
		{
			case 5:
				return 1.5f;
			case 4:
				return 1.4f;
			case 3:
				return 1.3f;
			case 2:
				return 1.2f;
			default:
				return 1f;
		}
	}

	// ---------- Зона замедления ----------

	/// <summary>Коэффициент скорости падения в зоне замедления (1 — эффекта нет).</summary>
	public static float FallSlowFactor(PerkState s, ItemType type)
	{
		int lvl = s.LevelOf(PerkId.SlowZone);
		if (lvl <= 0)
		{
			return 1f;
		}
		if (type == ItemType.Trash && lvl >= 4)
		{
			return 0.60f;
		}
		switch (lvl)
		{
			case 1:
				return 0.85f;
			case 2:
				return 0.75f;
			default:
				return 0.65f;
		}
	}

	/// <summary>true — замедление на нижней половине экрана, false — на нижней трети.</summary>
	public static bool SlowZoneCoversHalf(PerkState s) => s.LevelOf(PerkId.SlowZone) >= 3;

	/// <summary>Пауза всех предметов при появлении золотого (0 — выключено).</summary>
	public static float GoldFreezeDuration(PerkState s) => s.LevelOf(PerkId.SlowZone) >= 5 ? 1f : 0f;

	// ---------- Золотая лихорадка ----------

	/// <summary>Бонус к шансу появления золотого фрукта.</summary>
	public static float GoldenChanceBonus(PerkState s) => 0.05f * Mathf.Min(4, s.LevelOf(PerkId.GoldRush));

	/// <summary>Во сколько раз больше очков даёт золотой фрукт (3 — база без перка).</summary>
	public static int GoldenScoreMultiplier(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.GoldRush);
		if (lvl >= 5)
		{
			return 10;
		}
		if (lvl >= 3)
		{
			return 7;
		}
		if (lvl >= 1)
		{
			return 5;
		}
		return 3;
	}

	/// <summary>Золотой фрукт падает медленнее обычного.</summary>
	public static bool GoldenFallsSlower(PerkState s) => s.LevelOf(PerkId.GoldRush) >= 4;

	/// <summary>Каждый N-й фрукт гарантированно золотой (0 — выключено).</summary>
	public static int GoldenEveryN(PerkState s) => s.LevelOf(PerkId.GoldRush) >= 5 ? 10 : 0;

	// ---------- Фруктовый взрыв ----------

	/// <summary>+1 очко, если у пойманного фрукта есть сосед.</summary>
	public static bool ExplosionNearBonus(PerkState s) => s.LevelOf(PerkId.FruitExplosion) >= 1;

	/// <summary>Радиус взрыва в пикселях (0 — выключено).</summary>
	public static float ExplosionRadius(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.FruitExplosion);
		if (lvl >= 3)
		{
			return 2f * MetersToPx;
		}
		if (lvl == 2)
		{
			return 1f * MetersToPx;
		}
		return 0f;
	}

	/// <summary>Бонус очков за каждый собранный взрывом фрукт (ур. 3+).</summary>
	public static int ExplosionBonusPerFruit(PerkState s) => s.LevelOf(PerkId.FruitExplosion) >= 3 ? 1 : 0;

	/// <summary>Взрыв уничтожает мусор рядом.</summary>
	public static bool ExplosionDestroysTrash(PerkState s) => s.LevelOf(PerkId.FruitExplosion) >= 4;

	/// <summary>Взрывы могут вызывать новые взрывы (каскад).</summary>
	public static bool ExplosionCascades(PerkState s) => s.LevelOf(PerkId.FruitExplosion) >= 5;

	/// <summary>Радиус «рядом» для бонуса соседа (ур. 1).</summary>
	public static float ExplosionNearRadius(PerkState s) => 2f * MetersToPx;

	// ---------- Комбо-мастер ----------

	/// <summary>Очков комбо за одну поимку.</summary>
	public static int ComboGain(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.ComboMaster);
		if (lvl >= 5)
		{
			return 3;
		}
		if (lvl >= 2)
		{
			return 2;
		}
		return 1;
	}

	/// <summary>Сколько очков комбо нужно на один тир множителя (10 → x2, 5 → x3 на ур. 3+).</summary>
	public static int ComboStep(PerkState s) => s.LevelOf(PerkId.ComboMaster) >= 3 ? 5 : 10;

	/// <summary>Промах не сбрасывает комбо.</summary>
	public static bool ComboKeepsOnMiss(PerkState s) => s.LevelOf(PerkId.ComboMaster) >= 1;

	/// <summary>+1 HP каждые N очков комбо (0 — выключено).</summary>
	public static int ComboHpEvery(PerkState s) => s.LevelOf(PerkId.ComboMaster) >= 4 ? 10 : 0;

	/// <summary>Множитель очков по комбо: 1 + combo/step, кап x10.</summary>
	public static int ComboMultiplier(int combo, PerkState s)
	{
		return Mathf.Min(10, 1 + (int)(combo / ComboStep(s)));
	}

	// ---------- Панцирь ----------

	/// <summary>Сколько жизней «Панцирь» добавляет к базовым трём.</summary>
	public static int ShellMaxLivesBonus(PerkState s) => Mathf.Min(4, s.LevelOf(PerkId.Shell));

	/// <summary>Неуязвимость на 2 сек после получения урона (ур. 5).</summary>
	public static bool ShellInvulnerableOnDamage(PerkState s) => s.LevelOf(PerkId.Shell) >= 5;

	// ---------- Вторая жизнь ----------

	/// <summary>Сколько раз можно возродиться за забег.</summary>
	public static int ReviveCount(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.SecondLife);
		if (lvl >= 5)
		{
			return 3;
		}
		if (lvl >= 3)
		{
			return 2;
		}
		if (lvl >= 1)
		{
			return 1;
		}
		return 0;
	}

	/// <summary>Сколько жизней остаётся после возрождения.</summary>
	public static int ReviveHp(PerkState s) => s.LevelOf(PerkId.SecondLife) <= 1 ? 1 : 3;

	/// <summary>Неуязвимость после возрождения, секунды (ур. 4+).</summary>
	public static float ReviveInvulnDuration(PerkState s) => s.LevelOf(PerkId.SecondLife) >= 4 ? 2f : 0f;

	/// <summary>При возрождении экран очищается от мусора (ур. 5).</summary>
	public static bool ReviveClearsTrash(PerkState s) => s.LevelOf(PerkId.SecondLife) >= 5;

	// ---------- Язык-молния ----------

	/// <summary>Как часто авто-ловится ближайший фрукт (0 — выключено).</summary>
	public static float LightningInterval(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.LightningTongue);
		if (lvl >= 4)
		{
			return 5f;
		}
		if (lvl == 3)
		{
			return 10f;
		}
		if (lvl == 2)
		{
			return 20f;
		}
		if (lvl == 1)
		{
			return 30f;
		}
		return 0f;
	}

	/// <summary>Сколько фруктов ловится за один «разряд».</summary>
	public static int LightningCatchCount(PerkState s) => s.LevelOf(PerkId.LightningTongue) >= 3 ? 2 : 1;

	/// <summary>Язык поражает и уничтожает ближайший мусор (ур. 4).</summary>
	public static bool LightningDestroysTrash(PerkState s) => s.LevelOf(PerkId.LightningTongue) >= 4;

	// ---------- Взгляд василиска ----------

	/// <summary>Как часто наступает заморозка (0 — выключено).</summary>
	public static float GazeInterval(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.BasiliskGaze);
		if (lvl >= 4)
		{
			return 15f;
		}
		if (lvl == 3)
		{
			return 30f;
		}
		if (lvl == 2)
		{
			return 30f;
		}
		if (lvl == 1)
		{
			return 40f;
		}
		return 0f;
	}

	/// <summary>Длительность заморозки, секунды.</summary>
	public static float GazeFreezeDuration(PerkState s)
	{
		int lvl = s.LevelOf(PerkId.BasiliskGaze);
		if (lvl >= 4)
		{
			return 3f;
		}
		if (lvl == 3)
		{
			return 2f;
		}
		if (lvl == 2)
		{
			return 2f;
		}
		return 1f;
	}

	/// <summary>Мусор во время заморозки превращается в камень (безопасен, ур. 3+).</summary>
	public static bool GazeStonesTrash(PerkState s) => s.LevelOf(PerkId.BasiliskGaze) >= 3;

	/// <summary>Замороженные фрукты дают x2 очков (ур. 5).</summary>
	public static bool GazeFrozenDoubleScore(PerkState s) => s.LevelOf(PerkId.BasiliskGaze) >= 5;
}