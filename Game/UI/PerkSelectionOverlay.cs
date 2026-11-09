using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Оверлей выбора перка: показывается поверх HUD, когда игра остановлена
/// (SceneTree.Paused). Благодаря ProcessMode.Always этот узел продолжает
/// работать во время паузы — карточки реагируют на ввод. Выбор перка
/// происходит удержанием карточки 1 секунду (см. <see cref="HoldCardButton"/>),
/// случайный короткий клик выбор не срабатывает.
/// </summary>
public partial class PerkSelectionOverlay : Control
{
	private const float CardWidth = 600f;
	private const float CardHeight = 200f;
	private const float ScreenPadding = 24f;

	public event Action<PerkId>? PerkPicked;

	private ColorRect? _backdrop;
	private VBoxContainer? _box;
	private PerkOffer[]? _cards;
	private int _level;
	private int _selectedCount;
	private int _maxPerks = PerkState.MaxDistinctPerks;
	private bool _fitQueued;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Ignore;
		// Сбрасываем и смещения: одни якоря сохраняют прежний нулевой размер Control.
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.66f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(_backdrop);
		_backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_box = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		_box.AddThemeConstantOverride("separation", 14);

		// Обычный Control не навязывает размер контейнеру: весь блок можно вписать целиком.
		AddChild(_box);
		_box.MinimumSizeChanged += QueueFit;
		_box.Resized += QueueFit;
		Resized += RecenterIfVisible;

		Visible = false;
		Loc.Changed += OnLanguageChanged;
	}

	public override void _ExitTree() => Loc.Changed -= OnLanguageChanged;

	/// <summary>Смена языка: карточки перков пересобираются на новом языке, если окно открыто.</summary>
	private void OnLanguageChanged()
	{
		if (!Visible)
			return;
		RebuildCards();
		Recenter();
	}

	/// <summary>Показывает оверлей с карточками перков.</summary>
	public void Show(int level, PerkOffer[] cards, int selectedCount, int maxPerks)
	{
		_level = level;
		_cards = cards;
		_selectedCount = selectedCount;
		_maxPerks = maxPerks;
		RebuildCards();
		Visible = true;
		Recenter();
	}

	/// <summary>Скрывает оверлей.</summary>
	public void Hide()
	{
		Visible = false;
	}

	/// <summary>Вписывает все карточки в безопасную область при изменении окна.</summary>
	public void RecenterIfVisible()
	{
		if (Visible)
		{
			Recenter();
		}
	}

	private void Recenter()
	{
		if (_box == null)
		{
			return;
		}
		ApplyContentWidth(GetContentWidth(UiScale));
		QueueFit();
	}

	private void QueueFit()
	{
		if (_fitQueued) return;
		_fitQueued = true;
		Callable.From(FitContent).CallDeferred();
	}

	private void FitContent()
	{
		_fitQueued = false;
		if (!Visible || _box == null || Size.X <= 0f || Size.Y <= 0f) return;
		// Переносы текста меняют минимальную высоту после раскладки контейнера.
		// Повторный MinimumSizeChanged уточнит масштаб, не создавая прокрутку или обрезку.
		_box.ResetSize();
		Vector2 available = AvailableSize();
		Vector2 content = _box.Size;
		float scale = Mathf.Min(1f, Mathf.Min(available.X / Mathf.Max(1f, content.X), available.Y / Mathf.Max(1f, content.Y)));
		_box.Scale = Vector2.One * scale;
		_box.Position = (Size - content * scale) * 0.5f;
	}

	private Vector2 AvailableSize()
	{
		float padding = Mathf.Min(ScreenPadding, Mathf.Min(Size.X, Size.Y) * 0.04f);
		return (Size - Vector2.One * (padding * 2f)).Max(Vector2.One);
	}

	public float UiScale => 1f;

	private void RebuildCards()
	{
		if (_box == null)
		{
			return;
		}
		foreach (Node child in _box.GetChildren())
		{
			_box.RemoveChild(child);
			child.QueueFree();
		}
		if (_cards == null)
		{
			return;
		}

		float ui = UiScale;
		float contentWidth = GetContentWidth(ui);
		_box.CustomMinimumSize = new Vector2(contentWidth, 0f);

		_box.AddChild(MakeLabel(Loc.T(L.PerkNewTitle, _level), (int)Mathf.Round(44f * ui), new Color("ffd45e")));
		_box.AddChild(MakeLabel(Loc.T(L.PerkCount, _selectedCount, _maxPerks), (int)Mathf.Round(22f * ui), new Color(1f, 1f, 1f, 0.92f)));

		foreach (PerkOffer card in _cards)
		{
			PerkId id = card.Id;
			string text = Loc.T(L.PerkCard, card.Name, card.CurrentLevel, card.MaxLevel, card.CurrentLevel + 1, card.NextLevelEffect);
			HoldCardButton button = MakeCardButton(text, card.Accent, PerkCatalog.Icon(card.Id), ui);
			button.Held += () => PerkPicked?.Invoke(id);
			_box.AddChild(button);
		}

		_box.AddChild(MakeLabel(Loc.T(L.PerkFooter, _maxPerks), (int)Mathf.Round(24f * ui), new Color(1f, 1f, 1f, 0.75f)));
	}

	private HoldCardButton MakeCardButton(string text, Color accent, Texture2D? icon, float ui)
	{
		var button = new HoldCardButton
		{
			Text = text,
			Icon = icon,
			ExpandIcon = true,
			IconAlignment = HorizontalAlignment.Left,
			VerticalIconAlignment = VerticalAlignment.Center,
			Alignment = HorizontalAlignment.Left,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(GetContentWidth(ui), CardHeight * ui),
			FocusMode = Control.FocusModeEnum.None,
			MouseFilter = MouseFilterEnum.Pass,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			UiScale = ui,
			CornerRadius = UiStyle.CornerRadius * ui,
			OutlineWidth = StickerStyle.OutlineWidth * ui,
			MarginX = 16f * ui,
			FillColor = new Color(accent.Lightened(0.72f), 0.95f)
		};
		button.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(24f * ui));
		button.AddThemeConstantOverride("icon_max_width", (int)Mathf.Round(112f * ui));
		button.AddThemeConstantOverride("h_separation", (int)Mathf.Round(18f * ui));
		button.AddThemeColorOverride("font_color", new Color("173c34"));
		button.AddThemeColorOverride("font_hover_color", new Color("102c27"));
		button.AddThemeColorOverride("font_pressed_color", new Color("173c34"));
		button.SetAccent(accent);
		return button;
	}

	private Label MakeLabel(string text, int fontSize, Color color)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(GetContentWidth(UiScale), 0f)
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private float GetContentWidth(float ui)
	{
		return Mathf.Min(CardWidth * ui, AvailableSize().X);
	}

	private void ApplyContentWidth(float contentWidth)
	{
		if (_box == null)
		{
			return;
		}

		_box.CustomMinimumSize = new Vector2(contentWidth, _box.CustomMinimumSize.Y);
		foreach (Node child in _box.GetChildren())
		{
			if (child is Control control)
			{
				control.CustomMinimumSize = new Vector2(contentWidth, control.CustomMinimumSize.Y);
			}
		}
	}
}
