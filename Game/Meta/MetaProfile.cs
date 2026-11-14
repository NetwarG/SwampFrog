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
	private readonly Dictionary<UpgradeId, int> _upgrades = new();
	private readonly HashSet<AchievementId> _achievements = new();
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
		foreach (KeyValuePair<UpgradeId, int> pair in MetaSave.LoadUpgrades())
			profile._upgrades[pair.Key] = pair.Value;
		foreach (AchievementId id in MetaSave.LoadAchievements())
			profile._achievements.Add(id);
		return profile;
	}

	/// <summary>Сохраняет профиль (счётчики, слизь, скины, улучшения и достижения) в save.cfg.</summary>
	public void Save() => MetaSave.Save(Stats, Wallet.Balance, _owned, _hat, _gloves, _stand, _upgrades, _achievements);

	// ---------- Достижения ----------

	/// <summary>Сколько скинов открыто (без бесплатных вариантов «Нет»).</summary>
	public int OwnedSkinCount => _owned.Count;

	/// <summary>Суммарный уровень всех постоянных улучшений.</summary>
	public int TotalUpgradeLevels
	{
		get
		{
			int total = 0;
			foreach (int level in _upgrades.Values)
				total += level;
			return total;
		}
	}

	/// <summary>Достижение открыто.</summary>
	public bool HasAchievement(AchievementId id) => _achievements.Contains(id);

	/// <summary>Отмечает достижение открытым.</summary>
	public void UnlockAchievement(AchievementId id) => _achievements.Add(id);

	/// <summary>Сколько достижений открыто.</summary>
	public int AchievementsUnlocked => _achievements.Count;

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

	// ---------- Постоянные улучшения ----------

	/// <summary>Текущий уровень постоянного улучшения (0 — ещё не куплено).</summary>
	public int UpgradeLevel(UpgradeId id) => _upgrades.TryGetValue(id, out int level) ? level : 0;

	/// <summary>Улучшение прокачано до максимума.</summary>
	public bool IsUpgradeMaxed(UpgradeId id) => UpgradeLevel(id) >= UpgradeCatalog.Get(id).MaxLevel;

	/// <summary>
	/// Покупает следующий уровень улучшения: списывает слизь.
	/// Возвращает false, если улучшение максимально или не хватает слизи.
	/// </summary>
	public bool BuyUpgrade(UpgradeId id)
	{
		UpgradeDefinition def = UpgradeCatalog.Get(id);
		int level = UpgradeLevel(id);
		if (level >= def.MaxLevel)
			return false;
		if (!Wallet.Spend(def.PriceFor(level + 1)))
			return false;
		_upgrades[id] = level + 1;
		return true;
	}

	/// <summary>
	/// Снимок показателей для экрана профиля. Рекорды и запас берутся из уже
	/// существующих источников и передаются вызывающей стороной; слизь — из кошелька.
	/// </summary>
	public ProfileSnapshot Snapshot(int bestClassic, int bestSuika, int stockTotal)
	{
		return new ProfileSnapshot(
			Stats.RunsPlayed, Stats.FruitsCaught, Stats.GoldenCaught, Stats.SuikaMerges, Stats.FliesCaught,
			bestClassic, bestSuika, stockTotal, Wallet.Balance);
	}
}

/// <summary>Показатели профиля, готовые к показу на экране.</summary>
public readonly record struct ProfileSnapshot(
	int RunsPlayed,
	int FruitsCaught,
	int GoldenCaught,
	int SuikaMerges,
	int FliesCaught,
	int BestClassic,
	int BestSuika,
	int StockTotal,
	int Coins);

