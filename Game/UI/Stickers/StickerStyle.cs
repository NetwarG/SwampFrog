using Godot;

namespace SwampFrog;

/// <summary>
/// Стиль «стикер» для кнопок игры: тот же вид, что у иконок в Game/assets —
/// толстый коричневый контур, диагональный градиент заливки и глянцевый блик.
/// Геометрию рисует <see cref="StickerDraw"/>.
/// </summary>
public static class StickerStyle
{
	/// <summary>Цвет контура иконок (см. Tools/generate_icons.ps1).</summary>
	public static readonly Color Outline = new("2b170f");

	/// <summary>Толщина контура кнопок в логических единицах.</summary>
	public const float OutlineWidth = 4f;

	/// <summary>Осветлённый тон заливки: верхний левый угол градиента.</summary>
	public static Color Light(Color accent) => accent.Lightened(0.22f);

	/// <summary>Затемнённый тон заливки: нижний правый угол градиента.</summary>
	public static Color Dark(Color accent) => accent.Darkened(0.3f);

	/// <summary>Скруглённая панель стикера: коричневый контур, градиент и блик.</summary>
	public static void DrawPanel(CanvasItem target, Rect2 rect, float radius, Color light, Color dark,
		float outlineWidth, bool gloss) =>
		DrawPanel(target, rect, radius, radius, radius, radius, light, dark, outlineWidth, gloss);

	/// <summary>То же с разными скруглениями: панель паузы скруглена только слева.</summary>
	public static void DrawPanel(CanvasItem target, Rect2 rect, float topRight, float bottomRight,
		float bottomLeft, float topLeft, Color light, Color dark, float outlineWidth, bool gloss)
	{
		if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
			return;

		float width = Mathf.Min(outlineWidth, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.5f);
		if (width > 0f)
			target.DrawColoredPolygon(
				StickerDraw.RoundedPath(rect, topRight, bottomRight, bottomLeft, topLeft), Outline);

		Rect2 inner = rect.Grow(-width);
		if (inner.Size.X <= 0f || inner.Size.Y <= 0f)
			return;
		Vector2[] fill = StickerDraw.RoundedPath(inner, Inside(topRight, width), Inside(bottomRight, width),
			Inside(bottomLeft, width), Inside(topLeft, width));
		StickerDraw.FillGradient(target, fill, light, dark, inner);
		if (gloss)
			StickerDraw.PanelGloss(target, inner);
	}

	/// <summary>Круглая кнопка стикера: коричневый контур, градиент и блик.</summary>
	public static void DrawCirclePanel(CanvasItem target, Vector2 center, float radius, Color light, Color dark,
		float outlineWidth, bool gloss)
	{
		if (radius <= 0f)
			return;

		float width = Mathf.Min(outlineWidth, radius * 0.5f);
		if (width > 0f)
			StickerDraw.FillCircle(target, center, radius, Outline);
		float inner = radius - width;
		if (inner <= 0f)
			return;
		StickerDraw.FillCircleGradient(target, center, inner, light, dark);
		if (gloss)
			StickerDraw.CircleGloss(target, center, inner);
	}

	/// <summary>Невидимый стиль с внутренними отступами: задаёт кнопке поля текста и иконки.</summary>
	public static StyleBoxFlat MakeContentStyle(float marginX, float marginY)
	{
		var style = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0f), DrawCenter = false };
		style.SetBorderWidthAll(0);
		style.ContentMarginLeft = marginX;
		style.ContentMarginRight = marginX;
		style.ContentMarginTop = marginY;
		style.ContentMarginBottom = marginY;
		return style;
	}

	/// <summary>Радиус внутреннего угла: контур съедает свою толщину.</summary>
	private static float Inside(float radius, float width) => Mathf.Max(0f, radius - width);
}
