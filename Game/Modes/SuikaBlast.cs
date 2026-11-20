using Godot;

namespace SwampFrog;

/// <summary>Формулы взрыва бомбы: затухание и направление импульса.</summary>
public static class SuikaBlast
{
	/// <summary>Сила импульса: 1 в эпицентре и 0 на границе радиуса.</summary>
	public static float Falloff(float distance, float radius)
	{
		if (radius <= 0f || distance >= radius) return 0f;
		return 1f - Mathf.Max(0f, distance) / radius;
	}

	/// <summary>
	/// Направление импульса: радиально от эпицентра, вертикаль ограничена,
	/// чтобы фрукты не выбрасывало через открытый верх кувшина.
	/// </summary>
	public static Vector2 Direction(Vector2 offset)
	{
		if (offset.LengthSquared() < 1f)
			return new Vector2(0f, SuikaMechanicsTuning.BombBlastLiftRatio);
		Vector2 radial = offset.Normalized();
		return new Vector2(radial.X, Mathf.Min(radial.Y, SuikaMechanicsTuning.BombBlastLiftRatio));
	}
}
