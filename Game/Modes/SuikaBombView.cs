using Godot;

namespace SwampFrog;

/// <summary>Вид бомбы Suika: тёмный шар с контуром, блик и искрящий фитиль.</summary>
public partial class SuikaBombView : Node2D
{
	private static readonly Color Shell = new("2a2f36");
	private static readonly Color ShellLight = new("404852");
	private static readonly Color Outline = new("2b170f");
	private static readonly Color Fuse = new("c9825a");
	private static readonly Color Spark = new("ffb43c");

	/// <summary>Радиус бомбы в мировых единицах.</summary>
	public float Radius { get; set; } = SuikaMechanicsTuning.BombRadius;

	public override void _Draw()
	{
		float r = Radius;
		if (r <= 0f) return;
		DrawCircle(Vector2.Zero, r, Outline);
		DrawCircle(Vector2.Zero, r - 2.5f, Shell);
		DrawCircle(new Vector2(-r * 0.28f, -r * 0.3f), r * 0.46f, ShellLight);
		DrawCircle(new Vector2(-r * 0.38f, -r * 0.42f), r * 0.2f, new Color(1f, 1f, 1f, 0.55f));
		Vector2 fuseStart = new(r * 0.42f, -r * 0.78f);
		Vector2 fuseMid = fuseStart + new Vector2(r * 0.28f, -r * 0.26f);
		Vector2 fuseTip = fuseMid + new Vector2(r * 0.44f, -r * 0.12f);
		DrawPolyline(new[] { fuseStart, fuseMid, fuseTip }, Fuse, Mathf.Max(2f, r * 0.15f));
		DrawCircle(fuseTip, Mathf.Max(2f, r * 0.2f), Spark);
		DrawCircle(fuseTip, Mathf.Max(1f, r * 0.1f), new Color(1f, 1f, 0.84f));
	}
}
