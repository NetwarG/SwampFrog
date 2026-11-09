using Godot;

namespace SwampFrog;

/// <summary>
/// Фон мини-игры Suika: тёмный пруд со светлой полосой поверхности на уровне кромки
/// кувшина, декор из Game/assets/ui (кувшинки и цветки) и мягкие блики. Декор держится
/// у боковых полей, на дне и за стеклом, поэтому полоса падения остаётся чистой.
/// </summary>
internal static class SuikaBackdrop
{
	/// <summary>Полоса поверхности у кромки кувшина: высота, яркость и линия воды.</summary>
	private const float SurfaceBand = 34f;
	private const float SurfaceAlpha = 0.18f;
	private const float SurfaceLineAlpha = 0.3f;

	/// <summary>Тень над поверхностью (глубина за водой) и у нижней кромки поля.</summary>
	private const float TopShadeAlpha = 0.34f;
	private const float BottomShadeDepth = 0.12f;
	private const float BottomShadeAlpha = 0.22f;

	private const string PadPath = "res://Game/assets/ui/lily_pad.png";
	private const string FlowerPath = "res://Game/assets/ui/lily_flower.png";

	/// <summary>Поверхность воды и её глубина: светлее прежнего фона Suika, но темнее меню.</summary>
	private static readonly Color Surface = new("2b8f7c");
	private static readonly Color Depth = new("07201c");

	private static Texture2D? _pad;
	private static Texture2D? _flower;

	/// <summary>Место декора: доля ширины поля, доля высоты кувшина от кромки, радиус, поворот, отражение, альфа, цветок.</summary>
	private readonly record struct Spot(float X, float Y, float Radius, float Rotation, bool Flip, float Alpha, bool Flower);

	/// <summary>
	/// Декор: кувшинки у боковых полей и на дне, приглушённые кувшинки за стеклом
	/// (стекло полупрозрачное — они дают объём), цветки у самой кромки воды.
	/// </summary>
	private static readonly Spot[] Spots =
	{
		new(0.040f, 0.06f, 22f, -0.18f, false, 0.74f, false),
		new(0.960f, 0.16f, 19f, 0.26f, true, 0.70f, false),
		new(0.050f, 0.34f, 18f, 0.12f, true, 0.68f, false),
		new(0.952f, 0.48f, 21f, -0.30f, false, 0.72f, false),
		new(0.035f, 0.62f, 24f, 0.20f, false, 0.76f, false),
		new(0.965f, 0.76f, 22f, 0.09f, true, 0.76f, false),
		new(0.055f, 0.88f, 20f, -0.14f, true, 0.78f, false),
		new(0.945f, 0.94f, 18f, 0.22f, false, 0.78f, false),
		new(0.240f, 0.22f, 27f, 0.15f, false, 0.46f, false),
		new(0.760f, 0.38f, 24f, -0.22f, true, 0.44f, false),
		new(0.300f, 0.70f, 29f, 0.08f, true, 0.42f, false),
		new(0.630f, 0.88f, 26f, -0.16f, false, 0.42f, false),
		new(0.120f, 1.08f, 20f, 0.15f, false, 0.66f, false),
		new(0.880f, 1.10f, 18f, -0.18f, true, 0.66f, false),
		new(0.035f, 0.02f, 11f, 0.20f, false, 0.66f, true),
		new(0.965f, 0.06f, 10f, -0.10f, true, 0.60f, true),
		new(0.055f, 0.44f, 11f, 0.05f, false, 0.60f, true),
		new(0.940f, 0.66f, 10f, -0.14f, true, 0.58f, true),
	};

	/// <summary>Блики на воде: доля поля, радиус и прозрачность.</summary>
	private static readonly (float X, float Y, float Radius, float Alpha)[] Glints =
	{
		(0.20f, 0.05f, 5f, 0.07f),
		(0.72f, 0.03f, 4f, 0.06f),
		(0.08f, 0.30f, 4f, 0.05f),
		(0.93f, 0.44f, 4.5f, 0.05f),
		(0.06f, 0.72f, 5f, 0.06f),
		(0.95f, 0.68f, 3.5f, 0.05f),
		(0.12f, 0.97f, 4f, 0.05f),
	};

