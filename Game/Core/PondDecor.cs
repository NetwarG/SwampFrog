using Godot;

namespace SwampFrog;

/// <summary>
/// Декор пруда: листья кувшинок и цветок из PNG (см. Tools/generate_icons.ps1).
/// Расставлен у нижней и боковых кромок поля, поэтому средняя полоса, где летят
/// фрукты, и место лягушки остаются чистыми. Палитра приглушена, а альфа меньше
/// единицы: фон не спорит с предметами, эффектами и их контурами.
/// </summary>
internal static class PondDecor
{
	private const string PadPath = "res://Game/assets/ui/lily_pad.png";
	private const string FlowerPath = "res://Game/assets/ui/lily_flower.png";

	/// <summary>Место декора: доли поля, радиус в логических единицах, поворот, отражение, альфа.</summary>
	private readonly record struct Spot(float X, float Y, float Radius, float Rotation, bool Flip, float Alpha, bool Flower);

	private static readonly Spot[] Spots =
	{
		new(0.075f, 0.470f, 20f, -0.18f, false, 0.78f, false),
		new(0.905f, 0.500f, 17f, 0.24f, true, 0.78f, false),
		new(0.135f, 0.560f, 23f, 0.10f, true, 0.80f, false),
		new(0.845f, 0.620f, 21f, -0.30f, false, 0.80f, false),
		new(0.480f, 0.600f, 18f, 0.40f, false, 0.78f, false),
		new(0.020f, 0.660f, 22f, 0.15f, true, 0.82f, false),
		new(0.700f, 0.700f, 25f, -0.12f, false, 0.82f, false),
		new(0.310f, 0.720f, 19f, 0.33f, true, 0.84f, false),
		new(0.975f, 0.760f, 20f, -0.25f, false, 0.84f, false),
		new(0.120f, 0.815f, 26f, -0.21f, false, 0.90f, false),
		new(0.560f, 0.830f, 17f, 0.22f, true, 0.88f, false),
		new(0.830f, 0.880f, 23f, 0.09f, true, 0.90f, false),
		new(0.060f, 0.935f, 19f, 0.12f, false, 0.85f, false),
		new(0.420f, 0.960f, 21f, -0.16f, false, 0.85f, false),
		new(0.640f, 0.520f, 12f, 0.20f, false, 0.60f, true),
		new(0.180f, 0.700f, 11f, -0.15f, false, 0.58f, true),
		new(0.760f, 0.905f, 12f, 0.05f, true, 0.62f, true),
	};

	private static Texture2D? _pad;
	private static Texture2D? _flower;

	/// <summary>Рисует декор поверх воды: позиции считаются от размера поля.</summary>
	public static void Draw(CanvasItem target, Vector2 field)
	{
		if (field.X <= 0f || field.Y <= 0f)
			return;
		foreach (Spot spot in Spots)
		{
			Texture2D? texture = spot.Flower ? Flower() : Pad();
			if (texture == null)
				continue;
			float size = Mathf.Max(1f, texture.GetWidth());
			float scale = spot.Radius * 2f / size;
			target.DrawSetTransformMatrix(new Transform2D(spot.Rotation,
				new Vector2(spot.Flip ? -scale : scale, scale), 0f,
				new Vector2(field.X * spot.X, field.Y * spot.Y)));
			target.DrawTexture(texture, new Vector2(-size * 0.5f, -size * 0.5f), new Color(1f, 1f, 1f, spot.Alpha));
		}
		target.DrawSetTransformMatrix(Transform2D.Identity);
	}

	private static Texture2D? Pad() => _pad ??= GD.Load<Texture2D>(PadPath);

	private static Texture2D? Flower() => _flower ??= GD.Load<Texture2D>(FlowerPath);
}
