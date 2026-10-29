using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Состояние субперков текущего забега: уровни, накопленные проценты и очередь выдачи.
/// Чистый C# класс без привязки к Godot-нодам, чтобы частоту и полярность предложений
/// можно было легко тестировать.
///
/// Субперки выдаются весь забег: максимального уровня нет. Чем выше уровень жабы,
/// тем реже предложения — интервал растёт от <see cref="MinInterval"/> до
/// <see cref="MaxInterval"/> ступенями по 10 уровней. Предложение никогда не попадает
/// на уровень основного перка (кратный <see cref="PerkState.OfferEveryLevels"/>),
/// поэтому под-уровни и уровни не пересекаются.
/// </summary>
public sealed class SubPerkState
{
	/// <summary>Первый уровень, на котором предлагается субперк.</summary>
	public const int FirstOfferLevel = 2;

	/// <summary>Начальный интервал между предложениями.</summary>
	public const int MinInterval = 2;

	/// <summary>Максимальный интервал между предложениями в позднем забеге.</summary>
	public const int MaxInterval = 10;

	private readonly Dictionary<SubPerkId, int> _levels = new();
	private readonly Dictionary<SubPerkId, int> _percents = new();
	private int _offersTaken;

	/// <summary>Уровень жабы, на котором ждём следующее предложение субперка.</summary>
	public int NextOfferLevel { get; private set; } = FirstOfferLevel;

	/// <summary>Полярность ближайшего предложения: чередуется через каждое взятие.</summary>
	public SubPerkPolarity NextPolarity => _offersTaken % 2 == 0 ? SubPerkPolarity.Positive : SubPerkPolarity.Negative;

	/// <summary>Сколько разных субперков уже взято за забег.</summary>
	public int SelectedCount => _levels.Count;

	/// <summary>Нужно ли предложить субперк на этом уровне жабы.</summary>
	public bool WantsOffer(int level)
	{
		return level > 0 && level >= NextOfferLevel;
	}

	/// <summary>Интервал до следующего предложения: 2/4/6/8/10 на каждые 10 уровней жабы.</summary>
	public static int IntervalForLevel(int level)
	{
		int band = Mathf.Max(0, (level - 1) / 10);
		return Mathf.Min(MaxInterval, MinInterval + band * 2);
	}

	/// <summary>Следующий уровень предложения: со сдвигом, если он совпал с уровнем основного перка.</summary>
	public static int NextOfferLevelAfter(int level)
	{
		int next = level + IntervalForLevel(level);
		if (next % PerkState.OfferEveryLevels == 0)
			next++;
		return next;
	}

	/// <summary>Текущий уровень субперка (0 — ещё не взят).</summary>
	public int LevelOf(SubPerkId id)
	{
		return _levels.TryGetValue(id, out int level) ? level : 0;
	}

	/// <summary>Суммарный накопленный процент субперка.</summary>
	public int PercentOf(SubPerkId id)
	{
		return _percents.TryGetValue(id, out int percent) ? percent : 0;
	}

	/// <summary>Предложение взято: очередь сдвигается, полярность меняется на противоположную.</summary>
	public void RegisterOfferTaken(int level)
	{
		NextOfferLevel = NextOfferLevelAfter(level);
		_offersTaken++;
	}

	/// <summary>Добавляет уровень и процент субперку (максимума нет).</summary>
	public void Increment(SubPerkId id, int percent)
	{
		_levels[id] = LevelOf(id) + 1;
		_percents[id] = PercentOf(id) + Mathf.Max(0, percent);
	}

	/// <summary>
	/// Случайные субперки для оверлея: только текущей полярности, без повторов,
	/// не более count. Максимального уровня нет, поэтому пул не исчерпывается.
	/// </summary>
	public SubPerkId[] OfferCandidates(int count, RandomNumberGenerator rng)
	{
		var pool = new List<SubPerkId>();
		foreach (SubPerkDefinition def in SubPerkCatalog.All)
		{
			if (def.Polarity == NextPolarity)
				pool.Add(def.Id);
		}

		var result = new List<SubPerkId>();
		while (result.Count < count && pool.Count > 0)
		{
			int index = rng.RandiRange(0, pool.Count - 1);
			result.Add(pool[index]);
			pool.RemoveAt(index);
		}
		return result.ToArray();
	}

	/// <summary>Взятые субперки в порядке каталога: имя, уровень и накопленный процент.</summary>
	public SubPerkSummary[] PickedSnapshot()
	{
		var result = new List<SubPerkSummary>();
		foreach (SubPerkDefinition def in SubPerkCatalog.All)
		{
			int level = LevelOf(def.Id);
			if (level > 0)
				result.Add(new SubPerkSummary(def.Name, level, PercentOf(def.Id), def.Polarity));
		}
		return result.ToArray();
	}

	/// <summary>Сброс на начало забега.</summary>
	public void Reset()
	{
		_levels.Clear();
		_percents.Clear();
		_offersTaken = 0;
		NextOfferLevel = FirstOfferLevel;
	}
}
