using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// HUD поверх игрового мира: счёт и HP, красная вспышка от мусора,
/// оверлей «Игра окончена» и всплывающие надписи «+10».
/// </summary>
public partial class HUD : Control
{
	private const float HintDuration = 3.4f;
	private Label? _scoreLabel;
	private Label? _hpLabel;
	private int _lives = 3;
	private int _maxLives = 3;
	private FlashOverlay? _flash;
	private XpBar? _xpBar;
	private Control? _gameOverRoot;
	private Control? _startRoot;
	private Button? _miniGameButton;
	private bool _miniGameEnabled = true;
	private Label? _suikaHintLabel;
	private Label? _finalScore;
	private Label? _finalHigh;
	private PerkBar? _perkBar;
	private Label? _hint;
	private Control? _popups;

	/// <summary>Оверлей выбора перка (работает и на паузе игры).</summary>
	private PerkSelectionOverlay? _perkOverlay;

	/// <summary>Оверлей паузы (работает и на паузе игры).</summary>
	private PauseOverlay? _pauseOverlay;

	/// <summary>Выпадающее меню паузы у правого края поля.</summary>
	private PauseMenu? _pauseMenu;
	private bool _gameplayVisible;

	public event Action? PlayPressed;
	public event Action? MiniGamePressed;
	public event Action? ExitPressed;
	public event Action? RestartPressed;
	public event Action? MenuPressed;

	/// <summary>Круглая кнопка паузы в выдвижной панели.</summary>
	public event Action? PausePressed;

	/// <summary>Escape на клавиатуре: как системная кнопка «Назад».</summary>
	public event Action? BackRequested;

	/// <summary>Игрок продолжил игру из оверлея паузы.</summary>
	public event Action? ResumePressed;

	/// <summary>Игрок вышел в меню из оверлея паузы.</summary>
	public event Action? PauseMenuPressed;

	/// <summary>Игрок выбрал перк в оверлее выбора.</summary>
	public event Action<PerkId>? PerkPicked;

	/// <summary>Масштаб интерфейса задаётся один раз родителем GameScreen.</summary>
	public float UiScale => 1f;
	public Func<Vector2, Vector2>? WorldToUi { get; set; }

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		BuildUI();
		Resized += OnViewportResized;

		_perkOverlay = new PerkSelectionOverlay { ZIndex = 80 };
		AddChild(_perkOverlay);
		_perkOverlay.PerkPicked += OnPerkPicked;

