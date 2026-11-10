using System.Collections.Generic;

namespace SwampFrog;

/// <summary>Состояние карточки скина в галерее.</summary>
public enum SkinCardState
{
	/// <summary>Скин выбран сейчас.</summary>
	Selected,

	/// <summary>Скин доступен и его можно выбрать.</summary>
	Owned,

	/// <summary>Скин можно купить за слизь.</summary>
	Buyable,

	/// <summary>Скин закрыт: нужно набрать рекорд.</summary>
	Locked,
}

/// <summary>
/// Пункт галереи скинов: скин и его состояние для показа.
/// <see cref="RequiredScore"/> и <see cref="RequiredSuika"/> описывают условие открытия.
/// </summary>
public sealed record SkinGalleryEntry(
	SkinId Id,
	string Name,
	SkinSlot Slot,
	SkinCardState State,
	int Price,
	int RequiredScore,
	bool RequiredSuika,
	bool Affordable);

/// <summary>Собирает пункты галереи из профиля и рекордов (см. SkinUnlockRules).</summary>
public static class SkinGalleryState
{
	public static SkinGalleryEntry[] Build(MetaProfile profile, int bestClassic, int bestSuika)
	{
		var result = new List<SkinGalleryEntry>();
		int coins = profile.Wallet.Balance;
		foreach (SkinDefinition def in SkinCatalog.All)
		{
			bool owned = profile.Owns(def.Id);
			bool unlocked = SkinUnlockRules.IsUnlocked(def, owned, bestClassic, bestSuika);
			SkinCardState state;
			if (profile.Selected(def.Slot) == def.Id)
				state = SkinCardState.Selected;
			else if (unlocked)
				state = SkinCardState.Owned;
			else if (def.RequiredBestClassic > 0 || def.RequiredBestSuika > 0)
				state = SkinCardState.Locked;
			else
				state = SkinCardState.Buyable;

			int required = def.RequiredBestClassic > 0 ? def.RequiredBestClassic : def.RequiredBestSuika;
			result.Add(new SkinGalleryEntry(def.Id, def.Name, def.Slot, state,
				def.Price, required, def.RequiredBestSuika > 0, coins >= def.Price));
		}
		return result.ToArray();
	}
}
