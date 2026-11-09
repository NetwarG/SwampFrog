using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Примитивы рисования стиля «стикер»: скруглённые полигоны, диагональный градиент
/// по вершинам, круги и глянцевые блики. Используются стилем кнопок <see cref="StickerStyle"/>.
/// </summary>
internal static class StickerDraw
{
	/// <summary>Сегментов на каждом скруглённом углу панели.</summary>
	internal const int CornerSegments = 6;

	private const int CircleSegments = 40;
	private const int EllipseSegments = 24;

	private static readonly Color Gloss = new(1f, 1f, 1f, 0.38f);
	private static readonly Color GlossDot = new(1f, 1f, 1f, 0.26f);

	/// <summary>Обход скруглённого прямоугольника: правый верх, правый низ, левый низ, левый верх.</summary>
	internal static Vector2[] RoundedPath(Rect2 rect, float topRight, float bottomRight, float bottomLeft, float topLeft)
	{
		float x = rect.Position.X;
		float y = rect.Position.Y;
		float w = rect.Size.X;
		float h = rect.Size.Y;
		// Радиус не может быть больше половины стороны: иначе скругления перекрывают друг друга.
		float limit = Mathf.Min(w, h) * 0.5f;
		topRight = Mathf.Min(topRight, limit);
		bottomRight = Mathf.Min(bottomRight, limit);
		bottomLeft = Mathf.Min(bottomLeft, limit);
		topLeft = Mathf.Min(topLeft, limit);
		var points = new List<Vector2>(4 * (CornerSegments + 1));
		AddCorner(points, new Vector2(x + w - topRight, y + topRight), topRight, -Mathf.Pi * 0.5f);
		AddCorner(points, new Vector2(x + w - bottomRight, y + h - bottomRight), bottomRight, 0f);
		AddCorner(points, new Vector2(x + bottomLeft, y + h - bottomLeft), bottomLeft, Mathf.Pi * 0.5f);
		AddCorner(points, new Vector2(x + topLeft, y + topLeft), topLeft, Mathf.Pi);
		return points.ToArray();
	}

	/// <summary>Заливка полигона диагональным градиентом: цвета задаются по вершинам.</summary>
	internal static void FillGradient(CanvasItem target, Vector2[] points, Color light, Color dark, Rect2 bounds)
	{
		var colors = new Color[points.Length];
		float w = Mathf.Max(1f, bounds.Size.X);
		float h = Mathf.Max(1f, bounds.Size.Y);
		for (int i = 0; i < points.Length; i++)
		{
			float tx = (points[i].X - bounds.Position.X) / w;
			float ty = (points[i].Y - bounds.Position.Y) / h;
			colors[i] = light.Lerp(dark, Mathf.Clamp((tx + ty) * 0.5f, 0f, 1f));
		}
		target.DrawPolygon(points, colors);
	}

	/// <summary>Круг заливки: замкнутый многоугольник, градиент идёт по его вершинам.</summary>
	internal static void FillCircleGradient(CanvasItem target, Vector2 center, float radius, Color light, Color dark)
	{
		// Без центральной вершины: веер [центр, дуга] замыкался через центр и оставлял
		// сверху незалитый клин, сквозь который просвечивал контур (как у загрузки).
		FillGradient(target, CirclePoints(center, radius), light, dark,
			new Rect2(center - Vector2.One * radius, Vector2.One * radius * 2f));
	}

	/// <summary>Сплошной круг: контур круглых кнопок.</summary>
	internal static void FillCircle(CanvasItem target, Vector2 center, float radius, Color color) =>
		target.DrawColoredPolygon(CirclePoints(center, radius), color);

	/// <summary>Точки круга: ими рисуются и заливка, и контур круглых кнопок.</summary>
	private static Vector2[] CirclePoints(Vector2 center, float radius)
	{
		var points = new Vector2[CircleSegments];
		for (int i = 0; i < CircleSegments; i++)
		{
			float angle = -Mathf.Pi * 0.5f + Mathf.Pi * 2f * i / CircleSegments;
			points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}
		return points;
	}

	/// <summary>Блик панели: вытянутое пятно слева сверху и мелкая точка под ним.</summary>
	internal static void PanelGloss(CanvasItem target, Rect2 rect)
	{
		float width = Mathf.Min(rect.Size.X * 0.18f, rect.Size.Y * 0.44f);
		float height = rect.Size.Y * 0.44f;
		if (width < 3f || height < 3f)
			return;
		var center = new Vector2(rect.Position.X + rect.Size.X * 0.16f, rect.Position.Y + rect.Size.Y * 0.36f);
		FillEllipse(target, center, width * 0.5f, height * 0.5f, Gloss);
		FillEllipse(target, center + new Vector2(width * 0.1f, height * 0.42f), width * 0.09f, width * 0.09f, GlossDot);
	}

	/// <summary>Блик круга: пятно слева сверху и мелкая точка ниже.</summary>
	internal static void CircleGloss(CanvasItem target, Vector2 center, float radius)
	{
		FillEllipse(target, center - new Vector2(radius * 0.3f, radius * 0.34f), radius * 0.3f, radius * 0.38f, Gloss);
		FillEllipse(target, center + new Vector2(-radius * 0.34f, radius * 0.06f), radius * 0.11f, radius * 0.13f, GlossDot);
	}

	private static void AddCorner(List<Vector2> points, Vector2 center, float radius, float start)
	{
		if (radius <= 0.01f)
		{
			Add(points, center);
			return;
		}
		for (int i = 0; i <= CornerSegments; i++)
		{
			float angle = start + Mathf.Pi * 0.5f * i / CornerSegments;
			Add(points, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
		}
	}

	/// <summary>Пропускает совпадающие точки: на дублях триангуляция полигона ломается.</summary>
	private static void Add(List<Vector2> points, Vector2 point)
	{
		if (points.Count > 0 && points[^1].DistanceSquaredTo(point) < 0.0001f)
			return;
		points.Add(point);
	}

	private static void FillEllipse(CanvasItem target, Vector2 center, float radiusX, float radiusY, Color color)
	{
		var points = new Vector2[EllipseSegments];
		for (int i = 0; i < EllipseSegments; i++)
		{
			float angle = Mathf.Pi * 2f * i / EllipseSegments;
			points[i] = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
		}
		target.DrawColoredPolygon(points, color);
	}
}
