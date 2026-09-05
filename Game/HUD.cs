using Godot;

namespace SwampFrog;

/// <summary>
/// HUD поверх игрового мира: счёт, сердечки жизней, красная вспышка от мусора,
/// оверлей «Игра окончена» и всплывающие надписи «+10».
/// </summary>
public partial class HUD : CanvasLayer
{
	private const float HintDuration = 3.4f;

	private Label? _scoreLabel;
	private HeartsIndicator? _hearts;
	private FlashOverlay? _flash;
	private Control? _gameOverRoot;
	private Control? _startRoot;
	private Label? _finalScore;
	private Label? _finalHigh;

	/// <summary>
	/// Адаптивный масштаб HUD: кламп min(видимая/540, видимая/960) так же, как в Main.
	/// Нужен, чтобы элементы не становились крошечными на больших экранах.
	/// </summary>
	public float UiScale
	{
		get
		{
			Vector2 vp = GetViewport().GetVisibleRect().Size;
			if (vp.X <= 0f || vp.Y <= 0f)
			{
				return 1f;
			}
			return Mathf.Clamp(Mathf.Min(vp.X / 540f, vp.Y / 960f), 0.85f, 2f);
		}
	}

	public override void _Ready()
	{
		BuildUI();
		GetViewport().SizeChanged += OnViewportResized;
	}

