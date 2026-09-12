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
	public required string Name;

	/// <summary>Слоган-подзаголовок, например «Фрукты прилипают».</summary>
	public required string Tagline;

	public required int MaxLevel;

	/// <summary>Эффект каждого уровня (длина массива == MaxLevel).</summary>
	public required string[] LevelEffects;

	/// <summary>Акцентный цвет карточки перка.</summary>
	public required Color Accent;
}

/// <summary>Готовая «карточка» перка для оверлея выбора.</summary>
public record PerkOffer(PerkId Id, string Name, string Tagline, int CurrentLevel, string NextLevelEffect, int MaxLevel, Color Accent)
{
}