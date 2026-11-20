using Godot;

namespace SwampFrog;

/// <summary>
/// Якоря-плечи лягушки: точки на боках тела, из которых растут руки, и веер ладоней у пальца.
/// Смещения заданы в локальных единицах тела — левый бок у нулевой руки, правый у первой.
/// Веер разводит ладони по сторонам от оси «тело → палец», поэтому они не сливаются
/// и не крестятся при любом направлении прицела.
/// </summary>
public static class ArmAnchor
{
	/// <summary>
	/// Смещение плеча от центра тела по горизонтали, локальные единицы. Плечо лежит чуть
	/// внутри силуэта, чтобы корень руки выходил из тела без зазора во всех позах.
	/// </summary>
	public const float Side = 25f;

	/// <summary>Смещение плеча вниз от центра тела, локальные единицы.</summary>
	public const float Drop = 6f;

	/// <summary>Полуугол веера ладоней от оси «тело → палец», градусы.</summary>
	public const float FanDeg = 6f;

	/// <summary>Смещение плеча руки в локальных единицах (0 — левая, 1 — правая).</summary>
	public static Vector2 Local(int handIndex) => new(handIndex == 0 ? -Side : Side, Drop);

	/// <summary>Мировая точка плеча: смещение разворачивается вместе с телом.</summary>
	public static Vector2 World(Vector2 bodyCenter, float rotation, int handIndex) =>
		bodyCenter + Local(handIndex).Rotated(rotation);

	/// <summary>
	/// Точка прицела руки: палец, сдвинутый вбок перпендикулярно оси «тело → палец» в сторону
	/// своей руки. Сдвиг сам гаснет, когда палец ложится вдоль линии плеч, поэтому руки
	/// расходятся в стороны и никогда не крестятся.
	/// </summary>
	public static Vector2 AimPoint(Vector2 bodyCenter, float rotation, Vector2 aim, int handIndex)
	{
		Vector2 axis = aim - bodyCenter;
		float length = axis.Length();
		if (length <= 0.0001f)
			return aim;
		Vector2 axisUnit = axis / length;
		Vector2 separation = new Vector2(2f * Side, 0f).Rotated(rotation);
		Vector2 lateral = separation - axisUnit * separation.Dot(axisUnit);
		float spread = length * Mathf.Sin(Mathf.DegToRad(FanDeg));
		float side = handIndex == 0 ? -1f : 1f;
		return aim + lateral * (side * spread / separation.Length());
	}

	/// <summary>Направление руки: от её плеча к сдвинутой точке прицела.</summary>
	public static Vector2 ToTarget(Vector2 bodyCenter, float rotation, Vector2 aim, Vector2 origin, int handIndex)
	{
		Vector2 to = AimPoint(bodyCenter, rotation, aim, handIndex) - origin;
		return to.LengthSquared() > 0.0001f ? to.Normalized() : Vector2.Right;
	}
}
