using Godot;

namespace SwampFrog;

/// <summary>
/// Фон главного меню: светлая вода пруда с кувшинками, цветками и парящими фруктами,
/// разбросанными по всему экрану. Центральная полоса под заголовком и кнопками
/// держится спокойнее, поэтому надписи остаются читаемыми. Заливка непрозрачная:
/// тёмный игровой пруд и его декор под меню не просвечивают.
/// </summary>
public partial class MenuBackground : Control
{
	/// <summary>Полос градиента воды: чем больше, тем плавнее переход.</summary>
	private const int Steps = 64;

	private const string PadPath = "res://Game/assets/ui/lily_pad.png";
	private const string FlowerPath = "res://Game/assets/ui/lily_flower.png";

	/// <summary>Светлая поверхность воды и её глубина: темнее прежнего, но светлее игрового пруда.</summary>
	private static readonly Color Top = new("4fac93");
	private static readonly Color Bottom = new("1e7462");

	/// <summary>Вид объекта: лист кувшинки, цветок или фрукт.</summary>
	private enum Motif
	{
		Pad,
		Flower,
		Fruit,
	}

	/// <summary>Место объекта: доли экрана, радиус, поворот, отражение, прозрачность и вид.</summary>
	private readonly record struct Spot(float X, float Y, float Radius, float Rotation, bool Flip, float Alpha,
		Motif Kind, FruitKind Fruit);

	/// <summary>Светлый блик на воде: место, радиус и прозрачность.</summary>
	private readonly record struct Glint(float X, float Y, float Radius, float Alpha);

	/// <summary>Объекты меню: идут сверху вниз, в центре мельче и прозрачнее ради текста.</summary>
	private static readonly Spot[] Spots =
	{
		new(0.060f, 0.100f, 18f, -0.20f, false, 0.85f, Motif.Pad, FruitKind.Cherry),
		new(0.860f, 0.080f, 16f, 0.30f, true, 0.80f, Motif.Pad, FruitKind.Cherry),
		new(0.300f, 0.050f, 15f, 0.15f, true, 0.75f, Motif.Pad, FruitKind.Cherry),
		new(0.660f, 0.140f, 20f, -0.40f, false, 0.85f, Motif.Pad, FruitKind.Cherry),
		new(0.120f, 0.240f, 17f, 0.25f, true, 0.80f, Motif.Pad, FruitKind.Cherry),
		new(0.920f, 0.300f, 19f, -0.15f, false, 0.80f, Motif.Pad, FruitKind.Cherry),
		new(0.050f, 0.420f, 20f, 0.10f, true, 0.60f, Motif.Pad, FruitKind.Cherry),
		new(0.950f, 0.500f, 18f, 0.35f, false, 0.62f, Motif.Pad, FruitKind.Cherry),
		new(0.100f, 0.620f, 22f, -0.25f, false, 0.80f, Motif.Pad, FruitKind.Cherry),
		new(0.840f, 0.700f, 24f, 0.20f, true, 0.85f, Motif.Pad, FruitKind.Cherry),
		new(0.030f, 0.740f, 21f, 0.12f, true, 0.85f, Motif.Pad, FruitKind.Cherry),
		new(0.450f, 0.800f, 19f, -0.16f, false, 0.85f, Motif.Pad, FruitKind.Cherry),
		new(0.220f, 0.900f, 26f, -0.21f, false, 0.90f, Motif.Pad, FruitKind.Cherry),
		new(0.620f, 0.920f, 22f, 0.09f, true, 0.90f, Motif.Pad, FruitKind.Cherry),
		new(0.900f, 0.860f, 20f, -0.30f, false, 0.88f, Motif.Pad, FruitKind.Cherry),
		new(0.750f, 0.600f, 17f, 0.28f, true, 0.70f, Motif.Pad, FruitKind.Cherry),
		new(0.480f, 0.130f, 12f, 0.20f, false, 0.70f, Motif.Flower, FruitKind.Cherry),
		new(0.160f, 0.520f, 11f, -0.15f, false, 0.55f, Motif.Flower, FruitKind.Cherry),
		new(0.700f, 0.440f, 11f, 0.05f, true, 0.55f, Motif.Flower, FruitKind.Cherry),
		new(0.340f, 0.700f, 12f, 0.22f, false, 0.72f, Motif.Flower, FruitKind.Cherry),
		new(0.800f, 0.960f, 11f, -0.10f, true, 0.70f, Motif.Flower, FruitKind.Cherry),
		new(0.800f, 0.200f, 13f, 0.30f, false, 0.90f, Motif.Fruit, FruitKind.Cherry),
		new(0.200f, 0.330f, 14f, -0.20f, true, 0.45f, Motif.Fruit, FruitKind.Apple),
		new(0.520f, 0.620f, 12f, 0.40f, false, 0.45f, Motif.Fruit, FruitKind.Peach),
		new(0.080f, 0.560f, 15f, -0.35f, false, 0.50f, Motif.Fruit, FruitKind.Mandarin),
		new(0.440f, 0.240f, 12f, 0.18f, true, 0.40f, Motif.Fruit, FruitKind.Grape),
		new(0.880f, 0.620f, 14f, 0.24f, false, 0.72f, Motif.Fruit, FruitKind.Strawberry),
		new(0.300f, 0.840f, 16f, -0.12f, true, 0.82f, Motif.Fruit, FruitKind.Pear),
		new(0.620f, 0.380f, 11f, -0.30f, false, 0.35f, Motif.Fruit, FruitKind.Watermelon),
	};

