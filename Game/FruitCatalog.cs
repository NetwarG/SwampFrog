using System.Linq;
using Godot;

namespace SwampFrog;

/// <summary>
/// Описание одного вида фрукта: отображаемое имя, радиус коллизии (физический размер)
/// и активность в «классическом» режиме (который виден сейчас).
///
/// Все описания собраны в едином каталоге, чтобы легко добавлять/менять/убирать фрукты.
/// </summary>
public record FruitSpec(FruitKind Kind, string Name, float BaseRadius, bool ActiveInClassic)
{
	/// <summary>Имя вида (для отладки/будущих UI-подписей).</summary>
	public string Name => Name;
}

/// <summary>Каталог всех фруктов и списки для режимов.</summary>
public sealed class FruitCatalog
{
	public static FruitSpec Get(FruitKind kind) => All.Where(f => f.Kind == kind).First();

	/// <summary>Все виды фруктов (включая те, что для других режимов).</summary>
	public static FruitSpec[] All =
	{
		new FruitSpec(FruitKind.Cherry, "Вишня", 12f, true),
		new FruitSpec(FruitKind.Strawberry, "Клубника", 14f, true),
		new FruitSpec(FruitKind.Grape, "Виноград", 11f, true),
		new FruitSpec(FruitKind.Mandarin, "Мандарин", 16f, true),
		new FruitSpec(FruitKind.Apple, "Яблоко", 17f, true),
		new FruitSpec(FruitKind.Pear, "Груша", 16f, true),
		new FruitSpec(FruitKind.Peach, "Персик", 17f, false),
		new FruitSpec(FruitKind.Pineapple, "Ананас", 19f, false),
		new FruitSpec(FruitKind.Melon, "Дыня", 22f, false),
		new FruitSpec(FruitKind.Watermelon, "Арбуз", 26f, false),
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