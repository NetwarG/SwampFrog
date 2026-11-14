using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Вырезы экрана (фронтальная камера, «монобровь»): верхние элементы интерфейса
/// не опускаются вниз, а расходятся по горизонтали в свободные от вырезов места.
/// </summary>
public static class ScreenCutouts
{
	/// <summary>Зазор между элементом интерфейса и вырезом.</summary>
	public const float Gap = 12f;

	private const float Epsilon = 0.01f;

	/// <summary>Прямоугольники вырезов в координатах канваса. Без вырезов — пустой массив.</summary>
	public static Rect2[] ForViewport(Viewport? viewport)
	{
		Rect2[] cutouts = System.Linq.Enumerable.ToArray(DisplayServer.GetDisplayCutouts());
		if (cutouts.Length == 0 || viewport == null)
			return cutouts;
		Transform2D toCanvas = viewport.GetFinalTransform().AffineInverse();
		Rect2[] canvas = new Rect2[cutouts.Length];
		for (int i = 0; i < cutouts.Length; i++)
		{
			Vector2 topLeft = toCanvas * cutouts[i].Position;
			Rect2 rect = new(topLeft, toCanvas * cutouts[i].End - topLeft);
			canvas[i] = rect.Abs();
		}
		return canvas;
	}

	/// <summary>Переводит вырезы из координат канваса в единицы интерфейса игровой колонки.</summary>
	public static Rect2[] InLocal(Rect2[] cutouts, Rect2 arenaRect, float uiScale)
	{
		if (cutouts.Length == 0)
			return cutouts;
		if (uiScale <= 0f)
			uiScale = 1f;
		Rect2[] local = new Rect2[cutouts.Length];
		for (int i = 0; i < cutouts.Length; i++)
			local[i] = new Rect2((cutouts[i].Position - arenaRect.Position) / uiScale, cutouts[i].Size / uiScale);
		return local;
	}

	/// <summary>Совпадают ли наборы вырезов: раскладка пересчитывается только при изменении.</summary>
	public static bool Same(Rect2[] a, Rect2[] b)
	{
		if (ReferenceEquals(a, b))
			return true;
		if (a.Length != b.Length)
			return false;
		for (int i = 0; i < a.Length; i++)
		{
			if (!a[i].Position.IsEqualApprox(b[i].Position) || !a[i].Size.IsEqualApprox(b[i].Size))
				return false;
		}
		return true;
	}

	/// <summary>Суммарная площадь перекрытия прямоугольника с вырезами.</summary>
	public static float OverlapArea(Rect2 rect, Rect2[] cutouts)
	{
		float area = 0f;
		foreach (Rect2 cutout in cutouts)
		{
			if (!rect.Intersects(cutout))
				continue;
			Rect2 common = rect.Intersection(cutout);
			area += common.Size.X * common.Size.Y;
		}
		return area;
	}

	/// <summary>Пересекается ли прямоугольник хотя бы с одним вырезом.</summary>
	public static bool Overlaps(Rect2 rect, Rect2[] cutouts) => OverlapArea(rect, cutouts) > 0f;

	/// <summary>
	/// Место прямоугольника у верхней кромки с наименьшим перекрытием вырезов,
	/// а при равном перекрытии — с наименьшим сдвигом. Высота не меняется: элемент
	/// остаётся на месте без вырезов и на вырезе по центру, отходит в сторону от кромки
	/// при вырезе у края и прижимается к своей кромке, если места рядом не хватает.
	/// minX и maxX ограничивают перемещение (края поля и соседние элементы интерфейса).
	/// </summary>
	public static Vector2 Avoid(Rect2 rect, Rect2[] cutouts, float minX, float maxX)
	{
		if (cutouts.Length == 0)
			return rect.Position;

		float bestX = Mathf.Clamp(rect.Position.X, minX, maxX);
		float bestOverlap = OverlapArea(Moved(rect, bestX), cutouts);
		float bestShift = Mathf.Abs(bestX - rect.Position.X);

		foreach (float candidate in Candidates(rect, cutouts))
		{
			float x = Mathf.Clamp(candidate, minX, maxX);
			float overlap = OverlapArea(Moved(rect, x), cutouts);
			float shift = Mathf.Abs(x - rect.Position.X);
			bool better = overlap < bestOverlap - Epsilon
				|| (overlap <= bestOverlap + Epsilon && shift < bestShift - Epsilon);
			if (!better)
				continue;
			bestX = x;
			bestOverlap = overlap;
			bestShift = shift;
		}
		return new Vector2(bestX, rect.Position.Y);
	}

	/// <summary>Позиции-кандидаты: место сразу справа и сразу слева от каждого выреза.</summary>
	private static IEnumerable<float> Candidates(Rect2 rect, Rect2[] cutouts)
	{
		yield return rect.Position.X;
		foreach (Rect2 cutout in cutouts)
		{
			yield return cutout.End.X + Gap;
			yield return cutout.Position.X - Gap - rect.Size.X;
		}
	}

	private static Rect2 Moved(Rect2 rect, float x) => new(new Vector2(x, rect.Position.Y), rect.Size);
}
