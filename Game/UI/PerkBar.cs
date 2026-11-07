using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Панель слотов перков внизу экрана: по одному маленькому квадратику на перк забега.
/// Квадратик окрашен в акцентный цвет перка из <see cref="PerkCatalog"/>, содержит его иконку
/// и уровень в правом нижнем углу; на максимальном уровне обводка становится золотой.
/// Пустые слоты показаны приглушённой рамкой. Панель центрируется над шкалой
/// опыта и скрывается, когда игровой HUD неактивен.
/// </summary>
public partial class PerkBar : Control
{
	private const float IconSize = 38f;
	private const float IconGap = 8f;
	private const float BottomPadding = 46f;

	private readonly List<PerkIcon> _icons = new();
	private PerkIconInfo[] _perks = { };
	private int _maxSlots = PerkState.MaxDistinctPerks;
	private float _uiScale = 1f;

	/// <summary>Есть ли хотя бы один отображаемый слот.</summary>
	public bool HasIcons => _icons.Count > 0;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.CenterBottom);
		AnchorTop = 1f;
		AnchorBottom = 1f;
		Visible = false;
	}

	/// <summary>Обновляет набор отображаемых перков и число слотов забега.</summary>
	public void SetPerks(PerkIconInfo[] perks, int maxSlots)
	{
		_perks = perks ?? new PerkIconInfo[0];
		_maxSlots = Mathf.Max(_perks.Length, maxSlots);
		foreach (PerkIcon icon in _icons)
		{
			icon.QueueFree();
		}
		_icons.Clear();

		foreach (PerkIconInfo info in _perks)
		{
			PerkDefinition def = PerkCatalog.Find(info.Id);
			_icons.Add(AddIcon(new PerkIcon(info.Id, info.Level, def.Accent, def.MaxLevel, _uiScale)));
		}
		// Свободные слоты показывают, сколько перков ещё можно взять за забег.
		for (int i = _perks.Length; i < _maxSlots; i++)
		{
			_icons.Add(AddIcon(PerkIcon.Empty(_uiScale)));
		}
		UpdateLayout();
	}

	private PerkIcon AddIcon(PerkIcon icon)
	{
		AddChild(icon);
		return icon;
	}

	/// <summary>Обновляет масштаб квадратиков (вызывается при ресайзе).</summary>
	public void SetUiScale(float uiScale)
	{
		_uiScale = uiScale;
		foreach (PerkIcon icon in _icons)
		{
			icon.SetUiScale(uiScale);
		}
		UpdateLayout();
	}

	/// <summary>Пересчитывает размер панели и раскладку квадратиков по центру снизу.</summary>
	private void UpdateLayout()
	{
		if (_icons.Count == 0)
		{
			Visible = false;
			return;
		}
		Visible = true;

		float s = IconSize * _uiScale;
		float gap = IconGap * _uiScale;
		float totalWidth = _icons.Count * s + (_icons.Count - 1) * gap;
		Size = new Vector2(totalWidth, s);

		for (int i = 0; i < _icons.Count; i++)
		{
			_icons[i].Position = new Vector2(i * (s + gap), 0f);
			_icons[i].Size = new Vector2(s, s);
		}

		Vector2 vp = GetParentAreaSize();
		Position = new Vector2(vp.X * 0.5f - totalWidth * 0.5f, vp.Y - BottomPadding * _uiScale - s);
	}
}

/// <summary>
/// Квадратик слота в панели <see cref="PerkBar"/>: заливка акцентным цветом,
/// иконка перка и номер уровня в правом нижнем углу. На максимальном уровне обводка золотая,
/// у пустого слота — только приглушённая рамка без номера.
/// </summary>
public partial class PerkIcon : Control
{
	private static readonly Color Outline = new Color(1f, 1f, 1f, 0.35f);
	private static readonly Color EmptyOutline = new Color(1f, 1f, 1f, 0.18f);
	private static readonly Color EmptyFill = new Color(1f, 1f, 1f, 0.10f);
	private static readonly Color MaxOutline = new Color("ffd32e");

	private readonly int _level;
	private readonly int _maxLevel;
	private readonly Texture2D? _texture;
	private TextureRect? _iconView;
	private readonly bool _empty;
	private float _uiScale;
	private Label? _levelLabel;
	private Color _accent;

	public PerkIcon(PerkId id, int level, Color accent, int maxLevel, float uiScale, bool empty = false)
	{
		_texture = empty ? null : PerkCatalog.Icon(id);
		_level = level;
		_accent = accent;
		_maxLevel = maxLevel;
		_uiScale = uiScale;
		_empty = empty;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	/// <summary>Пустой слот забега: перк ещё не выбран.</summary>
	public bool IsEmpty => _empty;
	public bool HasIcon => _iconView?.Texture != null;
	public static PerkIcon Empty(float uiScale)
	{
		return new PerkIcon(PerkId.StickyPaws, 0, EmptyFill, 0, uiScale, true);
	}

	public override void _Ready()
	{
		if (!_empty)
		{
			_iconView = new TextureRect
			{
				Name = "Icon",
				Texture = _texture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = MouseFilterEnum.Ignore
			};
			AddChild(_iconView);

			_levelLabel = new Label
			{
				Text = _level.ToString(),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore
			};
			_levelLabel.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(16f * _uiScale));
			_levelLabel.AddThemeColorOverride("font_color", new Color("ffffff"));
			_levelLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
			_levelLabel.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(1f * _uiScale));
			_levelLabel.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(1f * _uiScale));
			_levelLabel.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.12f, 0.09f, 0.95f));
			_levelLabel.AddThemeConstantOverride("outline_size", (int)Mathf.Max(1f, Mathf.Round(3f * _uiScale)));
			AddChild(_levelLabel);
		}
		QueueRedraw();
		Resized += UpdateContentLayout;
		UpdateContentLayout();
	}

	/// <summary>Обновляет масштаб квадратика и его подписи.</summary>
	public void SetUiScale(float uiScale)
	{
		_uiScale = uiScale;
		if (_levelLabel != null)
		{
			_levelLabel.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(16f * uiScale));
			_levelLabel.AddThemeConstantOverride("outline_size", (int)Mathf.Max(1f, Mathf.Round(3f * uiScale)));
		}
		QueueRedraw();
		UpdateContentLayout();
	}

	private void UpdateContentLayout()
	{
		float side = Mathf.Min(Size.X, Size.Y);
		if (side <= 0f)
			return;
		float inset = Mathf.Max(2f, 3f * _uiScale);
		if (_iconView != null)
		{
			_iconView.Position = Vector2.One * inset;
			_iconView.Size = Vector2.One * Mathf.Max(0f, side - inset * 2f);
		}
		if (_levelLabel != null)
		{
			Vector2 badgeSize = new(side * 0.50f, side * 0.46f);
			_levelLabel.Position = new Vector2(side - badgeSize.X - 1f, side - badgeSize.Y);
			_levelLabel.Size = badgeSize;
		}
	}

	public override void _Draw()
	{
		float s = Size.X;

		// Квадратный слот: заливка акцентом, у пустого слота — едва заметная.
		DrawRect(new Rect2(1f, 1f, s - 2f, s - 2f), new Color(_accent, _empty ? 0.10f : 0.48f));

		// Обводка, на максимальном уровне — золотая.
		Color outline = _empty ? EmptyOutline : _level >= _maxLevel ? MaxOutline : Outline;
		DrawRect(new Rect2(0.5f, 0.5f, s - 1f, s - 1f), outline, false, Mathf.Max(1f, 1.5f * _uiScale));
	}
}