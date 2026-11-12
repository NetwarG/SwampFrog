namespace SwampFrog;

/// <summary>Тип условия достижения: по какому показателю профиля оно открывается.</summary>
public enum AchievementKind
{
	/// <summary>Число завершённых забегов классики.</summary>
	Runs,

	/// <summary>Поймано фруктов за всё время.</summary>
	Fruits,

	/// <summary>Поймано золотых фруктов за всё время.</summary>
	Golden,

	/// <summary>Слияний в мини-игре Suika за всё время.</summary>
	Merges,

	/// <summary>Рекорд классического режима.</summary>
	BestClassic,

	/// <summary>Рекорд мини-игры Suika.</summary>
	BestSuika,

	/// <summary>Сколько скинов открыто.</summary>
	Skins,

	/// <summary>Суммарный уровень постоянных улучшений.</summary>
	Upgrades,
}

/// <summary>Все достижения профиля.</summary>
public enum AchievementId
{
	FirstRun,
	Veteran,
	HundredFruits,
	ThousandFruits,
	GoldenHoard,
	GoldenMaster,
	ScoreFirst,
	ScoreFifty,
	ScoreHundred,
	MergeAdept,
	MergeMaster,
	SuikaChampion,
	Fashionista,
	Upgrader,
}

/// <summary>
/// Описание достижения: условие (вид и порог), награда в слизи, иконка и краткая
/// подпись (русская и английская, как у скинов и перков).
/// </summary>
public sealed record AchievementDefinition(
	AchievementId Id,
	AchievementKind Kind,
	int Target,
	int Reward,
	string IconName,
	string CaptionRu,
	string CaptionEn)
{
	/// <summary>Короткая подпись на текущем языке интерфейса.</summary>
	public string Caption => Loc.Pick(CaptionRu, CaptionEn);

	/// <summary>Путь иконки достижения в Game/assets/achievements.</summary>
	public string IconPath => $"res://Game/assets/achievements/{IconName}.png";
}
