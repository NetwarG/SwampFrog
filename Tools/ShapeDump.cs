using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

namespace SwampFrog;

/// <summary>
/// Инструмент: снимает силуэт иконок (непрозрачную область спрайтов) и печатает
/// готовую таблицу для Game/Fruits/FruitShape.cs. Читает те же импортированные
/// текстуры, что рисует игра, поэтому данные совпадают с картинкой.
/// Запуск: godot --headless --path . res://Tools/ShapeDump.tscn
/// </summary>
public partial class ShapeDump : Node
{
	/// <summary>Порог альфы: ниже пиксель считается прозрачным.</summary>
	private const byte AlphaThreshold = 8;

	/// <summary>Допуск упрощения оболочки в долях от BaseRadius.</summary>
	private const float SimplifyTolerance = 0.01f;

	public override void _Ready()
	{
		var table = new StringBuilder();
		foreach (FruitKind kind in Enum.GetValues<FruitKind>())
		{
			Probe probe = Build(Path(kind.ToString().ToLowerInvariant()));
			table.Append($"\t\t[FruitKind.{kind}] = new FruitShape(new Vector2({F(probe.Center.X)}f, {F(probe.Center.Y)}f), new[]\n\t\t{{\n");
			foreach (Vector2 point in probe.Outline)
				table.Append($"\t\t\tnew Vector2({F(point.X)}f, {F(point.Y)}f),\n");
			table.Append("\t\t}),\n");
			GD.Print($"// {kind}: точек {probe.Outline.Length}, отклонение силуэта {F(probe.Deviation)} BaseRadius");
		}
		Probe trash = Build(Path("trash"));
		table.Append($"\tpublic static readonly FruitShape Trash = new(new Vector2({F(trash.Center.X)}f, {F(trash.Center.Y)}f), new[]\n\t{{\n");
		foreach (Vector2 point in trash.Outline)
			table.Append($"\t\tnew Vector2({F(point.X)}f, {F(point.Y)}f),\n");
		table.Append("\t});\n");
		GD.Print($"// trash: точек {trash.Outline.Length}, отклонение силуэта {F(trash.Deviation)} BaseRadius");
		GD.Print(table.ToString());
		GetTree().Quit();
	}

	/// <summary>Число в инвариантном формате: таблица должна быть с точкой, а не с запятой.</summary>
	private static string F(float value) => value.ToString("0.000", CultureInfo.InvariantCulture);

	private static string Path(string name) => $"res://Game/assets/fruits/{name}.png";

	/// <summary>Силуэт одного спрайта: центр непрозрачной области и упрощённая оболочка.</summary>
	private sealed record Probe(Vector2 Center, Vector2[] Outline, float Deviation);

