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
			NameRu = "Больше опыта",
			NameEn = "More Experience",
			Polarity = SubPerkPolarity.Positive,
			TaglineRu = "Опыт капает",
			TaglineEn = "XP trickles in",
			Accent = new Color("9ad4ff"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Hoarding,
			NameRu = "Шанс двойного фрукта в запас",
			NameEn = "Chance of a double fruit in stock",
			Polarity = SubPerkPolarity.Positive,
			TaglineRu = "Мимо не пройдёт",
			TaglineEn = "Won't slip by",
			Accent = new Color("b4e863"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Vitamins,
			NameRu = "Шанс выпадения хилок",
			NameEn = "Chance of healing drops",
			Polarity = SubPerkPolarity.Positive,
			TaglineRu = "Здоровое питание",
			TaglineEn = "Healthy diet",
			Accent = new Color("ffb2a0"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.CleanPond,
			NameRu = "Меньше мусора",
			NameEn = "Less trash",
			Polarity = SubPerkPolarity.Positive,
			TaglineRu = "Меньше грязи",
			TaglineEn = "Less mess",
			Accent = new Color("7acbb2"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Gambling,
			NameRu = "Больше фруктов и мусора",
			NameEn = "More fruit and trash",
			Polarity = SubPerkPolarity.Negative,
			TaglineRu = "Больше риска — больше счёт",
			TaglineEn = "More risk, more score",
			Accent = new Color("ffd32e"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Hunger,
			NameRu = "Ускорение падения",
			NameEn = "Faster falling",
			Polarity = SubPerkPolarity.Negative,
			TaglineRu = "Быстрее, вкуснее",
			TaglineEn = "Faster, tastier",
			Accent = new Color("ff9a3c"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Crowding,
			NameRu = "Шанс мусора",
			NameEn = "Trash chance",
			Polarity = SubPerkPolarity.Negative,
			TaglineRu = "Всё валится",
			TaglineEn = "Everything tumbles",
			Accent = new Color("89a29d"),
		},
		new SubPerkDefinition
		{
			Id = SubPerkId.Insomnia,
			NameRu = "Меньше опыта",
			NameEn = "Less experience",
			Polarity = SubPerkPolarity.Negative,
			TaglineRu = "Мысли путаются",
			TaglineEn = "Thoughts get muddled",
			Accent = new Color("c9a9ff"),
		},
	};
}
