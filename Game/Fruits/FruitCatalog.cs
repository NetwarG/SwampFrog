using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SwampFrog;

/// <summary>
/// Описание одного вида фрукта: отображаемое имя (русское и английское), базовый радиус
/// (половина высоты холста спрайта) и активность в «классическом» режиме. Форма тела берётся
/// по спрайту из <see cref="FruitShape"/>, поэтому описания остаются короткими.
/// </summary>
public record FruitSpec(FruitKind Kind, string NameRu, string NameEn, float BaseRadius, bool ActiveInClassic)
{
	/// <summary>Имя фрукта на текущем языке интерфейса.</summary>
	public string Name => Loc.Pick(NameRu, NameEn);
}

/// <summary>Каталог всех фруктов и списки для режимов.</summary>
public sealed class FruitCatalog
{
	public static FruitSpec Get(FruitKind kind) => All.Where(f => f.Kind == kind).First();

	/// <summary>Кэш текстур иконок: предметы и подсказка используют один и тот же файл.</summary>
	private static readonly Dictionary<FruitKind, Texture2D?> Icons = new();

	/// <summary>Текстура иконки фрукта из Game/assets/fruits или null, если ассета нет.</summary>
	public static Texture2D? Icon(FruitKind kind)
	{
		if (Icons.TryGetValue(kind, out Texture2D? cached)) return cached;
		Texture2D? loaded = GD.Load<Texture2D>($"res://Game/assets/fruits/{kind.ToString().ToLower()}.png");
		Icons[kind] = loaded;
		return loaded;
	}

	/// <summary>Все виды фруктов (включая те, что для других режимов).</summary>
	public static FruitSpec[] All =
	{
		new FruitSpec(FruitKind.Cherry, "Вишня", "Cherry", 24f, true),
		new FruitSpec(FruitKind.Strawberry, "Клубника", "Strawberry", 27f, true),
		new FruitSpec(FruitKind.Grape, "Виноград", "Grape", 20f, true),
		new FruitSpec(FruitKind.Mandarin, "Мандарин", "Mandarin", 31f, true),
		new FruitSpec(FruitKind.Apple, "Яблоко", "Apple", 33f, true),
		new FruitSpec(FruitKind.Pear, "Груша", "Pear", 33f, true),
		new FruitSpec(FruitKind.Peach, "Персик", "Peach", 34f, false),
		new FruitSpec(FruitKind.Pineapple, "Ананас", "Pineapple", 36f, false),
		new FruitSpec(FruitKind.Melon, "Дыня", "Melon", 42f, false),
		new FruitSpec(FruitKind.Watermelon, "Арбуз", "Watermelon", 50f, false),
	};

	/// <summary>Список фруктов, которые можно встретить в этом (классическом) режиме.</summary>
	public static FruitSpec[] Classic => All.Where(f => f.ActiveInClassic).ToArray();

	/// <summary>Случайный фрукт из активного списка.</summary>
	public static FruitKind PickClassic(RandomNumberGenerator rng)
	{
		FruitSpec[] pool = Classic;
		return pool.Length == 0 ? FruitKind.Cherry : pool[rng.RandiRange(0, pool.Length - 1)].Kind;
	}
}