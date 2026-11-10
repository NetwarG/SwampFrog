using System.Collections.Generic;

namespace SwampFrog;

/// <summary>
/// Постоянный профиль игрока: пожизненная статистика, кошелёк «Слизь» и косметические
/// скины лягушки со сохранением (см. Game/UI/Meta/ProfilePanel.cs).
/// </summary>
public sealed class MetaProfile
{
	/// <summary>Счётчики профиля: партии, пойманные фрукты, слияния Suika.</summary>
	public MetaStats Stats { get; }

	/// <summary>Кошелёк валюты «Слизь».</summary>
	public Wallet Wallet { get; }

	private readonly HashSet<SkinId> _owned = new();
	private SkinId _hat = SkinCatalog.DefaultFor(SkinSlot.Hat).Id;
	private SkinId _gloves = SkinCatalog.DefaultFor(SkinSlot.Gloves).Id;
	private SkinId _stand = SkinCatalog.DefaultFor(SkinSlot.Stand).Id;

	private MetaProfile(MetaStats stats, Wallet wallet)
	{
		Stats = stats;
		Wallet = wallet;
	}

	/// <summary>Загружает профиль из save.cfg; отсутствие данных даёт пустые счётчики и дефолтные скины.</summary>
	public static MetaProfile Load()
	{
		var wallet = new Wallet();
		wallet.SetBalance(MetaSave.LoadCoins());
		var profile = new MetaProfile(MetaSave.LoadStats(), wallet);
		foreach (SkinId id in MetaSave.LoadOwnedSkins()) profile._owned.Add(id);
		profile._hat = MetaSave.LoadSelectedSkin(SkinSlot.Hat);
		profile._gloves = MetaSave.LoadSelectedSkin(SkinSlot.Gloves);
		profile._stand = MetaSave.LoadSelectedSkin(SkinSlot.Stand);
		return profile;
	}

	/// <summary>Сохраняет профиль (счётчики, слизь и скины) в save.cfg.</summary>
	public void Save() => MetaSave.Save(Stats, Wallet.Balance, _owned, _hat, _gloves, _stand);

	// ---------- Скины ----------

	/// <summary>Скин доступен: бесплатный по умолчанию или уже куплен/открыт.</summary>
	public bool Owns(SkinId id) => IsFree(id) || _owned.Contains(id);

	/// <summary>Отмечает скин купленным или открытым.</summary>
	public void Own(SkinId id) => _owned.Add(id);

	/// <summary>Выбранный скин слота.</summary>
	public SkinId Selected(SkinSlot slot) => slot switch
	{
		SkinSlot.Gloves => _gloves,
		SkinSlot.Stand => _stand,
		_ => _hat,
	};

	/// <summary>Выбирает скин: слот определяется по самому скину.</summary>
	public void Select(SkinId id)
	{
		switch (SkinCatalog.SlotOf(id))
		{
			case SkinSlot.Gloves:
				_gloves = id;
				break;
			case SkinSlot.Stand:
				_stand = id;
				break;
			default:
				_hat = id;
				break;
		}
	}

	private static bool IsFree(SkinId id) => SkinUnlockRules.IsFree(SkinCatalog.Get(id));

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

