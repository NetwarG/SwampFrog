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

	/// <summary>Отступ строки счёта и жизней от верхнего левого угла поля.</summary>
	private const float CounterMarginX = 20f;
	private const float CounterMarginY = 16f;

	/// <summary>Зазор между счётом и жизнями в одной строке.</summary>
	private const float CounterGap = 24f;

	private Label? _scoreLabel;
	private HealthDisplay? _healthDisplay;
	private int _lives = 3;
	private int _maxLives = 3;
	private FlashOverlay? _flash;
	private XpBar? _xpBar;
	private Control? _gameOverRoot;
	private Control? _startRoot;
	private Button? _miniGameButton;
	private IconStickerButton? _profileButton;
	private IconStickerButton? _shopButton;
	private IconStickerButton? _upButton;
	private HBoxContainer? _menuIcons;
	private WalletBadge? _walletBadge;
	private bool _miniGameEnabled = true;
	private Label? _suikaHintLabel;
	private Label? _finalScore;
	private Label? _finalHigh;
	private PerkBar? _perkBar;
	private Label? _hint;
	private Control? _popups;

	private int _score;
	private Label? _startSub;
	private Label? _startSub2;
	private Button? _playButton;
	private Button? _exitButton;
	private Label? _gameOverTitle;
	private Button? _restartButton;
	private Button? _gameOverMenuButton;

	/// <summary>Оверлей выбора перка (работает и на паузе игры).</summary>
	private PerkSelectionOverlay? _perkOverlay;

	/// <summary>Оверлей паузы (работает и на паузе игры).</summary>
	private PauseOverlay? _pauseOverlay;

	/// <summary>Выпадающее меню паузы у правого края поля.</summary>
	private PauseMenu? _pauseMenu;

	/// <summary>Экран профиля игрока поверх главного меню.</summary>
	private ProfilePanel? _profilePanel;

	/// <summary>Галерея скинов лягушки поверх профиля.</summary>
	private SkinGalleryPanel? _skinGallery;
	private bool _gameplayVisible;

	public event Action? PlayPressed;
	public event Action? MiniGamePressed;
	public event Action? ProfilePressed;
	public event Action? ExitPressed;

	/// <summary>Игрок открыл магазин скинов (кнопка с домиком).</summary>
	public event Action? ShopPressed;

	/// <summary>Игрок выбрал скин в галерее.</summary>
	public event Action<SkinId>? SkinSelected;

	/// <summary>Игрок запросил покупку скина.</summary>
	public event Action<SkinId>? SkinPurchased;

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

		_profilePanel = new ProfilePanel { ZIndex = 75 };
		AddChild(_profilePanel);

		_skinGallery = new SkinGalleryPanel { ZIndex = 76 };
		AddChild(_skinGallery);
		_skinGallery.SkinSelected += id => SkinSelected?.Invoke(id);
		_skinGallery.SkinPurchased += id => SkinPurchased?.Invoke(id);

		Loc.Changed += Localize;
	}

	public override void _ExitTree() => Loc.Changed -= Localize;

	/// <summary>Перечитывает подписи интерфейса при смене языка (см. <see cref="Loc.Changed"/>).</summary>
	private void Localize()
	{
		if (_startSub != null) _startSub.Text = Loc.T(L.MenuSubtitle1);
		if (_startSub2 != null) _startSub2.Text = Loc.T(L.MenuSubtitle2);
		if (_playButton != null) _playButton.Text = Loc.T(L.MenuPlay);
		if (_exitButton != null) _exitButton.Text = Loc.T(L.MenuExit);
		if (_miniGameButton != null) _miniGameButton.Text = Loc.T(L.MenuMiniGame);
		if (_upButton != null) _upButton.SetCaption(Loc.T(L.MenuUp));
		if (_gameOverTitle != null) _gameOverTitle.Text = Loc.T(L.GameOverTitle);
		if (_restartButton != null) _restartButton.Text = Loc.T(L.Restart);
		if (_gameOverMenuButton != null) _gameOverMenuButton.Text = Loc.T(L.ToMenu);
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = Loc.T(L.Score, _score);
			UpdateCounterPositions();
		}
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
		// Счёт и жизни располагаются в одной строке слева, без панели и контейнеров.
		_scoreLabel = MakeCounter("Score", Loc.T(L.Score, 0));
		_healthDisplay = new HealthDisplay
		{
			Name = "HP",
			ZIndex = 10
		};
		AddChild(_scoreLabel);
		AddChild(_healthDisplay);
		_healthDisplay.SetLives(_lives, _maxLives);
		UpdateCounterPositions();
	}

	/// <summary>
	/// Меню паузы: кнопка с тремя точками в правом верхнем углу поля, по нажатию
	/// она сменяется выезжающей панелью с круглой кнопкой паузы
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
	/// Кнопка паузы видна только в идущей партии и не спорит с оверлеями.
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
		if (_scoreLabel == null || _healthDisplay == null) return;
		_scoreLabel.ResetSize();
		_healthDisplay.SetLives(_lives, _maxLives);
		// Счёт и жизни держатся в одной строке слева: HP встаёт сразу за счётом.
		_scoreLabel.Position = new Vector2(CounterMarginX, CounterMarginY);
		_healthDisplay.Position = new Vector2(_scoreLabel.Position.X + _scoreLabel.Size.X + CounterGap, CounterMarginY);
	}

	private void UpdateHp()
	{
		_healthDisplay?.SetLives(_lives, _maxLives);
		UpdateCounterPositions();
	}

	/// <summary>Скрывает игровые показатели, пока открыт главное меню или мини-игра.</summary>
	public void SetGameplayVisible(bool visible)
	{
		_gameplayVisible = visible;
		if (_scoreLabel != null) _scoreLabel.Visible = visible;
		if (_healthDisplay != null) _healthDisplay.Visible = visible;
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

		// Светлый фон меню: вода, кувшинки, цветки и парящие фрукты по всему экрану.
		_startRoot.AddChild(new MenuBackground());

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 14);
		ModalContent.Mount(_startRoot, box);

		float ui = UiScale;

		Label title = UiStyle.MakeLabel("Swamp Frog", (int)Mathf.Round(50f * ui), new Color("ffe066"));
		_startSub = UiStyle.MakeLabel(Loc.T(L.MenuSubtitle1), (int)Mathf.Round(24f * ui), new Color("ffffff"));
		_startSub2 = UiStyle.MakeLabel(Loc.T(L.MenuSubtitle2), (int)Mathf.Round(20f * ui), new Color(1f, 1f, 1f));
		// Тень и обводка: на светлом фоне меню надписи остаются читаемыми.
		AddMenuTextShadow(title, ui);
		AddMenuTextShadow(_startSub, ui);
		AddMenuTextShadow(_startSub2, ui);
		_playButton = UiStyle.MakeButton(Loc.T(L.MenuPlay), ui, new Color("b4e863"));
		_exitButton = UiStyle.MakeButton(Loc.T(L.MenuExit), ui, new Color("ff8c7a"));
		_miniGameButton = UiStyle.MakeButton(Loc.T(L.MenuMiniGame), ui, new Color("ffd45e"));
		_playButton.Pressed += () => PlayPressed?.Invoke();
		_exitButton.Pressed += () => ExitPressed?.Invoke();
		_miniGameButton!.Pressed += () => MiniGamePressed?.Invoke();
		BuildMenuIcons(ui);
		// Применяем состояние, заданное до создания кнопки (например из _Ready).
		SetMiniGameEnabled(_miniGameEnabled);

		box.AddChild(title);
		box.AddChild(_startSub);
		box.AddChild(_startSub2);
		_walletBadge = new WalletBadge();
		box.AddChild(_walletBadge);
		box.AddChild(_playButton);
		box.AddChild(_miniGameButton!);
		box.AddChild(_menuIcons!);
		box.AddChild(_exitButton);

		var madeByText = UiStyle.MakeLabel("Made by NetwarG and anmiha321", (int)Mathf.Round(20f * ui), new Color(1f, 1f, 1f));
		AddMenuTextShadow(madeByText, ui);
		// Текст по центру в самом низу стартового экрана (вне центрируемого меню).
		madeByText.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		madeByText.AnchorTop = 1f;
		madeByText.AnchorBottom = 1f;
		madeByText.OffsetTop = -(int)Mathf.Round(42f * ui);
		madeByText.OffsetBottom = -(int)Mathf.Round(10f * ui);
		madeByText.HorizontalAlignment = HorizontalAlignment.Center;
		_startRoot.AddChild(madeByText);

		// Управление звуком и языком показывается вместе с меню и скрывается вместе с ним:
		// в партии, Suika и оверлеях их нет (см. SoundControls, LanguageButton).
		_startRoot.AddChild(new SoundControls { ZIndex = 20 });
		var language = new LanguageButton { ZIndex = 20 };
		_startRoot.AddChild(language);
		language.Position = new Vector2(16f * ui, 16f * ui);
	}

	/// <summary>
	/// Тень и обводка надписи меню: на светлом фоне сохраняется контраст.
	/// Общие стили не меняются — только метки стартового экрана.
	/// </summary>
	private static void AddMenuTextShadow(Label label, float ui)
	{
		label.AddThemeColorOverride("font_outline_color", new Color(0.03f, 0.16f, 0.13f, 1f));
		label.AddThemeConstantOverride("outline_size", (int)Mathf.Round(6f * ui));
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0.05f, 0.04f, 0.45f));
		label.AddThemeConstantOverride("shadow_offset_x", (int)Mathf.Round(2f * ui));
		label.AddThemeConstantOverride("shadow_offset_y", (int)Mathf.Round(3f * ui));
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

	/// <summary>Показывает экран профиля поверх главного меню.</summary>
	public void ShowProfile(ProfileSnapshot snapshot) => _profilePanel?.Show(snapshot);

	/// <summary>Показывает баланс валюты «Слизь» в меню.</summary>
	public void SetWallet(int amount) => _walletBadge?.SetAmount(amount);

	/// <summary>Строка иконочных кнопок меню: профиль, магазин скинов и улучшения («Up»).</summary>
	private void BuildMenuIcons(float ui)
	{
		_profileButton = MakeIconButton("res://Game/assets/ui/icon_profile.png", new Color("7fd6f2"), ui, "ProfileButton");
		_shopButton = MakeIconButton("res://Game/assets/ui/icon_shop.png", new Color("ffd45e"), ui, "ShopButton");
		_upButton = MakeIconButton("res://Game/assets/ui/icon_upgrade.png", new Color("b4e863"), ui, "UpButton");
		_profileButton.Pressed += () => ProfilePressed?.Invoke();
		_shopButton.Pressed += () => ShopPressed?.Invoke();
		// Улучшения (Фаза 2) ещё не сделаны: кнопка приглушена и не нажимается.
		_upButton.Disabled = true;
		_upButton.Modulate = new Color(1f, 1f, 1f, 0.4f);

		_menuIcons = new HBoxContainer { Name = "MenuIcons", Alignment = BoxContainer.AlignmentMode.Center };
		_menuIcons.AddThemeConstantOverride("separation", (int)Mathf.Round(14f * ui));
		_menuIcons.AddChild(_profileButton);
		_menuIcons.AddChild(_shopButton);
		_menuIcons.AddChild(_upButton);
	}

	/// <summary>Квадратная иконочная кнопка меню в общем стиле кнопок.</summary>
	private static IconStickerButton MakeIconButton(string iconPath, Color accent, float ui, string name)
	{
		const float size = 100f;
		var button = new IconStickerButton
		{
			Name = name,
			CustomMinimumSize = new Vector2(size * ui, size * ui),
			MouseFilter = MouseFilterEnum.Pass,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			CornerRadius = UiStyle.CornerRadius * ui * 1.6f
		};
		button.SetAccent(accent);
		button.SetIcon(iconPath);
		return button;
	}

	/// <summary>Показывает галерею скинов поверх профиля.</summary>
	public void ShowSkinGallery(SkinGalleryEntry[] entries) => _skinGallery?.Show(entries);

	/// <summary>Скрывает галерею скинов.</summary>
	public void HideSkinGallery() => _skinGallery?.Hide();

	/// <summary>Открыта ли галерея скинов: Escape/«Назад» закрывает её первым.</summary>
	public bool IsSkinGalleryOpen => _skinGallery?.Visible ?? false;

	/// <summary>Скрывает экран профиля.</summary>
	public void HideProfile() => _profilePanel?.Hide();

	/// <summary>Открыт ли экран профиля: Escape/«Назад» закрывает его первым.</summary>
	public bool IsProfileOpen => _profilePanel?.Visible ?? false;

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

				Text = Loc.T(L.NeedFruits),
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

		_gameOverTitle = UiStyle.MakeLabel(Loc.T(L.GameOverTitle), (int)Mathf.Round(48f * ui), new Color("ffd45e"));
		_finalScore = UiStyle.MakeLabel(Loc.T(L.Score, 0), (int)Mathf.Round(32f * ui), new Color("ffffff"));
		_finalHigh = UiStyle.MakeLabel(Loc.T(L.HighScore, 0), (int)Mathf.Round(32f * ui), new Color("cfe9c2"));
		_restartButton = UiStyle.MakeButton(Loc.T(L.Restart), ui, new Color("b4e863"));
		_gameOverMenuButton = UiStyle.MakeButton(Loc.T(L.ToMenu), ui, new Color("ffd45e"));
		_restartButton.Pressed += () => RestartPressed?.Invoke();
		_gameOverMenuButton.Pressed += () => MenuPressed?.Invoke();

		box.AddChild(_gameOverTitle);
		box.AddChild(_finalScore);
		box.AddChild(_finalHigh);
		box.AddChild(_restartButton);
		box.AddChild(_gameOverMenuButton);
	}

	/// <summary>Мимолётная подсказка «держи палец» после старта.</summary>
	public void ShowHint()
	{
		if (IsInstanceValid(_hint)) _hint!.QueueFree();
		float ui = UiScale;
		var hint = new Label
		{
			Text = Loc.T(L.HintTouch),
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
		_score = score;
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = Loc.T(L.Score, score);
			UpdateCounterPositions();
		}
	}

	public void SetLives(int lives)
	{
		_lives = Mathf.Clamp(lives, 0, _maxLives);
		UpdateHp();
	}

	/// <summary>Обновляет панель выбранных перков внизу экрана.</summary>
	public void SetPickedPerks(PerkIconInfo[] perks) => _perkBar?.SetPerks(perks, PerkState.MaxDistinctPerks);

	/// <summary>Всплеск квадратика перка в панели: видно, что перк сработал.</summary>
	public void PulsePerkIcon(PerkId id) => _perkBar?.Pulse(id);

	/// <summary>Прогресс перезарядки перка в панели: 0 — только что сработал, 1 — готов.</summary>
	public void SetPerkCooldown(PerkId id, float progress) => _perkBar?.SetCooldown(id, progress);

	/// <summary>Обновляет максимум жизней с учётом перка «Панцирь».</summary>
	public void SetMaxLives(int max)
	{
		_maxLives = Mathf.Max(3, max);
		_lives = Mathf.Min(_lives, _maxLives);
		UpdateHp();
	}

	/// <summary>Показывает оверлей выбора перка с карточками.</summary>
	public void ShowPerkSelection(int level, PerkOffer[] cards, int selectedCount, int maxPerks)
	{
		_perkOverlay?.Show(level, cards, selectedCount, maxPerks);
		UpdatePauseMenu();
	}

	/// <summary>Скрывает оверлей выбора перка.</summary>
	public void HidePerkSelection()
	{
		_perkOverlay?.Hide();
		UpdatePauseMenu();
	}

	/// <summary>Показывает оверлей паузы с показателями партии и списком субперков.</summary>
	public void ShowPause(int score, int lives, SubPerkSummary[] subPerks)
	{
		_pauseMenu?.Collapse();
		_pauseOverlay?.Show(score, lives, subPerks);
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
			_finalScore.Text = Loc.T(L.Score, score);
		}
		if (_finalHigh != null)
		{
			_finalHigh.Text = Loc.T(L.HighScore, highScore);
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
