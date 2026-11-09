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

	/// <summary>Кнопка-стикер в общем стиле игры: градиент, коричневый контур и блик.</summary>
	public static Button MakeButton(string text, float ui, Color accent, Vector2 minSize)
	{
		var button = new StickerButton
		{
			Text = text,
			CustomMinimumSize = minSize,
			MouseFilter = Control.MouseFilterEnum.Pass,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			CornerRadius = CornerRadius * ui,
			OutlineWidth = StickerStyle.OutlineWidth * ui,
			MarginX = 18f * ui
		};
		button.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(24f * ui));
		button.AddThemeColorOverride("font_color", new Color("173c34"));
		button.AddThemeColorOverride("font_hover_color", new Color("102c27"));
		button.AddThemeColorOverride("font_pressed_color", new Color("ffffff"));
		button.AddThemeColorOverride("font_focus_color", new Color("173c34"));
		button.SetAccent(accent);
		return button;
	}
}
