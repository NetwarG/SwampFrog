using Godot;

namespace SwampFrog;

/// <summary>
/// Каталог всех субперков игры: имя, слоган, полярность и цвет.
/// Субперки не дублируют основные перки и не имеют максимального уровня —
/// они выдаются весь забег, но реже по мере роста уровня жабы.
/// Числовая механика — в <see cref="SubPerkEffects"/>.
/// </summary>
public sealed class SubPerkCatalog
{
	public static SubPerkDefinition Find(SubPerkId id)
	{
		foreach (SubPerkDefinition def in All)
		{
			if (def.Id == id)
				return def;
		}
		return All[0];
	}

	public static SubPerkDefinition[] All =
	{
		new SubPerkDefinition
		{
			Id = SubPerkId.Wisdom,
			Name = "Больше опыта",
			Polarity = SubPerkPolarity.Positive,
			Tagline = "Опыт капает",
			Accent = new Color("9ad4ff"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Hoarding,
			Name = "Шанс двойного фрукта в запас",
			Polarity = SubPerkPolarity.Positive,
			Tagline = "Мимо не пройдёт",
			Accent = new Color("b4e863"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Vitamins,
			Name = "Шанс выпадения хилок",
			Polarity = SubPerkPolarity.Positive,
			Tagline = "Здоровое питание",
			Accent = new Color("ffb2a0"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.CleanPond,
			Name = "Меньше мусора",
			Polarity = SubPerkPolarity.Positive,
			Tagline = "Меньше грязи",
			Accent = new Color("7acbb2"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Gambling,
			Name = "Больше фруктов и мусора",
			Polarity = SubPerkPolarity.Negative,
			Tagline = "Больше риска — больше счёт",
			Accent = new Color("ffd32e"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Hunger,
			Name = "Ускорение падения",
			Polarity = SubPerkPolarity.Negative,
			Tagline = "Быстрее, вкуснее",
			Accent = new Color("ff9a3c"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Crowding,
			Name = "Шанс мусора",
			Polarity = SubPerkPolarity.Negative,
			Tagline = "Всё валится",
			Accent = new Color("89a29d"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Insomnia,
			Name = "Меньше опыта",
			Polarity = SubPerkPolarity.Negative,
			Tagline = "Мысли путаются",
			Accent = new Color("c9a9ff"),
		},
	};
}
