using Godot;

namespace SwampFrog;

/// <summary>
/// Кнопка-стикер меню с иконкой-картинкой (PNG из Game/assets/ui). Подпись необязательна:
/// если она задана, иконка поднимается, а текст встаёт под ней (например, «Up»).
/// Фон и стиль — общие, как у остальных кнопок (см. <see cref="StickerButton"/>).
/// </summary>
public partial class IconStickerButton : StickerButton
{
	/// <summary>Доля стороны кнопки, которую занимает иконка.</summary>
	private const float IconRatio = 0.62f;

	private TextureRect? _icon;
	private Label? _caption;

	/// <summary>Ставит иконку кнопки: PNG по пути из Game/assets/ui.</summary>
	public void SetIcon(string path)
	{
		Texture2D? texture = GD.Load<Texture2D>(path);
		EnsureNodes();
		if (_icon != null)
			_icon.Texture = texture;
		LayoutContent();
	}

	/// <summary>Подпись под иконкой; пустая строка убирает её.</summary>
	public void SetCaption(string caption)
	{
		EnsureNodes();
		if (_caption == null)
			return;
		_caption.Text = caption;
		_caption.Visible = !string.IsNullOrEmpty(caption);
		LayoutContent();
	}

	public override void _Ready()
	{
		base._Ready();
		Text = string.Empty;
		EnsureNodes();
		Resized += LayoutContent;
		LayoutContent();
	}

	private void EnsureNodes()
	{
		if (_icon == null)
		{
			_icon = new TextureRect
			{
				Name = "Icon",
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = MouseFilterEnum.Ignore
			};
			AddChild(_icon);
		}
		if (_caption == null)
		{
			_caption = UiStyle.MakeLabel(string.Empty, 16, new Color("173c34"));
			_caption.Name = "Caption";
			_caption.Visible = false;
			AddChild(_caption);
		}
	}

	/// <summary>Иконка по центру; с подписью — иконка выше, текст под ней.</summary>
	private void LayoutContent()
	{
		if (_icon == null || _caption == null)
			return;
		float size = Mathf.Min(Size.X, Size.Y);
		float iconSize = size * IconRatio;
		bool captioned = _caption.Visible && !string.IsNullOrEmpty(_caption.Text);
		float iconTop = captioned ? (Size.Y - iconSize) * 0.30f : (Size.Y - iconSize) * 0.5f;
		_icon.Position = new Vector2((Size.X - iconSize) * 0.5f, iconTop);
		_icon.Size = new Vector2(iconSize, iconSize);
		_caption.Position = new Vector2(0f, Size.Y - 26f);
		_caption.Size = new Vector2(Size.X, 20f);
	}
}
