namespace SwampFrog;

/// <summary>
/// Постоянный профиль игрока: пожизненная статистика, кошелёк «Слизь» и их сохранение.
/// Отдаёт снимок для экрана профиля (см. Game/UI/Meta/ProfilePanel.cs).
/// </summary>
public sealed class MetaProfile
{
	/// <summary>Счётчики профиля: партии, пойманные фрукты, слияния Suika.</summary>
	public MetaStats Stats { get; }

	/// <summary>Кошелёк валюты «Слизь».</summary>
	public Wallet Wallet { get; }

	private MetaProfile(MetaStats stats, Wallet wallet)
	{
		Stats = stats;
		Wallet = wallet;
	}

	/// <summary>Загружает профиль из save.cfg; отсутствие данных даёт пустую статистику и ноль слизи.</summary>
	public static MetaProfile Load()
	{
		var wallet = new Wallet();
		wallet.SetBalance(MetaSave.LoadCoins());
		return new MetaProfile(MetaSave.LoadStats(), wallet);
	}

	/// <summary>Сохраняет профиль (статистику и баланс) в save.cfg.</summary>
	public void Save() => MetaSave.Save(Stats, Wallet.Balance);

	/// <summary>
	/// Снимок показателей для экрана профиля. Рекорды и запас берутся из уже
	/// существующих источников и передаются вызывающей стороной; слизь — из кошелька.
	/// </summary>
	public ProfileSnapshot Snapshot(int bestClassic, int bestSuika, int stockTotal)
	{
		return new ProfileSnapshot(
			Stats.RunsPlayed, Stats.FruitsCaught, Stats.GoldenCaught, Stats.SuikaMerges,
			bestClassic, bestSuika, stockTotal, Wallet.Balance);
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
	int StockTotal,
	int Coins);
