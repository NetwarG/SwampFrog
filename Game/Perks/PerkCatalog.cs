using Godot;

namespace SwampFrog;

/// <summary>
/// Каталог всех перков игры. Каждый перк описывается здесь: имя, слоган,
/// число уровней и тексты эффектов. Числовая механика — в <see cref="PerkEffects"/>.
///
/// Добавить новый перк = значение в <see cref="PerkId"/> + запись в этом массиве.
/// </summary>
public sealed class PerkCatalog
{
	public static PerkDefinition Find(PerkId id)
	{
		foreach (PerkDefinition def in All)
		{
			if (def.Id == id)
			{
				return def;
			}
		}
		return All[0];
	}

	public static PerkDefinition[] All =
	{
		new PerkDefinition
		{
			Id = PerkId.StickyPaws,
			Name = "Липкие ладони",
			Tagline = "Фрукты прилипают",
			MaxLevel = 5,
			Accent = new Color("b4e863"),
			LevelEffects = new string[]
			{
				"Радиус захвата ладони +30%",
				"Радиус захвата ладони +60%",
				"Радиус захвата ладони +90%",
				"Магнит: фрукты в радиусе 2 м притягиваются к руке",
				"Магнит 4 м + можно ловить 2 фрукта одновременно",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.RubberGrip,
			Name = "Резиновый хват",
			Tagline = "Отскок спасёт",
			MaxLevel = 3,
			Accent = new Color("ffd45e"),
			LevelEffects = new string[]
			{
				"Пойманный фрукт даёт +1 к счёту",
				"Если рука коснулась мусора — 15% шанс отбросить его",
				"30% шанс отбросить мусор",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.RapidFire,
			Name = "Скорострельность",
			Tagline = "Как пулемёт",
			MaxLevel = 5,
			Accent = new Color("ff8c7a"),
			LevelEffects = new string[]
			{
				"Скорость вытягивания рук +20%",
				"Скорость втягивания рук +20%",
				"Общая скорость рук +30%",
				"Общая скорость рук +40%",
				"Общая скорость рук +50%",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.SlowZone,
			Name = "Зона замедления",
			Tagline = "Фрукты в сиропе",
			MaxLevel = 5,
			Accent = new Color("7acbb2"),
			LevelEffects = new string[]
			{
				"Падение фруктов замедляется на 15% в нижней трети экрана",
				"Замедление 25% в нижней трети экрана",
				"Замедление 35% в нижней половине экрана",
				"Мусор замедляется сильнее фруктов (40%)",
				"Полная остановка на 1 сек при появлении золотого фрукта",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.GoldRush,
			Name = "Золотая лихорадка",
			Tagline = "Шанс на богатство",
			MaxLevel = 5,
			Accent = new Color("ffd32e"),
			LevelEffects = new string[]
			{
				"+5% шанс появления золотого фрукта (x5 очков)",
				"+10% шанс появления золотого фрукта",
				"+15% шанс, золотой фрукт даёт x7 очков",
				"+20% шанс, золотой падает медленнее обычного",
				"Каждый 10-й фрукт гарантированно золотой (x10 очков)",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.FruitExplosion,
			Name = "Фруктовый взрыв",
			Tagline = "Цепная реакция",
			MaxLevel = 5,
			Accent = new Color("ff9a3c"),
			LevelEffects = new string[]
			{
				"Пойманный фрукт даёт +1 очко, если рядом другой фрукт",
				"Взрыв: пойманный фрукт собирает фрукты в радиусе 1 м",
				"Радиус взрыва 2 м + бонус за каждый собранный фрукт",
				"Взрыв уничтожает мусор рядом",
				"Каскад: взрывы могут вызывать новые взрывы",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.ComboMaster,
			Name = "Комбо-мастер",
			Tagline = "Серии решают",
			MaxLevel = 5,
			Accent = new Color("c9a9ff"),
			LevelEffects = new string[]
			{
				"Комбо не сбрасывается при пропуске фрукта",
				"Комбо-множитель растёт быстрее (+2 за поимку)",
				"Множитель x3 вместо x2",
				"Комбо даёт +1 HP каждые 10 серии",
				"Бесконечное комбо: +3 за поимку, множитель до x10",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.Shell,
			Name = "Панцирь",
			Tagline = "Жаба-черепаха",
			MaxLevel = 5,
			Accent = new Color("8fd05a"),
			LevelEffects = new string[]
			{
				"+1 HP (всего 4 вместо 3)",
				"+2 HP (всего 5)",
				"+3 HP (всего 6)",
				"+4 HP (всего 7)",
				"Неуязвимость на 2 сек после получения урона",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.TrashBounce,
			Name = "Отбрасывание мусора",
			Tagline = "Фу, гадость!",
			MaxLevel = 3,
			Accent = new Color("89a29d"),
			LevelEffects = new string[]
			{
				"Мусор отскакивает при касании (не наносит урон) с шансом 10%",
				"Шанс отскока мусора 20%",
				"Шанс отскока мусора 30%",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.SecondLife,
			Name = "Вторая жизнь",
			Tagline = "Жаба-феникс",
			MaxLevel = 5,
			Accent = new Color("ffb2a0"),
			LevelEffects = new string[]
			{
				"Один раз за игру при HP=0 остаётся 1 HP",
				"Один раз за игру при HP=0 остаётся 3 HP",
				"Возрождение работает два раза за игру",
				"+2 сек неуязвимости после возрождения",
				"Три раза + полная очистка экрана от мусора",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.LightningTongue,
			Name = "Язык-молния",
			Tagline = "Жаба-электрик",
			MaxLevel = 4,
			Accent = new Color("ffe066"),
			LevelEffects = new string[]
			{
				"Раз в 30 сек авто-ловля ближайшего фрукта языком",
				"Авто-ловля раз в 20 сек",
				"Раз в 10 сек + можно поймать 2 фрукта",
				"Раз в 5 сек + язык уничтожает ближайший мусор",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.BasiliskGaze,
			Name = "Взгляд василиска",
			Tagline = "Застынь!",
			MaxLevel = 5,
			Accent = new Color("7fe3d5"),
			LevelEffects = new string[]
			{
				"Каждые 40 сек замораживает все фрукты на 1 сек",
				"Каждые 30 сек замораживает на 2 сек",
				"Заморозка превращает мусор в камень (безопасен)",
				"Каждые 15 сек замораживает на 3 сек",
				"Замороженные фрукты дают x2 очков",
			},
		},
	};
}