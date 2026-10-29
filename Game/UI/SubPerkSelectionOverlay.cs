using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Оверлей выбора субперка: показывается поверх HUD, когда игра остановлена
/// (SceneTree.Paused). Благодаря ProcessMode.Always карточки продолжают работать
/// на паузе. В отличие от основных перков субперк выбирается одним тапом —
/// удержание не нужно, но пропустить взятие нельзя.
/// </summary>
public partial class SubPerkSelectionOverlay : Control
{
	private const float CardWidth = 520f;
	private const float CardHeight = 128f;

	public event Action<SubPerkId>? SubPerkPicked;

	private ColorRect? _backdrop;
	private VBoxContainer? _box;
	private SubPerkOffer[]? _cards;
	private int _level;
	private SubPerkPolarity _polarity;

	public float UiScale => 1f;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.66f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(_backdrop);
		_backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Блок вписывается в безопасную область без прокрутки (см. ModalContent).
		_box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_box.AddThemeConstantOverride("separation", 12);
		ModalContent.Mount(this, _box);

		Visible = false;
	}

	/// <summary>Показывает оверлей с карточками субперков одной полярности.</summary>
	public void Show(int level, SubPerkOffer[] cards, SubPerkPolarity polarity)
	{
		_level = level;
		_cards = cards;
		_polarity = polarity;
		RebuildCards();
		Visible = true;
	}

	/// <summary>Скрывает оверлей.</summary>
	public void Hide() => Visible = false;

	private void RebuildCards()
	{
		if (_box == null)
			return;
		foreach (Node child in _box.GetChildren())
		{
			_box.RemoveChild(child);
			child.QueueFree();
		}
		if (_cards == null)
			return;

		float ui = UiScale;
		float contentWidth = CardWidth * ui;
		_box.CustomMinimumSize = new Vector2(contentWidth, 0f);

		string polarityText = _polarity == SubPerkPolarity.Positive ? "положительные" : "отрицательные";
		_box.AddChild(MakeLabel($"Субперк!  Уровень {_level}", (int)Mathf.Round(38f * ui), new Color("ffd45e")));
		_box.AddChild(MakeLabel($"Сейчас — {polarityText}. Коснись карточки: пропустить нельзя.", (int)Mathf.Round(20f * ui), new Color(1f, 1f, 1f, 0.9f)));

		foreach (SubPerkOffer card in _cards)
		{
			SubPerkId id = card.Id;
			string text = $"{card.Name}\nУр. {card.CurrentLevel} → {card.CurrentLevel + 1}    {card.GainPercent}%";
			Button button = MakeCardButton(text, card.Accent, ui, contentWidth);
			button.Pressed += () => SubPerkPicked?.Invoke(id);
			_box.AddChild(button);
		}
	}

	private static Button MakeCardButton(string text, Color accent, float ui, float contentWidth)
	{
		var button = new Button
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(contentWidth, CardHeight * ui),
			FocusMode = Control.FocusModeEnum.None,
			MouseFilter = MouseFilterEnum.Pass,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		button.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(22f * ui));
		button.AddThemeColorOverride("font_color", new Color("173c34"));
		button.AddThemeColorOverride("font_hover_color", new Color("102c27"));
		button.AddThemeColorOverride("font_pressed_color", new Color("173c34"));
		button.AddThemeStyleboxOverride("normal", UiStyle.MakeButtonStyle(new Color(accent, 0.92f), accent, ui, 0.18f));
		button.AddThemeStyleboxOverride("hover", UiStyle.MakeButtonStyle(new Color(accent, 1f), new Color("ffffff"), ui, 0.32f));
		button.AddThemeStyleboxOverride("pressed", UiStyle.MakeButtonStyle(new Color(accent, 0.72f), new Color("ffffff"), ui, 0.10f));
		button.AddThemeStyleboxOverride("focus", UiStyle.MakeButtonStyle(new Color(accent, 0.92f), accent, ui, 0.18f));
		return button;
	}

	private static Label MakeLabel(string text, int fontSize, Color color)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}
}
