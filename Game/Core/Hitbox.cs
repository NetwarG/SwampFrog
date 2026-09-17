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

/// <summary>
/// Эллиптический хитбокс предмета: центр в мировых координатах, полуоси
/// (RadiusX — горизонтальная, RadiusY — вертикальная) и поворот в радианах.
/// Повторяет форму тела фрукта из спрайта (FruitCatalog.Body), поэтому пересечение
/// считается по реальным очертаниям, а не по прямоугольному холсту картинки.
/// </summary>
public record EllipseHitbox(Vector2 Center, float RadiusX, float RadiusY, float Rotation)
{
	private const float Tau = 6.2831853f;

	/// <summary>Точек на границе эллипса для проверки пересечения с кругом.</summary>
	private const int BoundarySamples = 14;

	/// <summary>Пересекается ли эллипс с кругом (например, с хитбоксом ладони).</summary>
	public bool Overlaps(CircleHitbox circle)
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