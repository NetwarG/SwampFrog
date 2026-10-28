using Godot;

namespace SwampFrog;

/// <summary>
/// Круговой хитбокс: центр в мировых координатах и радиус в мировых единицах.
/// Используется для честной проверки касания ладони лягушки и падающего предмета:
/// поймать можно только когда хитбоксы реально пересекаются.
/// </summary>
public record CircleHitbox(Vector2 Center, float Radius)
{
	/// <summary>Пересекаются ли два хитбокса (фактическое касание).</summary>
	public bool Overlaps(CircleHitbox other)
	{
		float sum = Radius + other.Radius;
		return Center.DistanceSquaredTo(other.Center) <= sum * sum;
	}
}

/// <summary>Хитбокс предмета: с ним сравнивается круговая ладонь лягушки.</summary>
public abstract record ItemHitbox
{
	/// <summary>Пересекается ли хитбокс с кругом (фактическое касание).</summary>
	public abstract bool Overlaps(CircleHitbox circle);
}

/// <summary>
/// Эллиптический хитбокс предмета: центр в мировых координатах, полуоси
/// (RadiusX — горизонтальная, RadiusY — вертикальная) и поворот в радианах.
/// Нужен предметам без спрайта (хилка): у фруктов и мусора форму задаёт силуэт иконки
/// (см. <see cref="PolygonHitbox"/>).
/// </summary>
public record EllipseHitbox(Vector2 Center, float RadiusX, float RadiusY, float Rotation) : ItemHitbox
{
	private const float Tau = 6.2831853f;

	/// <summary>Точек на границе эллипса для проверки пересечения с кругом.</summary>
	private const int BoundarySamples = 14;

	/// <summary>Пересекается ли эллипс с кругом (например, с хитбоксом ладони).</summary>
	public override bool Overlaps(CircleHitbox circle)
	{
		Vector2 toCircle = circle.Center - Center;
		float circleRsq = circle.Radius * circle.Radius;
		if (toCircle.LengthSquared() <= circleRsq)
		{
			return true; // круг накрывает центр эллипса
		}
		Vector2 local = toCircle.Rotated(-Rotation);
		float hx = local.X / RadiusX;
		float hy = local.Y / RadiusY;
		if (hx * hx + hy * hy <= 1f)
		{
			return true; // центр круга внутри эллипса
		}
		for (int i = 0; i < BoundarySamples; i++)
		{
			float t = Tau * i / BoundarySamples;
			Vector2 edge = new Vector2(Mathf.Cos(t) * RadiusX, Mathf.Sin(t) * RadiusY).Rotated(Rotation);
			Vector2 d = Center + edge - circle.Center;
			if (d.LengthSquared() <= circleRsq)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Эффективный радиус эллипса вдоль направления dir (мировые координаты).
	/// Позволяет отталкивать эллипсы друг от друга по тем же формулам, что и круги.
	/// </summary>
	public float RadiusAlong(Vector2 dir)
	{
		Vector2 local = dir.Rotated(-Rotation);
		float nx = local.X / RadiusX;
		float ny = local.Y / RadiusY;
		float len = Mathf.Sqrt(nx * nx + ny * ny);
		return len < 0.0001f ? Mathf.Min(RadiusX, RadiusY) : 1f / len;
	}
}

/// <summary>
/// Полигональный хитбокс предмета: выпуклый силуэт иконки (см. <see cref="FruitShape"/>)
/// в мировых координатах. Ловля идёт по реальному контуру картинки, поэтому хитбокс
/// совпадает с нарисованным фруктом, а не с холстом спрайта.
/// </summary>
public record PolygonHitbox(Vector2 Center, float Rotation, float Radius, Vector2[] Outline) : ItemHitbox
{
	public override bool Overlaps(CircleHitbox circle)
	{
		// Круг переводится в локальные единицы силуэта: там единица измерения — Radius.
		Vector2 local = (circle.Center - Center).Rotated(-Rotation) / Radius;
		float reach = circle.Radius / Radius;
		if (IsInside(local)) return true;
		for (int i = 0; i < Outline.Length; i++)
		{
			if (DistanceToSegment(local, Outline[i], Outline[(i + 1) % Outline.Length]) <= reach) return true;
		}
		return false;
	}

	/// <summary>Точка внутри выпуклого силуэта (обход в любую сторону).</summary>
	private bool IsInside(Vector2 point)
	{
		bool positive = false;
		bool negative = false;
		for (int i = 0; i < Outline.Length; i++)
		{
			float cross = Cross(Outline[i], Outline[(i + 1) % Outline.Length], point);
			if (cross > 0.0001f) positive = true;
			if (cross < -0.0001f) negative = true;
			if (positive && negative) return false;
		}
		return true;
	}

	private static float Cross(Vector2 a, Vector2 b, Vector2 c) =>
		(b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

	private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
	{
		Vector2 edge = b - a;
		float length = edge.LengthSquared();
		if (length < 0.0000001f) return point.DistanceTo(a);
		float t = Mathf.Clamp((point - a).Dot(edge) / length, 0f, 1f);
		return point.DistanceTo(a + edge * t);
	}
}
