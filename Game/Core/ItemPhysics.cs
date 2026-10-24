using Godot;

namespace SwampFrog;

/// <summary>Общие формы предметов и независимые слои столкновений двух режимов.</summary>
public static class ItemPhysics
{
	public const uint ClassicLayer = 1u;
	public const uint SuikaLayer = 1u << 1;

	public static ConvexPolygonShape2D Ellipse(float radiusX, float radiusY)
	{
		var points = new Vector2[24];
		for (int i = 0; i < points.Length; i++)
		{
			float angle = Mathf.Tau * i / points.Length;
			points[i] = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
		}
		return new ConvexPolygonShape2D { Points = points };
	}
}
