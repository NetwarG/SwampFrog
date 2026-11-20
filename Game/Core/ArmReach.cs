using Godot;

namespace SwampFrog;

/// <summary>
/// Пределы вылета руки: максимальная длина до дальнего угла поля и предел в конкретном
/// направлении по кромкам экрана. Рука отсчитывается от своего плеча, поэтому все замеры
/// идут от переданного якоря, а не от центра тела.
/// </summary>
public static class ArmReach
{
	/// <summary>Максимальная длина от якоря до самого дальнего угла видимой области с запасом.</summary>
	public static float ToViewCorners(Vector2 origin, Vector2 viewSize, float minReach, float margin)
	{
		float maxViewDist = origin.DistanceTo(Vector2.Zero);
		maxViewDist = Mathf.Max(maxViewDist, origin.DistanceTo(new Vector2(viewSize.X, 0f)));
		maxViewDist = Mathf.Max(maxViewDist, origin.DistanceTo(new Vector2(0f, viewSize.Y)));
		maxViewDist = Mathf.Max(maxViewDist, origin.DistanceTo(new Vector2(viewSize.X, viewSize.Y)));
		return Mathf.Max(minReach, maxViewDist * margin);
	}

	/// <summary>
	/// Предел длины руки в направлении: срез по боковым кромкам и по верхней/нижней,
	/// чтобы ладонь на любом вылете оставалась внутри поля.
	/// </summary>
	public static float LimitInDirection(Vector2 origin, Vector2 direction, Vector2 viewSize, float margin, float cap)
	{
		float limit = Mathf.Min(MaxAlongX(origin, direction, viewSize, margin), cap);
		limit = Mathf.Min(limit, MaxAlongY(origin, direction, viewSize, margin));
		return Mathf.Max(0f, limit);
	}

	/// <summary>Предел длины руки по горизонтали: дальше ладонь вышла бы за боковую кромку.</summary>
	private static float MaxAlongX(Vector2 origin, Vector2 direction, Vector2 viewSize, float margin)
	{
		if (direction.X > 0.0001f)
			return (viewSize.X - margin - origin.X) / direction.X;
		if (direction.X < -0.0001f)
			return (margin - origin.X) / direction.X;
		return float.PositiveInfinity;
	}

	/// <summary>Предел длины руки по вертикали: дальше ладонь вышла бы за верхнюю или нижнюю кромку.</summary>
	private static float MaxAlongY(Vector2 origin, Vector2 direction, Vector2 viewSize, float margin)
	{
		if (direction.Y > 0.0001f)
			return (viewSize.Y - margin - origin.Y) / direction.Y;
		if (direction.Y < -0.0001f)
			return (margin - origin.Y) / direction.Y;
		return float.PositiveInfinity;
	}
}
