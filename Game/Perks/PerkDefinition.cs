using Godot;

namespace SwampFrog;

/// <summary>
/// Уникальный идентификатор перка.
/// Как добавить новый перк:
/// 1. значение перечисления сюда;
/// 2. запись в <see cref="PerkCatalog"/>;
/// 3. числовые эффекты в <see cref="PerkEffects"/>;
/// 4. при необходимости — вызов нового модификатора в игровом коде (обычно 2–3 строки).
/// </summary>
public enum PerkId
{
	StickyPaws,
	RubberGrip,
	RapidFire,
	SlowZone,
	GoldRush,
	FruitExplosion,
	ComboMaster,
	Shell,
	TrashBounce,
	SecondLife,
	LightningTongue,
	BasiliskGaze,
}

/// <summary>
/// Описание перка для UI: имя, слоган, число уровней и тексты эффектов.
/// Здесь — только «текст», вся механика живёт в <see cref="PerkEffects"/>.
/// </summary>
public sealed class PerkDefinition
{
	public required PerkId Id;

	/// <summary>Русское имя перка.</summary>
	public required string NameRu;

	/// <summary>Английское имя перка.</summary>
	public required string NameEn;

	/// <summary>Слоган-подзаголовок по-русски, например «Фрукты прилипают».</summary>
	public required string TaglineRu;

	/// <summary>Слоган-подзаголовок по-английски.</summary>
	public required string TaglineEn;

	public required int MaxLevel;

	/// <summary>Эффект каждого уровня по-русски (длина массива == MaxLevel).</summary>
	public required string[] LevelEffectsRu;

	/// <summary>Эффект каждого уровня по-английски (длина массива == MaxLevel).</summary>
	public required string[] LevelEffectsEn;

	/// <summary>Акцентный цвет карточки перка.</summary>
	public required Color Accent;

	/// <summary>Имя перка на текущем языке интерфейса.</summary>
	public string Name => Loc.Pick(NameRu, NameEn);

	/// <summary>Слоган на текущем языке интерфейса.</summary>
	public string Tagline => Loc.Pick(TaglineRu, TaglineEn);

	/// <summary>Эффекты уровней на текущем языке интерфейса.</summary>
	public string[] LevelEffects => Loc.Pick(LevelEffectsRu, LevelEffectsEn);
}

/// <summary>Готовая «карточка» перка для оверлея выбора.</summary>
public record PerkOffer(PerkId Id, string Name, string Tagline, int CurrentLevel, string NextLevelEffect, int MaxLevel, Color Accent)
{
}

/// <summary>Данные выбранного перка для панели-индикатора внизу экрана.</summary>
public record PerkIconInfo(PerkId Id, int Level);