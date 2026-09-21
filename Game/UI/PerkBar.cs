using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Панель выбранных перков внизу экрана: по одному маленькому квадратику на перк.
/// Квадратик окрашен в акцентный цвет перка из <see cref="PerkCatalog"/>, в центре —
/// текущий уровень улучшения. Панель центрируется над шкалой опыта и скрывается,
/// когда выбранных перков нет или игровой HUD неактивен.
/// </summary>
public partial class PerkBar : Control
{
	private const float IconSize = 38f;
	private const float IconGap = 8f;
	private const float BottomPadding = 46f;

	private readonly List<PerkIcon> _icons = new();
	private PerkIconInfo[] _perks = { };
	private float _uiScale = 1f;

	/// <summary>Есть ли хотя бы один отображаемый перк.</summary>
	public bool HasIcons => _icons.Count > 0;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.CenterBottom);
		AnchorTop = 1f;
		AnchorBottom = 1f;
		Visible = false;
	}

	/// <summary>Обновляет набор отображаемых перков.</summary>
	public void SetPerks(PerkIconInfo[] perks)
	{
		_perks = perks ?? new PerkIconInfo[0];
		foreach (PerkIcon icon in _icons)
		{
			icon.QueueFree();
		}
		_icons.Clear();

		foreach (PerkIconInfo info in _perks)
		{
			PerkDefinition def = PerkCatalog.Find(info.Id);
			var icon = new PerkIcon(info.Level, def.Accent, def.MaxLevel, _uiScale);
			_icons.Add(icon);
			AddChild(icon);
		}
		UpdateLayout();
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

		Vector2 vp = GetViewport().GetVisibleRect().Size;
		Position = new Vector2(vp.X * 0.5f - totalWidth * 0.5f, vp.Y - BottomPadding * _uiScale - s);
	}
}

/// <summary>
/// Квадратик перка в панели <see cref="PerkBar"/>: заливка акцентным цветом,
/// тонкая обводка и номер уровня по центру. На максимальном уровне обводка золотая.
/// </summary>
public partial class PerkIcon : Control
{
	private static readonly Color Outline = new Color(1f, 1f, 1f, 0.35f);
	private static readonly Color MaxOutline = new Color("ffd32e");

	private readonly int _level;
	private readonly int _maxLevel;
	private float _uiScale;
	private Label? _levelLabel;
	private Color _accent;

	public PerkIcon(int level, Color accent, int maxLevel, float uiScale)
	{
		_level = level;
		_accent = accent;
		_maxLevel = maxLevel;
		_uiScale = uiScale;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Ready()
	{
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
		_levelLabel.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_levelLabel);
		QueueRedraw();
	}

	/// <summary>Обновляет масштаб квадратика и его подписи.</summary>
	public void SetUiScale(float uiScale)
	{
		_uiScale = uiScale;
		if (_levelLabel != null)
		{
			_levelLabel.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(16f * uiScale));
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		float s = Size.X;

		// Скруглённый квадрат: заливка акцентом.
		DrawRect(new Rect2(1f, 1f, s - 2f, s - 2f), new Color(_accent, 0.88f));

		// Обводка, на максимальном уровне — золотая.
		DrawRect(new Rect2(0.5f, 0.5f, s - 1f, s - 1f), _level >= _maxLevel ? MaxOutline : Outline, false, Mathf.Max(1f, 1.5f * _uiScale));
	}
}