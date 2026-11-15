using Godot;

namespace SwampFrog;

/// <summary>
/// Пределы вылета руки: максимальная длина до дальнего угла поля и предел в конкретном
/// направлении. Рука отсчитывается от своего плеча, поэтому все замеры идут от переданного
/// якоря, а не от центра тела.
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
	/// Предел длины руки в направлении: срез по боковой кромке плюс подгонка по высоте
	/// с учётом провиса (ладонь опускается тем сильнее, чем длиннее рука).
	/// </summary>
	public static float LimitInDirection(Vector2 origin, Vector2 direction, Vector2 viewSize,
		float margin, float cap, in ArmSag.Profile sag, Vector2 down)
	{
		// Провис вертикальный: на предел по горизонтали он не влияет.
		float limit = Mathf.Min(MaxAlongX(origin, direction, viewSize, margin), cap);
		return sag.FitLengthInsideHeight(origin, direction, down, margin, viewSize.Y - margin, limit);
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
}
