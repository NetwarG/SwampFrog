using Godot;

namespace SwampFrog;

/// <summary>Общие стили интерфейса: подписи и кнопки меню, паузы и результата.</summary>
public static class UiStyle
{
	/// <summary>Ширина и высота основной кнопки в логических единицах.</summary>
	public const float ButtonWidth = 330f;
	public const float ButtonHeight = 80f;

	public static Label MakeLabel(string text, int fontSize, Color color)
	{
		var label = new Label
		{
			Text = text,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	/// <summary>Основная кнопка меню стандартного размера.</summary>
	public static Button MakeButton(string text, float ui, Color accent) =>
		MakeButton(text, ui, accent, new Vector2(ButtonWidth * ui, ButtonHeight * ui));

	public static Button MakeButton(string text, float ui, Color accent, Vector2 minSize)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = minSize,
			MouseFilter = Control.MouseFilterEnum.Pass,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		button.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(24f * ui));
		button.AddThemeColorOverride("font_color", new Color("173c34"));
		button.AddThemeColorOverride("font_hover_color", new Color("102c27"));
		button.AddThemeColorOverride("font_pressed_color", new Color("ffffff"));
		button.AddThemeColorOverride("font_focus_color", new Color("173c34"));
		button.AddThemeStyleboxOverride("normal", MakeButtonStyle(new Color(accent, 0.92f), accent, ui, 0.18f));
		button.AddThemeStyleboxOverride("hover", MakeButtonStyle(new Color(accent, 1f), new Color("ffffff"), ui, 0.32f));
		button.AddThemeStyleboxOverride("pressed", MakeButtonStyle(new Color(accent, 0.72f), new Color("ffffff"), ui, 0.10f));
		button.AddThemeStyleboxOverride("focus", MakeButtonStyle(new Color(accent, 0.92f), accent, ui, 0.18f));
		return button;
	}

	public static StyleBoxFlat MakeButtonStyle(Color background, Color border, float ui, float shadowAlpha)
	{
		var style = new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = new Color(border, 0.72f),
			CornerRadiusTopLeft = (int)Mathf.Round(14f * ui),
			CornerRadiusTopRight = (int)Mathf.Round(14f * ui),
			CornerRadiusBottomLeft = (int)Mathf.Round(14f * ui),
			CornerRadiusBottomRight = (int)Mathf.Round(14f * ui),
			ShadowColor = new Color(0f, 0f, 0f, shadowAlpha),
			ShadowSize = (int)Mathf.Round(5f * ui)
		};
		style.SetBorderWidthAll((int)Mathf.Round(2f * ui));
		style.ContentMarginLeft = 18f * ui;
		style.ContentMarginRight = 18f * ui;
		return style;
	}
}
