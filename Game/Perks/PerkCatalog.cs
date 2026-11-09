using System.Collections.Generic;
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
	private static readonly Dictionary<PerkId, Texture2D?> Icons = new();

	public static Texture2D? Icon(PerkId id)
	{
		if (Icons.TryGetValue(id, out Texture2D? cached))
			return cached;
		Texture2D? loaded = GD.Load<Texture2D>($"res://Game/assets/perks/{IconName(id)}.png");
		Icons[id] = loaded;
		return loaded;
	}

	private static string IconName(PerkId id)
	{
		return id switch
		{
			PerkId.StickyPaws => "sticky_paws",
			PerkId.RubberGrip => "rubber_grip",
			PerkId.RapidFire => "rapid_fire",
			PerkId.SlowZone => "slow_zone",
			PerkId.GoldRush => "gold_rush",
			PerkId.FruitExplosion => "fruit_explosion",
			PerkId.ComboMaster => "combo_master",
			PerkId.Shell => "shell",
			PerkId.TrashBounce => "trash_bounce",
			PerkId.SecondLife => "second_life",
			PerkId.LightningTongue => "lightning_tongue",
			PerkId.BasiliskGaze => "basilisk_gaze",
			_ => "sticky_paws",
		};
	}

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
			NameRu = "Липкие ладони",
			NameEn = "Sticky Paws",
			TaglineRu = "Фрукты прилипают",
			TaglineEn = "Fruits stick",
			MaxLevel = 5,
			Accent = new Color("b4e863"),
			LevelEffectsRu = new string[]
			{
				"Радиус захвата ладони +30%",
				"Радиус захвата ладони +60%",
				"Радиус захвата ладони +90%",
				"Магнит: фрукты в радиусе 2 м притягиваются к руке",
				"Магнит 4 м + можно ловить 2 фрукта одновременно",
			},
			LevelEffectsEn = new string[]
			{
				"Palm catch radius +30%",
				"Palm catch radius +60%",
				"Palm catch radius +90%",
				"Magnet: fruits within 2 m are pulled to the hand",
				"Magnet 4 m + you can catch 2 fruits at once",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.RubberGrip,
			NameRu = "Резиновый хват",
			NameEn = "Rubber Grip",
			TaglineRu = "Отскок спасёт",
			TaglineEn = "A bounce will save you",
			MaxLevel = 3,
			Accent = new Color("ffd45e"),
			LevelEffectsRu = new string[]
			{
				"Пойманный фрукт даёт +1 к счёту",
				"Если рука коснулась мусора — 15% шанс отбросить его",
				"30% шанс отбросить мусор",
			},
			LevelEffectsEn = new string[]
			{
				"A caught fruit gives +1 to the score",
				"If the hand touches trash — 15% chance to deflect it",
				"30% chance to deflect trash",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.RapidFire,
			NameRu = "Скорострельность",
			NameEn = "Rapid Fire",
			TaglineRu = "Как пулемёт",
			TaglineEn = "Like a machine gun",
			MaxLevel = 5,
			Accent = new Color("ff8c7a"),
			LevelEffectsRu = new string[]
			{
				"Скорость вытягивания рук +20%",
				"Скорость втягивания рук +20%",
				"Общая скорость рук +30%",
				"Общая скорость рук +40%",
				"Общая скорость рук +50%",
			},
			LevelEffectsEn = new string[]
			{
				"Arm extension speed +20%",
				"Arm retraction speed +20%",
				"Overall arm speed +30%",
				"Overall arm speed +40%",
				"Overall arm speed +50%",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.SlowZone,
			NameRu = "Зона замедления",
			NameEn = "Slow Zone",
			TaglineRu = "Фрукты в сиропе",
			TaglineEn = "Fruits in syrup",
			MaxLevel = 5,
			Accent = new Color("7acbb2"),
			LevelEffectsRu = new string[]
			{
				"Падение фруктов замедляется на 15% в нижней трети экрана",
				"Замедление 25% в нижней трети экрана",
				"Замедление 35% в нижней половине экрана",
				"Мусор замедляется сильнее фруктов (40%)",
				"Полная остановка на 1 сек при поимке золотого фрукта (перезарядка 5 сек)",
			},
			LevelEffectsEn = new string[]
			{
				"Fruit falling slows by 15% in the bottom third of the screen",
				"25% slowdown in the bottom third of the screen",
				"35% slowdown in the bottom half of the screen",
				"Trash slows down more than fruit (40%)",
				"Full stop for 1 sec when a golden fruit is caught (5 sec cooldown)",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.GoldRush,
			NameRu = "Золотая лихорадка",
			NameEn = "Gold Rush",
			TaglineRu = "Шанс на богатство",
			TaglineEn = "A chance at riches",
			MaxLevel = 5,
			Accent = new Color("ffd32e"),
			LevelEffectsRu = new string[]
			{
				"+5% шанс появления золотого фрукта (x5 очков)",
				"+10% шанс появления золотого фрукта",
				"+15% шанс, золотой фрукт даёт x7 очков",
				"+20% шанс, золотой падает медленнее обычного",
				"Каждый 10-й фрукт гарантированно золотой (x10 очков)",
			},
			LevelEffectsEn = new string[]
			{
				"+5% golden fruit spawn chance (x5 score)",
				"+10% golden fruit spawn chance",
				"+15% chance, golden fruit gives x7 score",
				"+20% chance, golden falls slower than usual",
				"Every 10th fruit is guaranteed golden (x10 score)",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.FruitExplosion,
			NameRu = "Фруктовый взрыв",
			NameEn = "Fruit Explosion",
			TaglineRu = "Цепная реакция",
			TaglineEn = "Chain reaction",
			MaxLevel = 5,
			Accent = new Color("ff9a3c"),
			LevelEffectsRu = new string[]
			{
				"Пойманный фрукт даёт +1 очко, если рядом другой фрукт",
				"Взрыв: пойманный фрукт собирает фрукты в радиусе 1 м",
				"Радиус взрыва 2 м + бонус за каждый собранный фрукт",
				"Взрыв уничтожает мусор рядом",
				"Каскад: взрывы могут вызывать новые взрывы",
			},
			LevelEffectsEn = new string[]
			{
				"A caught fruit gives +1 point if another fruit is nearby",
				"Explosion: a caught fruit collects fruits within 1 m",
				"Blast radius 2 m + bonus for each collected fruit",
				"The blast destroys nearby trash",
				"Cascade: explosions can trigger new explosions",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.ComboMaster,
			NameRu = "Комбо-мастер",
			NameEn = "Combo Master",
			TaglineRu = "Серии решают",
			TaglineEn = "Streaks matter",
			MaxLevel = 5,
			Accent = new Color("c9a9ff"),
			LevelEffectsRu = new string[]
			{
				"Комбо не сбрасывается при пропуске фрукта",
				"Комбо-множитель растёт быстрее (+2 за поимку)",
				"Множитель x3 вместо x2",
				"Комбо даёт +1 HP каждые 10 серии",
				"Бесконечное комбо: +3 за поимку, множитель до x10",
			},
			LevelEffectsEn = new string[]
			{
				"Combo doesn't reset when a fruit is missed",
				"Combo multiplier grows faster (+2 per catch)",
				"Multiplier x3 instead of x2",
				"Combo gives +1 HP every 10 streak",
				"Endless combo: +3 per catch, multiplier up to x10",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.Shell,
			NameRu = "Панцирь",
			NameEn = "Shell",
			TaglineRu = "Жаба-черепаха",
			TaglineEn = "Turtle-toad",
			MaxLevel = 5,
			Accent = new Color("8fd05a"),
			LevelEffectsRu = new string[]
			{
				"+1 HP (всего 4 вместо 3)",
				"+2 HP (всего 5)",
				"+3 HP (всего 6)",
				"+4 HP (всего 7)",
				"Неуязвимость на 2 сек после получения урона",
			},
			LevelEffectsEn = new string[]
			{
				"+1 HP (4 instead of 3)",
				"+2 HP (5 total)",
				"+3 HP (6 total)",
				"+4 HP (7 total)",
				"Invulnerability for 2 sec after taking damage",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.TrashBounce,
			NameRu = "Отбрасывание мусора",
			NameEn = "Trash Deflect",
			TaglineRu = "Фу, гадость!",
			TaglineEn = "Yuck, gross!",
			MaxLevel = 3,
			Accent = new Color("89a29d"),
			LevelEffectsRu = new string[]
			{
				"Мусор отскакивает при касании (не наносит урон) с шансом 10%",
				"Шанс отскока мусора 20%",
				"Шанс отскока мусора 30%",
			},
			LevelEffectsEn = new string[]
			{
				"Trash bounces off on contact (deals no damage) with a 10% chance",
				"20% trash bounce chance",
				"30% trash bounce chance",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.SecondLife,
			NameRu = "Вторая жизнь",
			NameEn = "Second Life",
			TaglineRu = "Жаба-феникс",
			TaglineEn = "Phoenix-toad",
			MaxLevel = 5,
			Accent = new Color("ffb2a0"),
			LevelEffectsRu = new string[]
			{
				"Один раз за игру при HP=0 остаётся 1 HP",
				"Один раз за игру при HP=0 остаётся 3 HP",
				"Возрождение работает два раза за игру",
				"+2 сек неуязвимости после возрождения",
				"Три раза + полная очистка экрана от мусора",
			},
			LevelEffectsEn = new string[]
			{
				"Once per game, at HP=0 you keep 1 HP",
				"Once per game, at HP=0 you keep 3 HP",
				"Revive works twice per game",
				"+2 sec invulnerability after reviving",
				"Three times + a full screen clear of trash",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.LightningTongue,
			NameRu = "Язык-молния",
			NameEn = "Lightning Tongue",
			TaglineRu = "Жаба-электрик",
			TaglineEn = "Electric-toad",
			MaxLevel = 4,
			Accent = new Color("ffe066"),
			LevelEffectsRu = new string[]
			{
				"Раз в 20 сек авто-ловля ближайшего фрукта языком",
				"Авто-ловля раз в 18 сек",
				"Раз в 15 сек + можно поймать 2 фрукта",
				"Раз в 10 сек + язык уничтожает ближайший мусор",
			},
			LevelEffectsEn = new string[]
			{
				"Every 20 sec, auto-catch the nearest fruit with the tongue",
				"Auto-catch every 18 sec",
				"Every 15 sec + can catch 2 fruits",
				"Every 10 sec + the tongue destroys the nearest trash",
			},
		},
		new PerkDefinition
		{
			Id = PerkId.BasiliskGaze,
			NameRu = "Взгляд василиска",
			NameEn = "Basilisk Gaze",
			TaglineRu = "Застынь!",
			TaglineEn = "Freeze!",
			MaxLevel = 5,
			Accent = new Color("7fe3d5"),
			LevelEffectsRu = new string[]
			{
				"Каждые 30 сек замораживает все фрукты на 1 сек",
				"Каждые 25 сек замораживает на 2 сек",
				"Заморозка превращает мусор в камень (безопасен)",
				"Каждые 12 сек замораживает на 3 сек",
				"Замороженные фрукты дают x2 очков",
			},
			LevelEffectsEn = new string[]
			{
				"Every 30 sec freezes all fruits for 1 sec",
				"Every 25 sec freezes for 2 sec",
				"Freezing turns trash into stone (harmless)",
				"Every 12 sec freezes for 3 sec",
				"Frozen fruits give x2 score",
			},
		},
	};
}