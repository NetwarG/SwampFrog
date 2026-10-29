using Godot;

namespace SwampFrog;

/// <summary>
/// Круглая полупрозрачная кнопка со знаком «?» в правом верхнем углу мини-игры.
/// Собственный Control, а не Button: круг рисуется кодом, а знак ставится подписью по центру.
/// </summary>
public partial class HelpCircleButton : TouchArea
{
	/// <summary>Диаметр кнопки в логических единицах.</summary>
	public const float Diameter = 64f;

	/// <summary>Отступ кнопки от правого края поля.</summary>
	public const float MarginRight = 12f;

	private static readonly Color Fill = new(0.02f, 0.12f, 0.09f, 0.42f);
	private static readonly Color Outline = new("9ee4c3", 0.5f);
	private static readonly Color Glyph = new(0.93f, 1f, 0.97f, 0.85f);

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(Diameter, Diameter);
		Size = new Vector2(Diameter, Diameter);

		var label = new Label
		{
			Text = "?",
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		label.AddThemeFontSizeOverride("font_size", 34);
		label.AddThemeColorOverride("font_color", Glyph);
		AddChild(label);
		label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public override void _Draw()
	{
		Vector2 center = Size * 0.5f;
		float radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 1f;
		DrawCircle(center, radius, Fill);
		DrawCircle(center, radius, Outline, false, 2f);
	}
}
