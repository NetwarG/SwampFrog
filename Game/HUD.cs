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
	private Label? _finalScore;
	private Label? _finalHigh;

	public override void _Ready()
	{
		BuildUI();
	}

	private void BuildUI()
	{
		_scoreLabel = new Label();
		_scoreLabel.Text = "0";
		_scoreLabel.Position = new Vector2(18f, 12f);
		_scoreLabel.AddThemeFontSizeOverride("font_size", 44);
		_scoreLabel.AddThemeColorOverride("font_color", new Color("ffffff"));
		_scoreLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
		_scoreLabel.AddThemeConstantOverride("shadow_offset_x", 3);
		_scoreLabel.AddThemeConstantOverride("shadow_offset_y", 3);
		AddChild(_scoreLabel);

		_hearts = new HeartsIndicator();
		AddChild(_hearts);

		_flash = new FlashOverlay();
		AddChild(_flash);

		BuildGameOver();

		ShowHint();
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

		Label title = MakeLabel("Игра окончена!", 48, new Color("ffd45e"));
		_finalScore = MakeLabel("Счёт: 0", 32, new Color("ffffff"));
		_finalHigh = MakeLabel("Рекорд: 0", 32, new Color("cfe9c2"));
		Label hint = MakeLabel("Нажми, чтобы начать заново", 22, new Color(1f, 1f, 1f, 0.85f));

		box.AddChild(title);
		box.AddChild(_finalScore);
		box.AddChild(_finalHigh);
		box.AddChild(hint);

		box.ResetSize();
		Vector2 vp = GetViewport().GetVisibleRect().Size;
		box.Position = new Vector2(vp.X * 0.5f - box.Size.X * 0.5f, vp.Y * 0.5f - box.Size.Y * 0.5f);
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

	private void ShowHint()
	{
		var hint = new Label();
		hint.Text = "Держи палец на экране — руки растут";
		hint.AnchorLeft = 0.5f;
		hint.AnchorRight = 0.5f;
		hint.AnchorTop = 1f;
		hint.AnchorBottom = 1f;
		hint.OffsetLeft = -240f;
		hint.OffsetRight = 240f;
		hint.OffsetTop = -46f;
		hint.OffsetBottom = -14f;
		hint.HorizontalAlignment = HorizontalAlignment.Center;
		hint.AddThemeFontSizeOverride("font_size", 18);
		hint.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.9f));
		hint.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		hint.AddThemeConstantOverride("shadow_offset_x", 2);
		hint.AddThemeConstantOverride("shadow_offset_y", 2);
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
		var popup = new Label();
		popup.Text = text;
		popup.GlobalPosition = globalPos;
		popup.AddThemeFontSizeOverride("font_size", 30);
		popup.AddThemeColorOverride("font_color", new Color("fff45e"));
		popup.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
		popup.AddThemeConstantOverride("shadow_offset_x", 2);
		popup.AddThemeConstantOverride("shadow_offset_y", 2);
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

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		AnchorLeft = 1f;
		AnchorRight = 1f;
		AnchorTop = 0f;
		AnchorBottom = 0f;
		OffsetLeft = -118f;
		OffsetRight = -14f;
		OffsetTop = 14f;
		OffsetBottom = 78f;
	}

	public void SetLives(int lives)
	{
		_lives = Mathf.Clamp(lives, 0, 3);
		QueueRedraw();
	}

	public override void _Draw()
	{
		const float s = 30f;
		for (int i = 0; i < 3; i++)
		{
			Color color = i < _lives ? FullHeart : EmptyHeart;
			Vector2 c = new(12f + i * s * 0.98f, 22f);

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