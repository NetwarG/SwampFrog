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