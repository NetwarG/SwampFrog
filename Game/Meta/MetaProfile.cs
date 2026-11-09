namespace SwampFrog;

/// <summary>
/// Постоянный профиль игрока: пожизненная статистика и её сохранение.
/// Отдаёт снимок для экрана профиля (см. Game/UI/Meta/ProfilePanel.cs).
/// </summary>
public sealed class MetaProfile
{
	/// <summary>Счётчики профиля: партии, пойманные фрукты, слияния Suika.</summary>
	public MetaStats Stats { get; }

	private MetaProfile(MetaStats stats)
	{
		Stats = stats;
	}

	/// <summary>Загружает профиль из save.cfg; отсутствие данных даёт пустую статистику.</summary>
	public static MetaProfile Load() => new(MetaSave.LoadStats());

	/// <summary>Сохраняет профиль в save.cfg.</summary>
	public void Save() => MetaSave.SaveStats(Stats);

	/// <summary>
	/// Снимок показателей для экрана профиля. Рекорды и запас берутся из уже
	/// существующих источников и передаются вызывающей стороной.
	/// </summary>
	public ProfileSnapshot Snapshot(int bestClassic, int bestSuika, int stockTotal)
	{
		return new ProfileSnapshot(
			Stats.RunsPlayed, Stats.FruitsCaught, Stats.GoldenCaught, Stats.SuikaMerges,
			bestClassic, bestSuika, stockTotal);
	}
}

/// <summary>Показатели профиля, готовые к показу на экране.</summary>
public readonly record struct ProfileSnapshot(
	int RunsPlayed,
	int FruitsCaught,
	int GoldenCaught,
	int SuikaMerges,
	int BestClassic,
	int BestSuika,
	int StockTotal);
