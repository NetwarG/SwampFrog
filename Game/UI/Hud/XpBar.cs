using Godot;

namespace SwampFrog;

/// <summary>
/// Шкала опыта внизу экрана: тонкая капсула с прогрессом и мелкой подписью «LV n».
/// Сделана ненавязчивой: полупрозрачный тёмный фон, приглушённые цвета, малый размер.
/// </summary>
public partial class XpBar : Control
{
	private const float Width = 210f;
	private const float Height = 10f;

	private static readonly Color FillColor = new("5fd24a");
	private static readonly Color FillLevelColor = new("ffd32e");
	private static readonly Color TrackColor = new(1f, 1f, 1f, 0.14f);
	private static readonly Color TrackOutline = new(1f, 1f, 1f, 0.10f);

	private int _level = 1;
	private float _progress;
	private float _uiScale = 1f;
	private float _pulseT;
	private Label? _levelLabel;
	private Label? _levelUpLabel;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		_levelLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_levelLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.55f));
		_levelLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.5f));
		AddChild(_levelLabel);

		_levelUpLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_levelUpLabel.AddThemeColorOverride("font_color", new Color("ffd32e"));
		_levelUpLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		AddChild(_levelUpLabel);

		UpdateLayout();
		UpdateLabels();
	}

	public void SetLevel(int level)
	{
		_level = Mathf.Max(1, level);
		UpdateLabels();
	}

	public void SetProgress(float progress)
	{
		_progress = Mathf.Clamp(progress, 0f, 1f);
		QueueRedraw();
	}

	public void SetUiScale(float uiScale)
	{
		_uiScale = uiScale;
		UpdateLayout();
		UpdateLabels();
		QueueRedraw();
	}

	/// <summary>Короткий золотой «всплеск» шкалы при новом уровне.</summary>
	public void PulseLevelUp()
	{
		_pulseT = 1f;
		QueueRedraw();
	}

	/// <summary>Ненавязчивая надпись «Уровень N!» над шкалой, быстро исчезает.</summary>
	public void ShowLevelUpLabel(int level)
	{
		if (_levelUpLabel == null)
		{
			return;
		}
		_levelUpLabel.Text = Loc.T(L.LevelUp, level);
		_levelUpLabel.Visible = true;

		Tween tween = CreateTween();
		tween.TweenInterval(1.1f);
		tween.TweenProperty(_levelUpLabel, "modulate:a", 0f, 0.7f);
		tween.Chain().TweenCallback(Callable.From(() => _levelUpLabel.Visible = false));
		_levelUpLabel.Modulate = new Color(1f, 1f, 1f, 1f);
	}

	private void UpdateLayout()
	{
		AnchorLeft = 0.5f;
		AnchorRight = 0.5f;
		AnchorTop = 1f;
		AnchorBottom = 1f;

		OffsetLeft = -Width * 0.5f * _uiScale + Width * 0.5f * (1f - _uiScale);
		OffsetRight = Width * 0.5f * _uiScale + Width * 0.5f * (1f - _uiScale);
		OffsetTop = -Height * _uiScale - 10f * _uiScale;
		OffsetBottom = -10f * _uiScale;

		Size = new Vector2(Width * _uiScale, Height * _uiScale);
	}

	private void UpdateLabels()
	{
		if (_levelLabel == null)
		{
			return;
		}

		int size = (int)Mathf.Round(14f * _uiScale);
		_levelLabel.AddThemeFontSizeOverride("font_size", size);
		_levelLabel.Text = Loc.T(L.LevelShort, _level);

		// Подпись по центру над полосой.
		float w = Width * _uiScale;
		float h = Height * _uiScale;
		_levelLabel.Position = new Vector2((Size.X - w) * 0.5f, -h - 2f * _uiScale - size * 0.4f);
		_levelLabel.Size = new Vector2(w, size + 4f);

		if (_levelUpLabel != null)
		{
			_levelUpLabel.AddThemeFontSizeOverride("font_size", size + 6);
			_levelUpLabel.Position = new Vector2((Size.X - w) * 0.5f, -h - 2f * _uiScale - (size + 6) * 1.2f);
			_levelUpLabel.Size = new Vector2(w, size + 10);
		}
	}

	public override void _Process(double delta)
	{
		if (_pulseT > 0f)
		{
			_pulseT = Mathf.Max(0f, _pulseT - (float)delta * 2.5f);
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		float w = Width * _uiScale;
		float h = Height * _uiScale;
		// Позиционирование: полоса по центру, чуть выше низа.
		float y = Size.Y - h;

		// Тёмная подложка-капсула.
		DrawCircle(new Vector2(h * 0.5f, y + h * 0.5f), h * 0.5f, TrackColor);
		DrawCircle(new Vector2(w - h * 0.5f, y + h * 0.5f), h * 0.5f, TrackColor);
		DrawRect(new Rect2(h * 0.5f, y, w - h, h), TrackColor);

		// Заполнение прогресса.
		float fillW = Mathf.Max(0f, (w - h) * Mathf.Clamp(_progress, 0f, 1f));
		if (fillW > 0f)
		{
			bool levelUp = _pulseT > 0f;
			Color fill = levelUp ? FillLevelColor : FillColor;
			// Плавное появление после уровня.
			float alpha = levelUp ? 0.85f : 0.9f;
			DrawCircle(new Vector2(h * 0.5f, y + h * 0.5f), h * 0.5f, new Color(fill, alpha));
			DrawRect(new Rect2(h * 0.5f, y, fillW, h), new Color(fill, alpha));
			DrawCircle(new Vector2(h * 0.5f + fillW, y + h * 0.5f), h * 0.5f, new Color(fill, alpha));
		}

		// Обводка.
		DrawCircle(new Vector2(h * 0.5f, y + h * 0.5f), h * 0.5f, TrackOutline, false, 1f);
		DrawRect(new Rect2(h * 0.5f, y, w - h, h), TrackOutline, false, 1f);
		DrawCircle(new Vector2(w - h * 0.5f, y + h * 0.5f), h * 0.5f, TrackOutline, false, 1f);
	}
}
