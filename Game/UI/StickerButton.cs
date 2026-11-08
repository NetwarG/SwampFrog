using Godot;

namespace SwampFrog;

/// <summary>
/// Кнопка в стиле игры: фон-«стикер» рисует отдельный слой под родителем
/// (см. <see cref="StickerBackground"/>), поэтому надпись и иконка остаются
/// поверх градиентной заливки, коричневого контура и блика.
/// </summary>
public partial class StickerButton : Button
{
	private StickerBackground? _background;

	/// <summary>Цвет кнопки: из него строятся оба тона градиента.</summary>
	public Color Accent { get; private set; } = new("b4e863");

	/// <summary>Фон кнопки: для проверок и внешнего чтения цвета.</summary>
	public Color BackgroundColor => Accent;

	/// <summary>Радиус скругления кнопки.</summary>
	public float CornerRadius { get; set; } = UiStyle.CornerRadius;

	/// <summary>Толщина коричневого контура.</summary>
	public float OutlineWidth { get; set; } = StickerStyle.OutlineWidth;

	/// <summary>Рисовать ли глянцевый блик.</summary>
	public bool Gloss { get; set; } = true;

	/// <summary>Боковой отступ надписи и иконки внутри кнопки.</summary>
	public float MarginX { get; set; } = 18f;

	/// <summary>Вертикальный отступ надписи и иконки внутри кнопки.</summary>
	public float MarginY { get; set; }

	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		ApplyStyleBoxes();

		_background = new StickerBackground { Name = "StickerBackground" };
		AddChild(_background);
		_background.ShowBehindParent = true;
		ApplyColors();

		ButtonDown += () => ApplyPressed(true);
		ButtonUp += () => ApplyPressed(false);
		MouseEntered += () => SetHovered(true);
		MouseExited += () => SetHovered(false);
	}

	/// <summary>Задаёт цвет кнопки: можно вызывать и до добавления в дерево.</summary>
	public void SetAccent(Color accent)
	{
		Accent = accent;
		ApplyColors();
	}

	/// <summary>Задаёт отступы надписи: нужны карточкам с иконкой слева.</summary>
	public void SetContentMargin(float marginX, float marginY)
	{
		MarginX = marginX;
		MarginY = marginY;
		ApplyStyleBoxes();
	}

	private void ApplyColors()
	{
		if (_background == null)
			return;
		_background.LightColor = StickerStyle.Light(Accent);
		_background.DarkColor = StickerStyle.Dark(Accent);
		_background.CornerRadius = CornerRadius;
		_background.OutlineWidth = OutlineWidth;
		_background.Gloss = Gloss;
		_background.QueueRedraw();
	}

	private void ApplyPressed(bool pressed) => _background?.SetPressed(pressed);

	private void SetHovered(bool hovered) => _background?.SetHovered(hovered);

	/// <summary>Стиль прозрачный: фон рисует StickerBackground, от стиля нужны только отступы.</summary>
	private void ApplyStyleBoxes()
	{
		foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled", "hover_pressed" })
			AddThemeStyleboxOverride(state, StickerStyle.MakeContentStyle(MarginX, MarginY));
	}
}
