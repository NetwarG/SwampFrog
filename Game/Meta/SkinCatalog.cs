using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SwampFrog;

/// <summary>Каталог скинов лягушки: данные, слои, иконки и доступные варианты.</summary>
public sealed class SkinCatalog
{
	/// <summary>Все скины, в порядке отображения по слотам.</summary>
	public static SkinDefinition[] All =
	{
		// Шляпа
		new(SkinId.HatNone, SkinSlot.Hat, "Нет", "None", 0),
		new(SkinId.PartyHat, SkinSlot.Hat, "Колпак", "Party hat", 75),
		new(SkinId.TopHat, SkinSlot.Hat, "Цилиндр", "Top hat", 120),
		new(SkinId.FlowerHat, SkinSlot.Hat, "Цветок", "Flower", 180),
		new(SkinId.MushroomHat, SkinSlot.Hat, "Гриб", "Mushroom", 0, RequiredBestClassic: 2500),

		// Перчатки
		new(SkinId.GlovesNone, SkinSlot.Gloves, "Нет", "None", 0),
		new(SkinId.BoxingGloves, SkinSlot.Gloves, "Боксёрские", "Boxing", 75),
		new(SkinId.MintGloves, SkinSlot.Gloves, "Мятные", "Mint", 120),
		new(SkinId.CaptainGloves, SkinSlot.Gloves, "Капитанские", "Captain", 180),
		new(SkinId.StripedGloves, SkinSlot.Gloves, "Полосатые", "Striped", 0, RequiredBestSuika: 1500),

		// Подставка
		new(SkinId.StandNone, SkinSlot.Stand, "Нет", "None", 0),
		new(SkinId.LilyPadStand, SkinSlot.Stand, "Кувшинка", "Lily pad", 60),
		new(SkinId.CarpetStand, SkinSlot.Stand, "Коврик", "Carpet", 105),
		new(SkinId.StoneStand, SkinSlot.Stand, "Камень", "Stone", 150),
		new(SkinId.StumpStand, SkinSlot.Stand, "Пенёк", "Stump", 0, RequiredBestClassic: 1500),
	};

	/// <summary>Скин по идентификатору.</summary>
	public static SkinDefinition Get(SkinId id) => All.First(s => s.Id == id);

	/// <summary>Скин по умолчанию для слота: вариант «Нет».</summary>
	public static SkinDefinition DefaultFor(SkinSlot slot) => All.First(s => s.Slot == slot && s.Price == 0
		&& s.RequiredBestClassic == 0 && s.RequiredBestSuika == 0);

	/// <summary>Все скины слота в порядке каталога.</summary>
	public static SkinDefinition[] ForSlot(SkinSlot slot) => All.Where(s => s.Slot == slot).ToArray();

	/// <summary>Слот скина.</summary>
	public static SkinSlot SlotOf(SkinId id) => Get(id).Slot;

	/// <summary>Вариант «Нет»: пустой слот без иконки.</summary>
	public static bool IsNone(SkinId id) => id == DefaultFor(SlotOf(id)).Id;

	/// <summary>
	/// Иконка скина из Game/assets/skins (имя файла = значение enum в нижнем регистре).
	/// У варианта «Нет» иконки нет. Арт легко заменить: положить свой PNG с тем же именем.
	/// </summary>
	public static Texture2D? Icon(SkinId id)
	{
		if (Icons.TryGetValue(id, out Texture2D? cached))
			return cached;
		Texture2D? loaded = IsNone(id)
			? null
			: GD.Load<Texture2D>($"res://Game/assets/skins/{id.ToString().ToLowerInvariant()}.png");
		Icons[id] = loaded;
		return loaded;
	}

	/// <summary>Кэш иконок: превью карточек и лягушка используют один файл.</summary>
	private static readonly Dictionary<SkinId, Texture2D?> Icons = new();

	/// <summary>Приводит произвольный id к слоту, подставляя вариант по умолчанию.</summary>
	public static SkinId Normalize(SkinSlot slot, SkinId id)
	{
		return SlotOfSafe(id) == slot ? id : DefaultFor(slot).Id;
	}

	private static SkinSlot SlotOfSafe(SkinId id)
	{
		foreach (SkinDefinition def in All)
		{
			if (def.Id == id)
				return def.Slot;
		}
		return SkinSlot.Hat;
	}
}
