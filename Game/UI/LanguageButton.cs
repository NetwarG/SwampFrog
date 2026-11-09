using Godot;

namespace SwampFrog;

/// <summary>
/// Квадратная кнопка-стикер с кодом языка («RU»/«EN») в левом верхнем углу меню.
/// Тап переключает язык интерфейса (см. <see cref="Loc"/>): подписи всех экранов
/// обновляются по событию <see cref="Loc.Changed"/>.
/// </summary>
public partial class LanguageButton : TouchArea
{
	/// <summary>Размер кнопки в логических единицах.</summary>
	public static readonly Vector2 ButtonSize = new(64f, 64f);

	private const float CornerRadius = 16f;

	/// <summary>Цвет кнопки: золотистый, как кнопка мини-игры в меню.</summary>
	private static readonly Color Accent = new("ffd45e");

	/// <summary>Цвет надписи: тёмный, как контур иконок.</summary>
	private static readonly Color Glyph = new("2b170f");

	private Label? _label;

	public override void _Ready()
	{
		CustomMinimumSize = ButtonSize;
		Size = ButtonSize;

		_label = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_label.AddThemeFontSizeOverride("font_size", 26);
		_label.AddThemeColorOverride("font_color", Glyph);
		AddChild(_label);
		_label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		Pressed += Loc.Toggle;
		Loc.Changed += Refresh;
		Refresh();
	}

	public override void _ExitTree() => Loc.Changed -= Refresh;

	/// <summary>Показывает код текущего языка: «RU» или «EN».</summary>
	private void Refresh()
	{
		if (_label != null)
			_label.Text = Loc.IsEnglish ? "EN" : "RU";
	}

	public override void _Draw()
	{
		StickerStyle.DrawPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius,
			StickerStyle.Light(Accent), StickerStyle.Dark(Accent), StickerStyle.OutlineWidth, true);
	}
}
