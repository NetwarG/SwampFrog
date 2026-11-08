using Godot;

namespace SwampFrog;

/// <summary>
/// Палитра и примитивы эффектов перков. Эффекты намеренно ярче фона пруда:
/// цвета взяты из палитры иконок игры, а контур держит форму на любом фоне.
/// </summary>
internal static class EffectStyle
{
	/// <summary>Контур эффектов, как у иконок: держит форму поверх воды и декора.</summary>
	internal static readonly Color Outline = new("2b170f");

	internal static readonly Color Gold = new("ffd32e");
	internal static readonly Color Spark = new("ff9a3c");
	internal static readonly Color Frost = new("7fe3d5");
	internal static readonly Color Ice = new("d8f6ff");
	internal static readonly Color Syrup = new("7acbb2");
	internal static readonly Color Stone = new("b9c6c2");
	internal static readonly Color White = new(1f, 1f, 1f);

	/// <summary>Кольцо с тёмной обводкой: ударные волны и вспышки.</summary>
	internal static void Ring(CanvasItem target, Vector2 center, float radius, Color color, float width, float alpha = 1f)
	{
		if (radius <= 0.5f || alpha <= 0f)
			return;
		target.DrawCircle(center, radius, new Color(Outline, alpha), false, width + 3f);
		target.DrawCircle(center, radius, new Color(color, alpha), false, width);
	}

	/// <summary>Точка с обводкой: искры и вспышки в точке удара.</summary>
	internal static void Dot(CanvasItem target, Vector2 center, float radius, Color color, float alpha = 1f)
	{
		if (radius <= 0.3f || alpha <= 0f)
			return;
		target.DrawCircle(center, radius, new Color(Outline, alpha * 0.85f));
		target.DrawCircle(center, radius * 0.7f, new Color(color, alpha));
	}

	/// <summary>Ломаная с тёмной обводкой: молния языка.</summary>
	internal static void Polyline(CanvasItem target, Vector2[] points, Color color, float width, float alpha = 1f)
	{
		if (points.Length < 2 || alpha <= 0f)
			return;
		target.DrawPolyline(points, new Color(Outline, alpha), width + 3f);
		target.DrawPolyline(points, new Color(color, alpha), width);
	}
}