		_pauseOverlay = new PauseOverlay { ZIndex = 70 };
		AddChild(_pauseOverlay);
		_pauseOverlay.ResumePressed += () => ResumePressed?.Invoke();
		_pauseOverlay.MenuPressed += () => PauseMenuPressed?.Invoke();
	}

	/// <summary>
	/// Escape работает как системная кнопка «Назад». Обрабатывает HUD, а не игровой узел:
	/// HUD продолжает получать ввод и на паузе (ProcessMode.Always).
	/// </summary>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key || key.Keycode != Key.Escape) return;
		BackRequested?.Invoke();
		GetViewport().SetInputAsHandled();
	}

	private void OnPerkPicked(PerkId id)
	{
		PerkPicked?.Invoke(id);
	}

	private void OnViewportResized()
	{
		// Обновляем адаптивные размеры HUD-элементов.
		float ui = UiScale;
		UpdateCounterPositions();
		_xpBar?.SetUiScale(ui);
		_perkBar?.SetUiScale(ui);
		_perkOverlay?.RecenterIfVisible();
		UpdatePopupTransform();
	}

	public void UpdatePopupTransform()
	{
		if (_popups == null) return;
		_popups.Position = WorldToUi?.Invoke(Vector2.Zero) ?? Vector2.Zero;
		_popups.Scale = WorldToUi == null ? Vector2.One : WorldToUi(Vector2.One) - WorldToUi(Vector2.Zero);
	}

	private void BuildUI()
	{
		float ui = UiScale;
		BuildCounters();
		BuildPauseMenu();

		_perkBar = new PerkBar();
		_perkBar.SetUiScale(ui);
		AddChild(_perkBar);

		_flash = new FlashOverlay();
		AddChild(_flash);

		_xpBar = new XpBar();
		_xpBar.SetUiScale(ui);
		AddChild(_xpBar);

		BuildGameOver();
		BuildStart();
		_popups = new Control { MouseFilter = MouseFilterEnum.Ignore, ZIndex = 50 };
		AddChild(_popups);
		UpdatePopupTransform();
	}

	private void BuildCounters()
	{
		// Две самостоятельные надписи поверх поля, без панели и контейнеров.
		_scoreLabel = MakeCounter("Score", "Score: 0");
		_hpLabel = MakeCounter("HP", $"HP: {_lives}/{_maxLives}");
		AddChild(_scoreLabel);
		AddChild(_hpLabel);
		UpdateCounterPositions();
	}

	/// <summary>
	/// Выдвижное меню паузы: полукруглый язычок у правого края по центру высоты,
	/// по нажатию он сменяется выезжающей панелью с круглой кнопкой паузы
	/// (см. <see cref="PauseMenu"/>).
	/// </summary>
	private void BuildPauseMenu()
	{
		_pauseMenu = new PauseMenu();
		_pauseMenu.PausePressed += () => PausePressed?.Invoke();
		AddChild(_pauseMenu);
		UpdatePauseMenu();
	}

	/// <summary>Сворачивает выпадающее меню: вызывается при любом игровом касании и смене экрана.</summary>
	public void HidePauseMenu() => _pauseMenu?.Collapse();

	/// <summary>
	/// Стрелка и пункт «Пауза» видны только в идущей партии и не спорят с оверлеями;
	/// сам пункт появляется лишь при развёрнутом меню.
	/// </summary>
	private void UpdatePauseMenu() =>
		_pauseMenu?.SetAvailable(_gameplayVisible && _perkOverlay?.Visible != true && _pauseOverlay?.Visible != true);

	private static Label MakeCounter(string name, string text)
	{
		var label = new Label { Name = name, Text = text, MouseFilter = MouseFilterEnum.Ignore, ZIndex = 10 };
		label.AddThemeFontSizeOverride("font_size", 32);
		label.AddThemeColorOverride("font_color", Colors.White);
		label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.12f, 0.09f, 0.85f));
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		return label;
	}

	private void UpdateCounterPositions()
	{
		if (_scoreLabel == null || _hpLabel == null) return;
		_scoreLabel.ResetSize();
		_hpLabel.ResetSize();
		_scoreLabel.Position = new Vector2(20f, 16f);
		_hpLabel.Position = new Vector2(Size.X - 20f - _hpLabel.Size.X, 16f);
	}

	private void UpdateHp()
	{
		if (_hpLabel != null) _hpLabel.Text = $"HP: {_lives}/{_maxLives}";
		UpdateCounterPositions();
	}

	/// <summary>Скрывает игровые показатели, пока открыт главное меню или мини-игра.</summary>
	public void SetGameplayVisible(bool visible)
	{
		_gameplayVisible = visible;
		if (_scoreLabel != null) _scoreLabel.Visible = visible;
		if (_hpLabel != null) _hpLabel.Visible = visible;
		if (_xpBar != null) _xpBar.Visible = visible;
		if (_perkBar != null && _perkBar.HasIcons) _perkBar.Visible = visible;
		if (!visible) _pauseMenu?.Collapse();
		UpdatePauseMenu();
		if (!visible)
		{
			if (IsInstanceValid(_hint)) _hint!.QueueFree();
			_hint = null;
			if (_popups != null)
				foreach (Node child in _popups.GetChildren()) child.QueueFree();
		}
	}

	private void BuildStart()
	{
		_startRoot = new Control
		{
			Visible = true,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		_startRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_startRoot);

		var backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.55f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_startRoot.AddChild(backdrop);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 14);
		ModalContent.Mount(_startRoot, box);

		float ui = UiScale;

		Label title = UiStyle.MakeLabel("Лягушка-охотница", (int)Mathf.Round(50f * ui), new Color("ffe066"));
		Label sub = UiStyle.MakeLabel("Собирай фрукты, не лови мусор!", (int)Mathf.Round(24f * ui), new Color("ffffff"));
		Label sub2 = UiStyle.MakeLabel("Упустишь фрукт — потеряешь жизнь.", (int)Mathf.Round(20f * ui), new Color(1f, 1f, 1f, 0.8f));
		Button play = UiStyle.MakeButton("Играть", ui, new Color("b4e863"));
		Button exit = UiStyle.MakeButton("Выйти", ui, new Color("ff8c7a"));
		_miniGameButton = UiStyle.MakeButton("Мини-игра: Suika", ui, new Color("ffd45e"));
		play.Pressed += () => PlayPressed?.Invoke();
		exit.Pressed += () => ExitPressed?.Invoke();
		_miniGameButton!.Pressed += () => MiniGamePressed?.Invoke();
		// Применяем состояние, заданное до создания кнопки (например из _Ready).
		SetMiniGameEnabled(_miniGameEnabled);

		box.AddChild(title);
		box.AddChild(sub);
		box.AddChild(sub2);
		box.AddChild(play);
		box.AddChild(_miniGameButton!);
		box.AddChild(exit);

		var madeByText = UiStyle.MakeLabel("Made by NetwarG and anmiha321", (int)Mathf.Round(20f * ui), new Color(1f, 1f, 1f, 0.8f));
		// Текст по центру в самом низу стартового экрана (вне центрируемого меню).
		madeByText.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		madeByText.AnchorTop = 1f;
		madeByText.AnchorBottom = 1f;
		madeByText.OffsetTop = -(int)Mathf.Round(42f * ui);
		madeByText.OffsetBottom = -(int)Mathf.Round(10f * ui);
		madeByText.HorizontalAlignment = HorizontalAlignment.Center;
		_startRoot.AddChild(madeByText);
	}

	/// <summary>Показывает главное меню с запуском режимов и выходом.</summary>
	public void ShowStart()
	{
		if (_startRoot != null)
		{
			_startRoot.Visible = true;
		}
		SetGameplayVisible(false);
	}

	/// <summary>Скрывает главное меню в начале игры.</summary>
	public void HideStart()
	{
		if (_startRoot != null)
		{
			_startRoot.Visible = false;
		}
	}

	/// <summary>
	/// Блокирует/разблокирует кнопку «Мини-игра: Suika» в зависимости от того,
	/// есть ли у игрока фрукты в запасе. Кнопка остаётся кликабельной всегда,
	/// чтобы при пустом запасе можно было показать подсказку.
	/// </summary>
	public void SetMiniGameEnabled(bool enabled)
	{
		_miniGameEnabled = enabled;
		if (_miniGameButton == null) return;
		_miniGameButton.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.35f);
	}

	/// <summary>
	/// Подсказка над кнопкой Suika, когда фруктов в запасе нет:
	/// плавно появляется и растворяется.
	/// </summary>
	public void ShowSuikaNeedFruitsHint()
	{
		float ui = UiScale;
		if (_suikaHintLabel == null)
		{
			_suikaHintLabel = new Label
			{

				Text = "Сначала собери фрукты в основном режиме!",
				MouseFilter = MouseFilterEnum.Ignore,
				HorizontalAlignment = HorizontalAlignment.Center,
				ZIndex = 60

			};
			_suikaHintLabel.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(22f * ui));
			_suikaHintLabel.AddThemeColorOverride("font_color", new Color("ffd45e"));
			_suikaHintLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
			_suikaHintLabel.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(2f * ui));
			_suikaHintLabel.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(2f * ui));
			AddChild(_suikaHintLabel);
		}

		_suikaHintLabel.ResetSize();
		// Позиционируем над кнопкой «Мини-игра: Suika» по её верхнему краю.
		if (_miniGameButton != null)
		{
			Vector2 btn = GetGlobalTransform().AffineInverse() * _miniGameButton.GlobalPosition;
			Vector2 btnSize = _miniGameButton.Size;
			_suikaHintLabel.Position = new Vector2(
				btn.X + btnSize.X * 0.5f - _suikaHintLabel.Size.X * 0.5f,
				btn.Y - _suikaHintLabel.Size.Y - 14f * ui);
		}
		else
		{
			// Запасной вариант: центр экрана, чуть выше средней зоны.
			Vector2 vp = Size;
			_suikaHintLabel.Position = new Vector2(
				vp.X * 0.5f - _suikaHintLabel.Size.X * 0.5f,
				vp.Y * 0.24f);
		}

		_suikaHintLabel.Visible = true;
		_suikaHintLabel.Modulate = new Color(1f, 1f, 1f, 1f);
		_suikaHintLabel.QueueRedraw();

		// Последовательный твин: сначала показываем, потом растворяем и скрываем.
		Tween tween = CreateTween();
		tween.TweenInterval(2.2f);
		tween.TweenProperty(_suikaHintLabel, "modulate:a", 0f, 0.6f);
		tween.Chain().TweenCallback(Callable.From(() => _suikaHintLabel.Visible = false));
	}

	private void BuildGameOver()
	{
		_gameOverRoot = new Control
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		_gameOverRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_gameOverRoot);

		var backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.62f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_gameOverRoot.AddChild(backdrop);

		var box = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		box.AddThemeConstantOverride("separation", 16);
		ModalContent.Mount(_gameOverRoot, box);

		float ui = UiScale;

		Label title = UiStyle.MakeLabel("Игра окончена!", (int)Mathf.Round(48f * ui), new Color("ffd45e"));
		_finalScore = UiStyle.MakeLabel("Счёт: 0", (int)Mathf.Round(32f * ui), new Color("ffffff"));
		_finalHigh = UiStyle.MakeLabel("Рекорд: 0", (int)Mathf.Round(32f * ui), new Color("cfe9c2"));
		Button restart = UiStyle.MakeButton("Заново", ui, new Color("b4e863"));
		Button menu = UiStyle.MakeButton("В меню", ui, new Color("ffd45e"));
		restart.Pressed += () => RestartPressed?.Invoke();
		menu.Pressed += () => MenuPressed?.Invoke();

		box.AddChild(title);
		box.AddChild(_finalScore);
		box.AddChild(_finalHigh);
		box.AddChild(restart);
		box.AddChild(menu);
	}

	/// <summary>Мимолётная подсказка «держи палец» после старта.</summary>
	public void ShowHint()
	{
		if (IsInstanceValid(_hint)) _hint!.QueueFree();
		float ui = UiScale;
		var hint = new Label
		{
			Text = "Держи палец на экране — руки растут",
			MouseFilter = MouseFilterEnum.Ignore,
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -240f * ui,
			OffsetRight = 240f * ui,
			OffsetTop = -130f * ui,
			OffsetBottom = -94f * ui,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		hint.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(18f * ui));
		hint.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.9f));
		hint.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		hint.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(2f * ui));
		hint.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(2f * ui));
		AddChild(hint);
		_hint = hint;

		Tween tween = hint.CreateTween();
		tween.TweenInterval(HintDuration);
		tween.TweenProperty(hint, "modulate:a", 0f, 0.7f);
		tween.TweenCallback(Callable.From(hint.QueueFree));
	}

	public void SetScore(int score)
	{
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = $"Score: {score}";
			UpdateCounterPositions();
		}
	}

	public void SetLives(int lives)
	{
		_lives = Mathf.Clamp(lives, 0, _maxLives);
		UpdateHp();
	}

	/// <summary>Обновляет панель выбранных перков внизу экрана.</summary>
	public void SetPickedPerks(PerkIconInfo[] perks) => _perkBar?.SetPerks(perks);

	/// <summary>Обновляет максимум жизней с учётом перка «Панцирь».</summary>
	public void SetMaxLives(int max)
	{
		_maxLives = Mathf.Max(3, max);
		_lives = Mathf.Min(_lives, _maxLives);
		UpdateHp();
	}

	/// <summary>Показывает оверлей выбора перка с карточками.</summary>
	public void ShowPerkSelection(int level, PerkOffer[] cards)
	{
		_perkOverlay?.Show(level, cards);
		UpdatePauseMenu();
	}

	/// <summary>Скрывает оверлей выбора перка.</summary>
	public void HidePerkSelection()
	{
		_perkOverlay?.Hide();
		UpdatePauseMenu();
	}

	/// <summary>Показывает оверлей паузы с показателями текущей партии.</summary>
	public void ShowPause(int score, int lives)
	{
		_pauseMenu?.Collapse();
		_pauseOverlay?.Show(score, lives);
		UpdatePauseMenu();
	}

	/// <summary>Скрывает оверлей паузы.</summary>
	public void HidePause()
	{
		_pauseOverlay?.Hide();
		UpdatePauseMenu();
	}

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
		SetGameplayVisible(false);
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
		var popup = new Label
		{
			Text = text,
			Position = globalPos,
			MouseFilter = MouseFilterEnum.Ignore
		};
		popup.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(30f * ui));
		popup.AddThemeColorOverride("font_color", new Color("fff45e"));
		popup.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
		popup.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(2f * ui));
		popup.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(2f * ui));
		_popups!.AddChild(popup);

		Tween tween = popup.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(popup, "position:y", popup.Position.Y - 80f, 0.9f);
		tween.TweenProperty(popup, "modulate:a", 0f, 0.9f);
		tween.Chain().TweenCallback(Callable.From(popup.QueueFree));
	}

	/// <summary>Обновляет шкалу опыта: номер уровня и прогресс 0..1.</summary>
	public void SetXp(int level, float progress)
	{
		_xpBar?.SetLevel(level);
		_xpBar?.SetProgress(progress);
	}

	/// <summary>Короткий ненавязчивый фидбек при повышении уровня.</summary>
	public void ShowLevelUp(int newLevel)
	{
		_xpBar?.PulseLevelUp();
		_xpBar?.ShowLevelUpLabel(newLevel);
	}
}

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
		_levelUpLabel.Text = $"Уровень {level}!";
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
		_levelLabel.Text = $"LV {_level}";

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
