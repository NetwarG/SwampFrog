using Godot;

namespace SwampFrog;

/// <summary>
/// Плитка достижения в сетке профиля: непрозрачная «стикер»-подложка, иконка и
/// короткая подпись. Открытое достижение — светлое с золотым акцентом, закрытое —
/// серое с приглушённой иконкой и прогрессом «текущий/цель».
/// </summary>
public partial class AchievementTile : Control
{
	public const float TileWidth = 108f;
	public const float TileHeight = 148f;
	private const float IconSize = 72f;

	/// <summary>Боковой отступ области подписи внутри плитки.</summary>
	public const float CaptionInsetX = 4f;

	/// <summary>Верхняя граница области подписи: сразу под иконкой.</summary>
	public const float CaptionTop = IconSize + 10f;

	/// <summary>Отступ подписи от нижней кромки плитки.</summary>
	public const float CaptionBottomMargin = 4f;

	/// <summary>Размер шрифта подписи: три строки закрытого достижения должны вписываться в плитку.</summary>
	private const int CaptionFontSize = 13;

	/// <summary>Межстрочный интервал подписи: меньше темы, чтобы три строки не выходили за плитку.</summary>
	private const int CaptionLineSpacing = 1;

	/// <summary>Приглушённый тон иконки закрытого достижения.</summary>
	private static readonly Color LockedIconTint = new(0.58f, 0.6f, 0.64f, 1f);

	/// <summary>Цвет подписи открытого достижения.</summary>
	private static readonly Color UnlockedTextColor = new("173c34");

	/// <summary>Цвет подписи закрытого достижения.</summary>
	private static readonly Color LockedTextColor = new("57584f");

	private AchievementEntry? _entry;
	private StickerBackground? _background;
	private TextureRect? _icon;
	private Label? _caption;

	/// <summary>Задаёт достижение плитки; вызывается до добавления в дерево.</summary>
	public void Setup(AchievementEntry entry)
	{
		_entry = entry;
		if (_caption != null)
			Refresh();
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(TileWidth, TileHeight);
		Size = new Vector2(TileWidth, TileHeight);

		_background = new StickerBackground { CornerRadius = 16f };
		AddChild(_background);

		_icon = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			Position = new Vector2((TileWidth - IconSize) * 0.5f, 8f),
			Size = new Vector2(IconSize, IconSize)
		};
		AddChild(_icon);

		_caption = UiStyle.MakeLabel(string.Empty, CaptionFontSize, UnlockedTextColor);
		_caption.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_caption.VerticalAlignment = VerticalAlignment.Top;
		_caption.AddThemeConstantOverride("line_spacing", CaptionLineSpacing);
		_caption.ClipText = true;
		_caption.Position = new Vector2(CaptionInsetX, CaptionTop);
		_caption.Size = new Vector2(TileWidth - CaptionInsetX * 2f, TileHeight - CaptionTop - CaptionBottomMargin);
		AddChild(_caption);
		Refresh();
	}

	/// <summary>Вписывается ли подпись (вместе с прогрессом) в границы плитки.</summary>
	public bool CaptionFits() => CaptionTop + CaptionRequiredHeight() <= Size.Y;

	/// <summary>Требуемая высота подписи с учётом переносов и межстрочного интервала.</summary>
	public float CaptionRequiredHeight()
	{
		if (_caption == null)
			return 0f;
		int lines = _caption.GetLineCount();
		if (lines <= 0)
			return 0f;
		float height = 0f;
		for (int i = 0; i < lines; i++)
			height += _caption.GetLineHeight(i);
		return height + _caption.GetThemeConstant("line_spacing") * (lines - 1);
	}

	/// <summary>Перечитывает иконку, подпись, прогресс и яркость (открыто/закрыто).</summary>
	public void Refresh()
	{
		if (_entry == null || _icon == null || _caption == null)
			return;
		AchievementDefinition def = AchievementCatalog.Get(_entry.Id);
		_icon.Texture = GD.Load<Texture2D>(_entry.IconPath);
		_icon.Modulate = _entry.Unlocked ? Colors.White : LockedIconTint;
		_caption.Text = _entry.Unlocked
			? def.Caption
			: $"{def.Caption}\n{_entry.Progress}/{_entry.Target}";
		_caption.AddThemeColorOverride("font_color", _entry.Unlocked ? UnlockedTextColor : LockedTextColor);
		if (_background != null)
		{
			Color accent = _entry.Unlocked ? new Color("ffd45e") : new Color("9aa0a8");
			_background.LightColor = StickerStyle.Light(accent);
			_background.DarkColor = StickerStyle.Dark(accent);
			_background.QueueRedraw();
		}
	}
}
