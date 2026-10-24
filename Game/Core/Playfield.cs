using Godot;

namespace SwampFrog;

/// <summary>Размер мира и баланс не зависят от разрешения экрана.</summary>
public static class Playfield
{
	public static readonly Vector2 Size = new(540f, 960f);
	public static readonly Rect2 Bounds = new(Vector2.Zero, Size);

	public static Rect2 Fit(Rect2 available)
	{
		float scale = Mathf.Min(available.Size.X / Size.X, available.Size.Y / Size.Y);
		Vector2 size = Size * Mathf.Max(0f, scale);
		return new Rect2(available.GetCenter() - size * 0.5f, size);
	}
}
