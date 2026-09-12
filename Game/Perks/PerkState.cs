using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Состояние перков текущего забега: уровень каждого перка и число занятых слотов.
/// Чистый C# класс (по образцу <see cref="XpSystem"/>), без привязки к Godot-нодам,
/// чтобы логику выбора и лимитов можно было легко тестировать и менять.
/// </summary>
public sealed class PerkState
{
	/// <summary>Раз в сколько уровней жабы предлагается выбор перка.</summary>
	public const int OfferEveryLevels = 6;

	/// <summary>Максимум улучшений (слотов) за один забег.</summary>
	public const int MaxSlots = 8;

	private readonly Dictionary<PerkId, int> _levels = new();

	/// <summary>Сколько улучшений уже взято за забег.</summary>
	public int PickedCount { get; private set; }

	/// <summary>Осталось ли место под новые улучшения.</summary>
	public bool HasFreeSlots => PickedCount < MaxSlots;

	/// <summary>Нужно ли предложить выбор перка на этом уровне жабы.</summary>
	public bool WantsOffer(int level)
	{
		return HasFreeSlots && level > 0 && level % OfferEveryLevels == 0;
	}

	/// <summary>Текущий уровень перка (0 — ещё не выбран).</summary>
	public int LevelOf(PerkId id)
	{
		return _levels.TryGetValue(id, out int level) ? level : 0;
	}

	/// <summary>Можно ли прокачать этот перк дальше.</summary>
	public bool CanPick(PerkId id)
	{
		return HasFreeSlots && LevelOf(id) < PerkCatalog.Find(id).MaxLevel;
	}

	/// <summary>Прокачивает перк на один уровень.</summary>
	/// <returns>false, если лимит слотов или максимальный уровень уже достигнуты.</returns>
	public bool Increment(PerkId id)
	{
		if (!CanPick(id))
		{
			return false;
		}
		_levels[id] = LevelOf(id) + 1;
		PickedCount++;
		return true;
	}

	/// <summary>Случайные доступные перки для оверлея (без повторов, не более count).</summary>
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
		PickedCount = 0;
	}
}