	private void OnViewportResized()
	{
		RecenterOverlays();

		// Обновляем адаптивные размеры HUD-элементов.
		float ui = UiScale;
		if (_scoreLabel != null)
		{
			_scoreLabel.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(44f * ui));
		}
		_hearts?.SetUiScale(ui);
	}

	/// <summary>Перецентрирует все видимые оверлеи после изменения размера окна.</summary>
	public void RecenterOverlays()
	{
		RecenterBoxIfVisible(_gameOverRoot);
		RecenterBoxIfVisible(_startRoot);
	}

	// Переименованный хелпер: центрирует контейнер внутри родителя, если тот видим.
	private void RecenterBoxIfVisible(Control? root)
	{
		if (root == null || !root.Visible)
		{
			return;
		}
		RecenterBox(root);
	}

	private void RecenterBox(Control parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is VBoxContainer box)
			{
				Vector2 vp = GetViewport().GetVisibleRect().Size;
				box.ResetSize();
				box.Position = new Vector2(vp.X * 0.5f - box.Size.X * 0.5f, vp.Y * 0.5f - box.Size.Y * 0.5f);
			}
		}
	}

	private void BuildUI()
	{
		float ui = UiScale;

		_scoreLabel = new Label();
		_scoreLabel.Text = "0";
		_scoreLabel.Position = new Vector2(18f, 12f);
		_scoreLabel.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(44f * ui));
		_scoreLabel.AddThemeColorOverride("font_color", new Color("ffffff"));
		_scoreLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
		_scoreLabel.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(3f * ui));
		_scoreLabel.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(3f * ui));
		AddChild(_scoreLabel);

		_hearts = new HeartsIndicator();
		_hearts.SetUiScale(ui);
		AddChild(_hearts);

		_flash = new FlashOverlay();
		AddChild(_flash);

		BuildGameOver();
		BuildStart();
	}

	private void BuildStart()
	{
		_startRoot = new Control();
		_startRoot.Visible = true;
		_startRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_startRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_startRoot);

		var backdrop = new ColorRect();
		backdrop.Color = new Color(0.02f, 0.12f, 0.09f, 0.55f);
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		backdrop.MouseFilter = Control.MouseFilterEnum.Ignore;
		_startRoot.AddChild(backdrop);

		var box = new VBoxContainer();
		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddThemeConstantOverride("separation", 20);
		_startRoot.AddChild(box);

		float ui = UiScale;

		Label title = MakeLabel("Лягушка-охотница", (int)Mathf.Round(50f * ui), new Color("ffe066"));
		Label sub = MakeLabel("Собирай фрукты, не лови мусор!", (int)Mathf.Round(24f * ui), new Color("ffffff"));
		Label sub2 = MakeLabel("Упустишь фрукт — потеряешь жизнь.", (int)Mathf.Round(20f * ui), new Color(1f, 1f, 1f, 0.8f));
		Label tap = MakeLabel("Нажми, чтобы начать", (int)Mathf.Round(27f * ui), new Color("b4e863"));

		box.AddChild(title);
		box.AddChild(sub);
		box.AddChild(sub2);
		box.AddChild(tap);

		RecenterBox(_startRoot);
	}

	/// <summary>Показывает стартовый экран «нажми, чтобы начать».</summary>
	public void ShowStart()
	{
		if (_startRoot != null)
		{
			_startRoot.Visible = true;
		}
	}

	/// <summary>Скрывает стартовый экран в начале игры.</summary>
	public void HideStart()
	{
		if (_startRoot != null)
		{
			_startRoot.Visible = false;
		}
	}

	private void BuildGameOver()
	{
		_gameOverRoot = new Control();
		_gameOverRoot.Visible = false;
		_gameOverRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_gameOverRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_gameOverRoot);

		var backdrop = new ColorRect();
		backdrop.Color = new Color(0.02f, 0.12f, 0.09f, 0.62f);
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		backdrop.MouseFilter = Control.MouseFilterEnum.Ignore;
		_gameOverRoot.AddChild(backdrop);

		var box = new VBoxContainer();
		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddThemeConstantOverride("separation", 16);
		_gameOverRoot.AddChild(box);

		float ui = UiScale;

		Label title = MakeLabel("Игра окончена!", (int)Mathf.Round(48f * ui), new Color("ffd45e"));
		_finalScore = MakeLabel("Счёт: 0", (int)Mathf.Round(32f * ui), new Color("ffffff"));
		_finalHigh = MakeLabel("Рекорд: 0", (int)Mathf.Round(32f * ui), new Color("cfe9c2"));
		Label hint = MakeLabel("Нажми, чтобы начать заново", (int)Mathf.Round(22f * ui), new Color(1f, 1f, 1f, 0.85f));

		box.AddChild(title);
		box.AddChild(_finalScore);
		box.AddChild(_finalHigh);
		box.AddChild(hint);

		RecenterBox(_gameOverRoot);
	}

	private static Label MakeLabel(string text, int fontSize, Color color)
	{
		var label = new Label();
		label.Text = text;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	/// <summary>Мимолётная подсказка «держи палец» после старта.</summary>
	public void ShowHint()
	{
		float ui = UiScale;
		var hint = new Label();
		hint.Text = "Держи палец на экране — руки растут";
		hint.AnchorLeft = 0.5f;
		hint.AnchorRight = 0.5f;
		hint.AnchorTop = 1f;
		hint.AnchorBottom = 1f;
		hint.OffsetLeft = -240f * ui;
		hint.OffsetRight = 240f * ui;
		hint.OffsetTop = -46f * ui;
		hint.OffsetBottom = -14f * ui;
		hint.HorizontalAlignment = HorizontalAlignment.Center;
		hint.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(18f * ui));
		hint.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.9f));
		hint.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		hint.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(2f * ui));
		hint.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(2f * ui));
		AddChild(hint);

		Tween tween = CreateTween();
		tween.TweenInterval(HintDuration);
		tween.TweenProperty(hint, "modulate:a", 0f, 0.7f);
		tween.TweenCallback(Callable.From(hint.QueueFree));
	}

	public void SetScore(int score)
	{
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = score.ToString();
		}
	}

	public void SetLives(int lives) => _hearts?.SetLives(lives);

	public void FlashRed() => _flash?.Start();

	public void ShowGameOver(int score, int highScore)
	{
		if (_finalScore != null)
		{
			_finalScore.Text = $"Счёт: {score}";
		}
		if (_finalHigh != null)
		{
			_finalHigh.Text = $"Рекорд: {highScore}";
		}
		if (_gameOverRoot != null)
		{
			_gameOverRoot.Visible = true;
		}
	}

	public void HideGameOver()
	{
		if (_gameOverRoot != null)
		{
			_gameOverRoot.Visible = false;
		}
	}

	/// <summary>Всплывающая надпись «+10» в точке поимки.</summary>
	public void SpawnPopup(Vector2 globalPos, string text)
	{
		float ui = UiScale;
		var popup = new Label();
		popup.Text = text;
		popup.GlobalPosition = globalPos;
		popup.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(30f * ui));
		popup.AddThemeColorOverride("font_color", new Color("fff45e"));
		popup.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
		popup.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(2f * ui));
		popup.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(2f * ui));
		popup.ZIndex = 50;
		AddChild(popup);

		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(popup, "position:y", popup.Position.Y - 80f, 0.9f);
		tween.TweenProperty(popup, "modulate:a", 0f, 0.9f);
		tween.Chain().TweenCallback(Callable.From(popup.QueueFree));
	}
}
/// <summary>Три сердечка жизней в правом верхнем углу.</summary>
public partial class HeartsIndicator : Control
{
	private static readonly Color FullHeart = new("f7454a");
	private static readonly Color EmptyHeart = new Color(1f, 1f, 1f, 0.22f);

