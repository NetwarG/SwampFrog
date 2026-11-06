using Godot;

namespace SwampFrog;

/// <summary>Общие стили интерфейса: подписи и кнопки меню, паузы и результата.</summary>
public static class UiStyle
{
	/// <summary>Ширина и высота основной кнопки в логических единицах.</summary>
	public const float ButtonWidth = 330f;
	public const float ButtonHeight = 80f;

	/// <summary>Радиус скругления кнопок, карточек перков и заливки карточек.</summary>
	public const float CornerRadius = 14f;

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
			CornerRadiusTopLeft = (int)Mathf.Round(CornerRadius * ui),
			CornerRadiusTopRight = (int)Mathf.Round(CornerRadius * ui),
			CornerRadiusBottomLeft = (int)Mathf.Round(CornerRadius * ui),
			CornerRadiusBottomRight = (int)Mathf.Round(CornerRadius * ui),
			ShadowColor = new Color(0f, 0f, 0f, shadowAlpha),
			ShadowSize = (int)Mathf.Round(5f * ui)
		};
		style.SetBorderWidthAll((int)Mathf.Round(2f * ui));
		style.ContentMarginLeft = 18f * ui;
		style.ContentMarginRight = 18f * ui;
		return style;
	}

	/// <summary>
	/// Рисует скруглённую панель с обводкой в заданном прямоугольнике.
	/// Используется кнопками и ползунком звука вместо готовых стилей.
	/// </summary>
	public static void DrawRoundedPanel(CanvasItem target, Rect2 rect, float radius, Color fill, Color outline, float width)
	{
		float x = rect.Position.X;
		float y = rect.Position.Y;
		float w = rect.Size.X;
		float h = rect.Size.Y;

		target.DrawRect(new Rect2(x + radius, y, w - radius * 2f, h), fill);
		target.DrawRect(new Rect2(x, y + radius, w, h - radius * 2f), fill);
		target.DrawCircle(new Vector2(x + radius, y + radius), radius, fill);
		target.DrawCircle(new Vector2(x + w - radius, y + radius), radius, fill);
		target.DrawCircle(new Vector2(x + radius, y + h - radius), radius, fill);
		target.DrawCircle(new Vector2(x + w - radius, y + h - radius), radius, fill);

		if (width <= 0f || outline.A <= 0f)
			return;

		float inner = Mathf.Max(1f, radius - width * 0.5f);
		target.DrawLine(new Vector2(x + radius, y + width * 0.5f), new Vector2(x + w - radius, y + width * 0.5f), outline, width);
		target.DrawLine(new Vector2(x + radius, y + h - width * 0.5f), new Vector2(x + w - radius, y + h - width * 0.5f), outline, width);
		target.DrawLine(new Vector2(x + width * 0.5f, y + radius), new Vector2(x + width * 0.5f, y + h - radius), outline, width);
		target.DrawLine(new Vector2(x + w - width * 0.5f, y + radius), new Vector2(x + w - width * 0.5f, y + h - radius), outline, width);
		target.DrawArc(new Vector2(x + radius, y + radius), inner, Mathf.Pi, Mathf.Pi * 1.5f, 8, outline, width);
		target.DrawArc(new Vector2(x + w - radius, y + radius), inner, Mathf.Pi * 1.5f, Mathf.Pi * 2f, 8, outline, width);
		target.DrawArc(new Vector2(x + w - radius, y + h - radius), inner, 0f, Mathf.Pi * 0.5f, 8, outline, width);
		target.DrawArc(new Vector2(x + radius, y + h - radius), inner, Mathf.Pi * 0.5f, Mathf.Pi, 8, outline, width);
	}
}