	private static Probe Build(string path)
	{
		Image image = GD.Load<Texture2D>(path).GetImage();
		if (image.GetFormat() != Image.Format.Rgba8) image.Convert(Image.Format.Rgba8);
		int width = image.GetWidth();
		int height = image.GetHeight();
		float half = height * 0.5f;
		byte[] data = image.GetData();
		// Случайные точки арта (например, одиночная строка пикселей сверху) не должны
		// попадать в силуэт: берём только самую крупную связную область.
		bool[] body = LargestComponent(data, width, height);
		var boundary = new List<Vector2>();
		int minX = width;
		int minY = height;
		int maxX = -1;
		int maxY = -1;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (!body[y * width + x]) continue;
				minX = Mathf.Min(minX, x);
				maxX = Mathf.Max(maxX, x);
				minY = Mathf.Min(minY, y);
				maxY = Mathf.Max(maxY, y);
				// Внутренние пиксели силуэт не описывают — для оболочки нужны граничные.
				if (Opaque(body, width, height, x - 1, y) && Opaque(body, width, height, x + 1, y)
					&& Opaque(body, width, height, x, y - 1) && Opaque(body, width, height, x, y + 1)) continue;
				boundary.Add(new Vector2((x - width * 0.5f) / half, (y - height * 0.5f) / half));
			}
		}
		var center = new Vector2(
			((minX + maxX + 1) * 0.5f - width * 0.5f) / half,
			((minY + maxY + 1) * 0.5f - height * 0.5f) / half);
		Vector2[] outline = Simplify(ConvexHull(boundary), SimplifyTolerance);
		return new Probe(center, outline, Deviation(boundary, outline));
	}

	/// <summary>Крупнейшая связная область непрозрачных пикселей — сам предмет.</summary>
	private static bool[] LargestComponent(byte[] data, int width, int height)
	{
		var label = new int[width * height];
		var sizes = new List<int> { 0 };
		var queue = new Queue<int>();
		for (int start = 0; start < width * height; start++)
		{
			if (label[start] != 0 || data[start * 4 + 3] <= AlphaThreshold) continue;
			int id = sizes.Count;
			int size = 0;
			label[start] = id;
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				int index = queue.Dequeue();
				size++;
				int x = index % width;
				int y = index / width;
				Enqueue(data, label, queue, width, height, x - 1, y, id);
				Enqueue(data, label, queue, width, height, x + 1, y, id);
				Enqueue(data, label, queue, width, height, x, y - 1, id);
				Enqueue(data, label, queue, width, height, x, y + 1, id);
			}
			sizes.Add(size);
		}
		int best = 0;
		for (int i = 1; i < sizes.Count; i++)
			if (sizes[i] > sizes[best]) best = i;
		var body = new bool[width * height];
		for (int i = 0; i < label.Length; i++) body[i] = label[i] == best;
		return body;
	}

	private static void Enqueue(byte[] data, int[] label, Queue<int> queue, int width, int height, int x, int y, int id)
	{
		if (x < 0 || y < 0 || x >= width || y >= height) return;
		int index = y * width + x;
		if (label[index] != 0 || data[index * 4 + 3] <= AlphaThreshold) return;
		label[index] = id;
		queue.Enqueue(index);
	}

	private static bool Opaque(bool[] body, int width, int height, int x, int y) =>
		x >= 0 && y >= 0 && x < width && y < height && body[y * width + x];


	/// <summary>Выпуклая оболочка точек (обход Эндрю): форма тела не может быть вогнутой.</summary>
	private static Vector2[] ConvexHull(List<Vector2> points)
	{
		List<Vector2> sorted = points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
		if (sorted.Count < 3) return sorted.ToArray();
		var hull = new List<Vector2>();
		foreach (Vector2 point in sorted)
		{
			while (hull.Count >= 2 && Cross(hull[^2], hull[^1], point) <= 0f) hull.RemoveAt(hull.Count - 1);
			hull.Add(point);
		}
		int floor = hull.Count + 1;
		for (int i = sorted.Count - 2; i >= 0; i--)
		{
			Vector2 point = sorted[i];
			while (hull.Count >= floor && Cross(hull[^2], hull[^1], point) <= 0f) hull.RemoveAt(hull.Count - 1);
			hull.Add(point);
		}
		hull.RemoveAt(hull.Count - 1);
		return hull.ToArray();
	}

	/// <summary>Убирает вершины, которые почти не меняют форму: острые углы остаются.</summary>
	private static Vector2[] Simplify(Vector2[] hull, float tolerance)
	{
		var points = new List<Vector2>(hull);
		while (points.Count > 8)
		{
			int worst = -1;
			float worstDeviation = tolerance;
			for (int i = 0; i < points.Count; i++)
			{
				Vector2 previous = points[(i - 1 + points.Count) % points.Count];
				Vector2 next = points[(i + 1) % points.Count];
				float deviation = DistanceToSegment(points[i], previous, next);
				if (deviation <= worstDeviation)
				{
					worstDeviation = deviation;
					worst = i;
				}
			}
			if (worst < 0) break;
			points.RemoveAt(worst);
		}
		return points.ToArray();
	}

	/// <summary>Насколько силуэт выходит за упрощённую оболочку.</summary>
	private static float Deviation(List<Vector2> boundary, Vector2[] outline)
	{
		float worst = 0f;
		foreach (Vector2 point in boundary)
			if (!IsInside(point, outline)) worst = Mathf.Max(worst, DistanceToBoundary(point, outline));
		return worst;
	}

	private static float Cross(Vector2 a, Vector2 b, Vector2 c) =>
		(b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

	private static bool IsInside(Vector2 point, Vector2[] polygon)
	{
		bool positive = false;
		bool negative = false;
		for (int i = 0; i < polygon.Length; i++)
		{
			float cross = Cross(polygon[i], polygon[(i + 1) % polygon.Length], point);
			if (cross > 0.0001f) positive = true;
			if (cross < -0.0001f) negative = true;
			if (positive && negative) return false;
		}
		return true;
	}

	private static float DistanceToBoundary(Vector2 point, Vector2[] polygon)
	{
		float best = float.MaxValue;
		for (int i = 0; i < polygon.Length; i++)
			best = Mathf.Min(best, DistanceToSegment(point, polygon[i], polygon[(i + 1) % polygon.Length]));
		return best;
	}

	private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
	{
		Vector2 edge = b - a;
		float length = edge.LengthSquared();
		if (length < 0.0000001f) return point.DistanceTo(a);
		float t = Mathf.Clamp((point - a).Dot(edge) / length, 0f, 1f);
		return point.DistanceTo(a + edge * t);
	}
}

