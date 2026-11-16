using Godot;

namespace SwampFrog;

/// <summary>
/// Квадратная кнопка-стикер с иконкой рекламы в левом верхнем углу меню — сразу под кнопкой
/// языка. Тап открывает ролик с наградой (см. docs/ADS.md).
/// </summary>
public partial class AdRewardButton : TouchArea
{
	/// <summary>Размер кнопки в логических единицах: как у кнопки языка.</summary>
	public static readonly Vector2 ButtonSize = new(64f, 64f);

	/// <summary>Доля стороны кнопки, которую занимает иконка.</summary>
	private const float IconRatio = 0.62f;

	private const float CornerRadius = 16f;
	private const string IconPath = "res://Game/assets/ui/icon_ad.png";

	/// <summary>Цвет кнопки: голубой, как у кнопки «Профиль».</summary>
	private static readonly Color Accent = new("7fd6f2");

	private TextureRect? _icon;
	private Label? _hint;

	public override void _Ready()
	{
		CustomMinimumSize = ButtonSize;
		Size = ButtonSize;
		_icon = new TextureRect
		{
			Name = "Icon",
			Texture = GD.Load<Texture2D>(IconPath),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_icon);
		LayoutIcon();
		Resized += LayoutIcon;
	}

	/// <summary>Подсказка справа от кнопки: плавно появляется и растворяется.</summary>
	public void ShowHint(string text)
	{
		if (_hint == null)
		{
			_hint = UiStyle.MakeLabel(string.Empty, 20, new Color("ffd45e"));
			_hint.Name = "Hint";
			_hint.HorizontalAlignment = HorizontalAlignment.Left;
			_hint.ZIndex = 60;
			_hint.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
			_hint.AddThemeConstantOverride("shadow_offset_x", 2);
			_hint.AddThemeConstantOverride("shadow_offset_y", 2);
			AddChild(_hint);
		}
		_hint.Text = text;
		_hint.ResetSize();
		_hint.Position = new Vector2(Size.X + 10f, (Size.Y - _hint.Size.Y) * 0.5f);
		_hint.Visible = true;
		_hint.Modulate = Colors.White;
		Tween tween = CreateTween();
		tween.TweenInterval(1.8f);
		tween.TweenProperty(_hint, "modulate:a", 0f, 0.5f);
		tween.Chain().TweenCallback(Callable.From(() => _hint!.Visible = false));
	}

	private void LayoutIcon()
	{
		if (_icon == null)
			return;
		float size = Mathf.Min(Size.X, Size.Y) * IconRatio;
		_icon.Position = (Size - Vector2.One * size) * 0.5f;
		_icon.Size = Vector2.One * size;
	}

	public override void _Draw()
	{
		StickerStyle.DrawPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius,
			StickerStyle.Light(Accent), StickerStyle.Dark(Accent), StickerStyle.OutlineWidth, true);
	}
}