	/// <summary>Рисует фон: воду, полосу поверхности у кромки кувшина, блики и декор.</summary>
	public static void Draw(CanvasItem target, Vector2 field, Rect2 bowl)
	{
		if (field.X <= 0f || field.Y <= 0f) return;
		DrawWater(target, field);
		DrawSurface(target, field, bowl.Position.Y);
		DrawBottomShade(target, field);
		DrawGlints(target, field);
		DrawSpots(target, field, bowl);
	}

	/// <summary>Вода: вертикальный градиент от светлой поверхности к глубине.</summary>
	private static void DrawWater(CanvasItem target, Vector2 field) =>
		StickerDraw.FillVerticalGradient(target, new Rect2(Vector2.Zero, field), Surface, Depth);

	/// <summary>Глубина за водой и светлая полоса у самой кромки кувшина — линия воды.</summary>
	private static void DrawSurface(CanvasItem target, Vector2 field, float rim)
	{
		float top = Mathf.Max(0f, rim - SurfaceBand);
		// Тень над полосой поверхности: за водой должна читаться глубина.
		StickerDraw.FillVerticalGradient(target, new Rect2(0f, 0f, field.X, top),
			new Color(Depth, TopShadeAlpha), new Color(Depth, 0f));
		// Свет возвращается у самой кромки кувшина: у поверхности вода светлее.
		StickerDraw.FillVerticalGradient(target, new Rect2(0f, top, field.X, SurfaceBand),
			new Color(Surface, 0f), new Color(Surface, SurfaceAlpha));

		target.DrawLine(new Vector2(0f, rim), new Vector2(field.X, rim),
			new Color(Surface, SurfaceLineAlpha), 2f);
	}

	/// <summary>Мягкое затемнение нижней кромки поля: декор у дна «садится» на место.</summary>
	private static void DrawBottomShade(CanvasItem target, Vector2 field)
	{
		float depth = field.Y * BottomShadeDepth;
		StickerDraw.FillVerticalGradient(target, new Rect2(0f, field.Y - depth, field.X, depth),
			new Color(Depth, 0f), new Color(Depth, BottomShadeAlpha));
	}

	/// <summary>Блики на воде: мягкие пятна без новых ассетов.</summary>
	private static void DrawGlints(CanvasItem target, Vector2 field)
	{
		foreach ((float x, float y, float radius, float alpha) in Glints)
			target.DrawCircle(new Vector2(field.X * x, field.Y * y), radius, new Color(1f, 1f, 1f, alpha));
	}

	/// <summary>Декор: листья и цветки из Game/assets/ui со своим поворотом и прозрачностью.</summary>
	private static void DrawSpots(CanvasItem target, Vector2 field, Rect2 bowl)
	{
		foreach (Spot spot in Spots)
		{
			Texture2D? texture = spot.Flower ? Flower() : Pad();
			if (texture == null)
				continue;
			float size = Mathf.Max(1f, texture.GetWidth());
			float scale = spot.Radius * 2f / size;
			// Высота считается от кромки кувшина: декор держится у воды и стенок, а не «плывёт» по экрану.
			Vector2 position = new(field.X * spot.X, bowl.Position.Y + bowl.Size.Y * spot.Y);
			target.DrawSetTransformMatrix(new Transform2D(spot.Rotation,
				new Vector2(spot.Flip ? -scale : scale, scale), 0f, position));
			target.DrawTexture(texture, new Vector2(-size * 0.5f, -size * 0.5f), new Color(1f, 1f, 1f, spot.Alpha));
		}
		target.DrawSetTransformMatrix(Transform2D.Identity);
	}

	private static Texture2D? Pad() => _pad ??= GD.Load<Texture2D>(PadPath);

	private static Texture2D? Flower() => _flower ??= GD.Load<Texture2D>(FlowerPath);
}
