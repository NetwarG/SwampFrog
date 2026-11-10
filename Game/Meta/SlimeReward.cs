namespace SwampFrog;

/// <summary>
/// Начисление валюты «Слизь» за события (см. docs/META.md). Чистые функции,
/// чтобы формулы было легко проверить тестом и менять без правки механики.
/// </summary>
public static class SlimeReward
{
	/// <summary>Бонус за новый рекорд классического режима.</summary>
	public const int RecordBonus = 10;

	/// <summary>Слизь за классический забег: счёт, пойманные фрукты и бонус за рекорд.</summary>
	public static int Run(int score, int caught, bool newRecord)
	{
		int reward = score / 1000 + caught / 50;
		if (newRecord)
			reward += RecordBonus;
		return reward;
	}

	/// <summary>Слизь за завершённый раунд Suika: слияния и очки.</summary>
	public static int Suika(int merges, int score)
	{
		return merges / 5 + score / 100;
	}
}
