using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Состояние перков текущего забега: уровни выбранных перков и число занятых мест.
/// Чистый C# класс (по образцу <see cref="XpSystem"/>), без привязки к Godot-нодам,
/// чтобы логику выбора и лимитов можно было легко тестировать и менять.
/// </summary>
public sealed class PerkState
{
	/// <summary>Раз в сколько уровней жабы предлагается выбор перка.</summary>
	public const int OfferEveryLevels = 3;

	/// <summary>Сколько разных перков можно взять за один забег.</summary>
	public const int MaxDistinctPerks = 6;

	private readonly Dictionary<PerkId, int> _levels = new();

	/// <summary>Сколько разных перков уже взято за забег.</summary>
	public int SelectedCount => _levels.Count;

	/// <summary>Есть ли место под новый (ещё не выбранный) перк.</summary>
	public bool CanTakeNewPerk => SelectedCount < MaxDistinctPerks;

	/// <summary>Есть ли перк, который можно предложить в окне выбора.</summary>
	public bool HasOfferablePerk
	{
		get
		{
			foreach (PerkDefinition def in PerkCatalog.All)
			{
				if (CanPick(def.Id))
					return true;
			}
			return false;
		}
	}

	/// <summary>Нужно ли предложить выбор перка на этом уровне жабы.</summary>
	public bool WantsOffer(int level)
	{
		return level > 0 && level % OfferEveryLevels == 0 && HasOfferablePerk;
	}

	/// <summary>Текущий уровень перка (0 — ещё не выбран).</summary>
	public int LevelOf(PerkId id)
	{
		return _levels.TryGetValue(id, out int level) ? level : 0;
	}

	/// <summary>Перк выбран и достиг максимального уровня.</summary>
	public bool IsMaxed(PerkId id)
	{
		return LevelOf(id) >= PerkCatalog.Find(id).MaxLevel;
	}

	/// <summary>Сколько выбранных перков докачано до максимума.</summary>
	public int MaxedSelectedCount
	{
		get
		{
			int count = 0;
			foreach (PerkId id in _levels.Keys)
			{
				if (IsMaxed(id))
					count++;
			}
			return count;
		}
	}

	/// <summary>Можно ли прокачать этот перк дальше: новый перк — при свободном месте,
	/// уже выбранный — до максимального уровня.</summary>
	public bool CanPick(PerkId id)
	{
		int level = LevelOf(id);
		if (level <= 0)
			return CanTakeNewPerk;
		return level < PerkCatalog.Find(id).MaxLevel;
	}

	/// <summary>Прокачивает перк на один уровень.</summary>
	/// <returns>false, если нет места под новый перк или максимальный уровень уже достигнут.</returns>
	public bool Increment(PerkId id)
	{
		if (!CanPick(id))
		{
			return false;
		}
		_levels[id] = LevelOf(id) + 1;
		return true;
	}

	/// <summary>Выбранные перки в порядке каталога: идентификатор и уровень.</summary>
	public PerkIconInfo[] PickedSnapshot()
	{
		var result = new List<PerkIconInfo>();
		foreach (PerkDefinition def in PerkCatalog.All)
		{
			int level = LevelOf(def.Id);
			if (level > 0)
			{
				result.Add(new PerkIconInfo(def.Id, level));
			}
		}
		return result.ToArray();
	}

	/// <summary>Случайные перки для оверлея: пока выбрано меньше <see cref="MaxDistinctPerks"/> —
	/// любые доступные, после — только из уже выбранных (без повторов, не более count).</summary>
	public PerkId[] OfferCandidates(int count, RandomNumberGenerator rng)
	{
		var pool = new List<PerkId>();
		foreach (PerkDefinition def in PerkCatalog.All)
		{
			if (CanPick(def.Id))
			{
				pool.Add(def.Id);
			}
		}

		var result = new List<PerkId>();
		while (result.Count < count && pool.Count > 0)
		{
			int index = rng.RandiRange(0, pool.Count - 1);
			result.Add(pool[index]);
			pool.RemoveAt(index);
		}
		return result.ToArray();
	}

	/// <summary>Сброс на начало забега.</summary>
	public void Reset()
	{
		_levels.Clear();
	}
}