	/// <summary>Мягкие блики на воде: приглушены, чтобы не выбеливать середину экрана.</summary>
	private static readonly Glint[] Glints =
	{
		new(0.180f, 0.160f, 5f, 0.11f),
		new(0.720f, 0.100f, 4f, 0.09f),
		new(0.400f, 0.300f, 3.5f, 0.08f),
		new(0.880f, 0.440f, 4.5f, 0.09f),
		new(0.100f, 0.480f, 3f, 0.08f),
		new(0.550f, 0.760f, 5f, 0.10f),
		new(0.260f, 0.660f, 3.5f, 0.08f),
		new(0.780f, 0.820f, 4f, 0.09f),
		new(0.460f, 0.940f, 3.5f, 0.08f),
		new(0.940f, 0.720f, 3f, 0.08f),
	};

	private static Texture2D? _pad;
	private static Texture2D? _flower;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		Resized += QueueRedraw;
	}

	public override void _Draw()
	{
		if (Size.X <= 0f || Size.Y <= 0f)
			return;
		DrawWater();
		DrawGlints();
		DrawSpots();
	}

	/// <summary>Светлая вода: вертикальный градиент от поверхности к глубине.</summary>
	private void DrawWater()
	{
		float strip = Size.Y / Steps;
		for (int i = 0; i < Steps; i++)
		{
			float t = (i + 0.5f) / Steps;
			DrawRect(new Rect2(0f, i * strip, Size.X, strip + 1f), Top.Lerp(Bottom, t));
		}
	}

	/// <summary>Блики рисуются кодом: они дают воде объём без новых ассетов.</summary>
	private void DrawGlints()
	{
		foreach (Glint glint in Glints)
		{
			DrawCircle(new Vector2(Size.X * glint.X, Size.Y * glint.Y), glint.Radius, new Color(1f, 1f, 1f, glint.Alpha));
		}
	}

	/// <summary>Объекты: листья, цветки и фрукты, каждый со своим поворотом и прозрачностью.</summary>
	private void DrawSpots()
	{
		foreach (Spot spot in Spots)
		{
			Texture2D? texture = Resolve(spot.Kind, spot.Fruit);
			if (texture == null)
				continue;
			float size = Mathf.Max(1f, texture.GetWidth());
			float scale = spot.Radius * 2f / size;
			DrawSetTransformMatrix(new Transform2D(spot.Rotation,
				new Vector2(spot.Flip ? -scale : scale, scale), 0f,
				new Vector2(Size.X * spot.X, Size.Y * spot.Y)));
			DrawTexture(texture, new Vector2(-size * 0.5f, -size * 0.5f), new Color(1f, 1f, 1f, spot.Alpha));
		}
		DrawSetTransformMatrix(Transform2D.Identity);
	}

	private static Texture2D? Resolve(Motif kind, FruitKind fruit)
	{
		return kind switch
		{
			Motif.Pad => Pad(),
			Motif.Flower => Flower(),
			_ => FruitCatalog.Icon(fruit),
		};
	}

	private static Texture2D? Pad()
	{
		return _pad ??= GD.Load<Texture2D>(PadPath);
	}

	private static Texture2D? Flower()
	{
		return _flower ??= GD.Load<Texture2D>(FlowerPath);
	}
}
