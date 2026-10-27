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

		_box.AddChild(MakeLabel($"Новый перк!  Уровень {_level}", (int)Mathf.Round(44f * ui), new Color("ffd45e")));
		_box.AddChild(MakeLabel($"Перки: {_selectedCount}/{_maxPerks} — выбери одно улучшение, удерживай карточку 1 сек", (int)Mathf.Round(22f * ui), new Color(1f, 1f, 1f, 0.92f)));

		foreach (PerkOffer card in _cards)
		{
			PerkId id = card.Id;
			string text = $"{card.Name}\n“{card.Tagline}”\nУр. {card.CurrentLevel}/{card.MaxLevel} → {card.CurrentLevel + 1}\n{card.NextLevelEffect}";
			HoldCardButton button = MakeCardButton(text, card.Accent, ui);
			button.Held += () => PerkPicked?.Invoke(id);
			_box.AddChild(button);
		}

		_box.AddChild(MakeLabel($"За забег можно взять {_maxPerks} перков — потом их можно докачать до максимума", (int)Mathf.Round(24f * ui), new Color(1f, 1f, 1f, 0.75f)));
	}

	private HoldCardButton MakeCardButton(string text, Color accent, float ui)
	{
		var button = new HoldCardButton
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(GetContentWidth(ui), CardHeight * ui),
			FocusMode = Control.FocusModeEnum.None,
			MouseFilter = MouseFilterEnum.Pass,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			UiScale = ui,
			BarColor = new Color(accent, 0.9f)
		};
		button.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(24f * ui));
		button.AddThemeColorOverride("font_color", new Color("173c34"));
		button.AddThemeColorOverride("font_hover_color", new Color("102c27"));
		button.AddThemeColorOverride("font_pressed_color", new Color("173c34"));
		button.AddThemeStyleboxOverride("normal", MakeCardStyle(new Color(accent, 0.92f), accent, ui, 0.18f));
		button.AddThemeStyleboxOverride("hover", MakeCardStyle(new Color(accent, 1f), new Color("ffffff"), ui, 0.32f));
		button.AddThemeStyleboxOverride("pressed", MakeCardStyle(new Color(accent, 0.72f), new Color("ffffff"), ui, 0.10f));
		button.AddThemeStyleboxOverride("focus", MakeCardStyle(new Color(accent, 0.92f), accent, ui, 0.18f));
		return button;
	}

	private static StyleBoxFlat MakeCardStyle(Color background, Color border, float ui, float shadowAlpha)
	{
		var style = new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = new Color(border, 0.72f),
			CornerRadiusTopLeft = (int)Mathf.Round(14f * ui),
			CornerRadiusTopRight = (int)Mathf.Round(14f * ui),
			CornerRadiusBottomLeft = (int)Mathf.Round(14f * ui),
			CornerRadiusBottomRight = (int)Mathf.Round(14f * ui),
			ShadowColor = new Color(0f, 0f, 0f, shadowAlpha),
			ShadowSize = (int)Mathf.Round(5f * ui)
		};
		style.SetBorderWidthAll((int)Mathf.Round(2f * ui));
		style.ContentMarginLeft = 16f * ui;
		style.ContentMarginRight = 16f * ui;
		return style;
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
