using System.Linq;
using Godot;

namespace SwampFrog;

/// <summary>
/// Форма тела фрукта для хитбокса: полуоси эллипса в долях от BaseRadius.
/// Значения подобраны по непрозрачной области спрайта (Game/assets/fruits),
/// поэтому хитбокс повторяет реальные очертания фрукта, а не холст картинки.
/// </summary>
public record FruitBody(float HitX, float HitY)
{
}

/// <summary>
/// Описание одного вида фрукта: отображаемое имя, базовый радиус (половина высоты
/// холста спрайта), форма тела для хитбокса и активность в «классическом» режиме.
///
/// Все описания собраны в едином каталоге, чтобы легко добавлять/менять/убирать фрукты.
/// </summary>
public record FruitSpec(FruitKind Kind, string Name, float BaseRadius, FruitBody Body, bool ActiveInClassic)
{
}

/// <summary>Каталог всех фруктов и списки для режимов.</summary>
public sealed class FruitCatalog
{
	public static FruitSpec Get(FruitKind kind) => All.Where(f => f.Kind == kind).First();

	/// <summary>Все виды фруктов (включая те, что для других режимов).</summary>
	public static FruitSpec[] All =
	{
		new FruitSpec(FruitKind.Cherry, "Вишня", 24f, new FruitBody(0.680f, 0.701f), true),
		new FruitSpec(FruitKind.Strawberry, "Клубника", 27f, new FruitBody(0.589f, 0.751f), true),
		new FruitSpec(FruitKind.Grape, "Виноград", 20f, new FruitBody(0.626f, 0.823f), true),
		new FruitSpec(FruitKind.Mandarin, "Мандарин", 31f, new FruitBody(0.637f, 0.702f), true),
		new FruitSpec(FruitKind.Apple, "Яблоко", 33f, new FruitBody(0.673f, 0.756f), true),
		new FruitSpec(FruitKind.Pear, "Груша", 33f, new FruitBody(0.555f, 0.797f), true),
		new FruitSpec(FruitKind.Peach, "Персик", 34f, new FruitBody(0.660f, 0.942f), false),
		new FruitSpec(FruitKind.Pineapple, "Ананас", 36f, new FruitBody(0.540f, 0.948f), false),
		new FruitSpec(FruitKind.Melon, "Дыня", 42f, new FruitBody(0.691f, 0.779f), false),
		new FruitSpec(FruitKind.Watermelon, "Арбуз", 50f, new FruitBody(0.714f, 0.807f), false),
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