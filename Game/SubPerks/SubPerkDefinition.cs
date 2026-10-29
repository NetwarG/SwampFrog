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
	public required string Name;
	public required SubPerkPolarity Polarity;

	/// <summary>Слоган-подзаголовок, например «Опыт капает».</summary>
	public required string Tagline;

	/// <summary>Акцентный цвет карточки субперка.</summary>
	public required Color Accent;
}

/// <summary>Готовая «карточка» субперка для оверлея выбора.</summary>
public record SubPerkOffer(SubPerkId Id, string Name, string Tagline, SubPerkPolarity Polarity, int CurrentLevel, int GainPercent, Color Accent);

/// <summary>Строка списка активных субперков (для окна паузы).</summary>
public record SubPerkSummary(string Name, int Level, int Percent, SubPerkPolarity Polarity);
