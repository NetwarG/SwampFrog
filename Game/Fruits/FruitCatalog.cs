using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SwampFrog;

/// <summary>
/// Описание одного вида фрукта: отображаемое имя, базовый радиус (половина высоты
/// холста спрайта) и активность в «классическом» режиме. Форма тела берётся по спрайту
/// из <see cref="FruitShape"/>, поэтому описания остаются короткими.
/// </summary>
public record FruitSpec(FruitKind Kind, string Name, float BaseRadius, bool ActiveInClassic)
{
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
		new FruitSpec(FruitKind.Cherry, "Вишня", 24f, true),
		new FruitSpec(FruitKind.Strawberry, "Клубника", 27f, true),
		new FruitSpec(FruitKind.Grape, "Виноград", 20f, true),
		new FruitSpec(FruitKind.Mandarin, "Мандарин", 31f, true),
		new FruitSpec(FruitKind.Apple, "Яблоко", 33f, true),
		new FruitSpec(FruitKind.Pear, "Груша", 33f, true),
		new FruitSpec(FruitKind.Peach, "Персик", 34f, false),
		new FruitSpec(FruitKind.Pineapple, "Ананас", 36f, false),
		new FruitSpec(FruitKind.Melon, "Дыня", 42f, false),
		new FruitSpec(FruitKind.Watermelon, "Арбуз", 50f, false),
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