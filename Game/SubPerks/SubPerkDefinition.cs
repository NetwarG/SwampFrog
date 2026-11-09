using Godot;

namespace SwampFrog;

/// <summary>
/// Уникальный идентификатор субперка.
/// Как добавить новый субперк:
/// 1. значение перечисления сюда;
/// 2. запись в <see cref="SubPerkCatalog"/>;
/// 3. коэффициент в <see cref="SubPerkEffects"/>.
/// </summary>
public enum SubPerkId
{
	Wisdom,
	Hoarding,
	Vitamins,
	CleanPond,
	Gambling,
	Hunger,
	Crowding,
	Insomnia,
}

/// <summary>
/// Полярность субперка. Предложения строго чередуются: положительные сменяются
/// отрицательными, поэтому игрок то получает помощь, то платит за неё.
/// </summary>
public enum SubPerkPolarity
{
	Positive,
	Negative,
}

/// <summary>
/// Описание субперка для UI: имя, слоган, полярность и акцентный цвет.
/// Сила эффекта задаётся не здесь, а случайным приростом 1–5% за взятие,
/// числовая механика — в <see cref="SubPerkEffects"/>.
/// </summary>
public sealed class SubPerkDefinition
{
	public required SubPerkId Id;

	/// <summary>Русское имя субперка.</summary>
	public required string NameRu;

	/// <summary>Английское имя субперка.</summary>
	public required string NameEn;

	public required SubPerkPolarity Polarity;

	/// <summary>Слоган-подзаголовок по-русски, например «Опыт капает».</summary>
	public required string TaglineRu;

	/// <summary>Слоган-подзаголовок по-английски.</summary>
	public required string TaglineEn;

	/// <summary>Акцентный цвет карточки субперка.</summary>
	public required Color Accent;

	/// <summary>Имя субперка на текущем языке интерфейса.</summary>
	public string Name => Loc.Pick(NameRu, NameEn);

	/// <summary>Слоган на текущем языке интерфейса.</summary>
	public string Tagline => Loc.Pick(TaglineRu, TaglineEn);
}

/// <summary>Готовая «карточка» субперка для оверлея выбора.</summary>
public record SubPerkOffer(SubPerkId Id, string Name, string Tagline, SubPerkPolarity Polarity, int CurrentLevel, int GainPercent, Color Accent);

/// <summary>Строка списка активных субперков (для окна паузы).</summary>
public record SubPerkSummary(SubPerkId Id, int Level, int Percent, SubPerkPolarity Polarity)
{
	/// <summary>Имя субперка на текущем языке интерфейса.</summary>
	public string Name => SubPerkCatalog.Find(Id).Name;
}
