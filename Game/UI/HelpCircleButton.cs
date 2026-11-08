using Godot;

namespace SwampFrog;

/// <summary>
/// Круглая кнопка-стикер со знаком «?» в правом верхнем углу мини-игры: фон рисуется
/// в общем стиле игры, а знак ставится подписью по центру.
/// </summary>
public partial class HelpCircleButton : TouchArea
{
	/// <summary>Диаметр кнопки в логических единицах.</summary>
	public const float Diameter = 64f;

	/// <summary>Отступ кнопки от правого края поля.</summary>
	public const float MarginRight = 12f;

	private static readonly Color Accent = new("b4e863");

	/// <summary>Цвет знака «?»: тёмный, как контур иконок.</summary>
	private static readonly Color Glyph = new("2b170f");

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
		StickerStyle.DrawCirclePanel(this, center, radius,
			StickerStyle.Light(Accent), StickerStyle.Dark(Accent), StickerStyle.OutlineWidth, true);
	}
}