	private int _lives = 3;
	private float _uiScale = 1f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		UpdateLayout();
	}

	public void SetLives(int lives)
	{
		_lives = Mathf.Clamp(lives, 0, 3);
		QueueRedraw();
	}

	/// <summary>Обновляет масштаб сердечек (вызывается при ресайзе).</summary>
	public void SetUiScale(float uiScale)
	{
		_uiScale = uiScale;
		UpdateLayout();
		QueueRedraw();
	}

	private void UpdateLayout()
	{
		AnchorLeft = 1f;
		AnchorRight = 1f;
		AnchorTop = 0f;
		AnchorBottom = 0f;
		OffsetLeft = -118f * _uiScale;
		OffsetRight = -14f * _uiScale;
		OffsetTop = 14f * _uiScale;
		OffsetBottom = 78f * _uiScale;
		Size = new Vector2(104f * _uiScale, 64f * _uiScale);
	}

	public override void _Draw()
	{
		const float sBase = 30f;
		float s = sBase * _uiScale;
		for (int i = 0; i < 3; i++)
		{
			Color color = i < _lives ? FullHeart : EmptyHeart;
			Vector2 c = new(12f + i * s * 0.98f, 22f * _uiScale);

			DrawCircle(c + new Vector2(-s * 0.26f, -s * 0.22f), s * 0.32f, color);
			DrawCircle(c + new Vector2(s * 0.26f, -s * 0.22f), s * 0.32f, color);
			DrawRect(new Rect2(c.X - s * 0.54f, c.Y - s * 0.30f, s * 1.08f, s * 0.58f), color);

			Vector2[] tri =
			{
				c + new Vector2(-s * 0.52f, -s * 0.02f),
				c + new Vector2(s * 0.52f, -s * 0.02f),
				c + new Vector2(0f, s * 0.62f),
			};
			DrawColoredPolygon(tri, color);
		}
	}
}

/// <summary>Красная вспышка на весь экран при попадании мусором.</summary>
public partial class FlashOverlay : Control
{
	private float _alpha;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		_alpha = 0f;
	}

	public void Start()
	{
		_alpha = 0.6f;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_alpha <= 0f)
		{
			return;
		}
		_alpha = Mathf.Max(0f, _alpha - (float)delta * 1.5f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_alpha <= 0f)
		{
			return;
		}
		DrawRect(new Rect2(Vector2.Zero, Size), new Color(1f, 0.16f, 0.12f, _alpha));
	}
}