using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Оверлей выбора перка: показывается поверх HUD, когда игра остановлена
/// (SceneTree.Paused). Благодаря ProcessMode.Always этот узел продолжает
/// работать во время паузы — кнопки карточек остаются кликабельными.
/// </summary>
public partial class PerkSelectionOverlay : Control
{
	public event Action<PerkId>? PerkPicked;

	private ColorRect? _backdrop;
	private VBoxContainer? _box;
	private PerkOffer[]? _cards;
	private int _level;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);

		_backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.66f),
			MouseFilter = MouseFilterEnum.Stop
		};
		_backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_backdrop);

		_box = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		_box.AddThemeConstantOverride("separation", 14);
		AddChild(_box);

		Visible = false;
	}

	/// <summary>Показывает оверлей с карточками перков.</summary>
	public void Show(int level, PerkOffer[] cards)
	{
		_level = level;
		_cards = cards;
		RebuildCards();
		Visible = true;
		Recenter();
	}

	/// <summary>Скрывает оверлей.</summary>
	public void Hide()
	{
		Visible = false;
	}

	/// <summary>Перецентрирует блок, если оверлей виден (вызывается при ресайзе).</summary>
	public void RecenterIfVisible()
	{
		if (Visible)
		{
			Recenter();
		}
	}

	/// <summary>Адаптивный масштаб UI (тот же принцип, что в HUD).</summary>
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

	private void RebuildCards()
	{
		if (_box == null)
		{
			return;
		}
		foreach (Node child in _box.GetChildren())
		{
			child.QueueFree();
		}
		if (_cards == null)
		{
			return;
		}

		float ui = UiScale;

		_box.AddChild(MakeLabel($"Новый перк!  Уровень {_level}", (int)Mathf.Round(44f * ui), new Color("ffd45e")));
		_box.AddChild(MakeLabel("Выбери одно улучшение", (int)Mathf.Round(22f * ui), new Color(1f, 1f, 1f, 0.92f)));

		foreach (PerkOffer card in _cards)
		{
			PerkId id = card.Id;
			string text = $"{card.Name}\n“{card.Tagline}”\nУр. {card.CurrentLevel}/{card.MaxLevel} → {card.CurrentLevel + 1}\n{card.NextLevelEffect}";
			Button button = MakeCardButton(text, card.Accent, ui);
			button.Pressed += () => PerkPicked?.Invoke(id);
			_box.AddChild(button);
		}

		_box.AddChild(MakeLabel("Слоты перков ограничены — выбирай с умом!", (int)Mathf.Round(18f * ui), new Color(1f, 1f, 1f, 0.75f)));
	}

	private Button MakeCardButton(string text, Color accent, float ui)
	{
		var button = new Button
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.Word,
			CustomMinimumSize = new Vector2(430f * ui, 156f * ui),
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		button.AddThemeFontSizeOverride("font_size", (int)Mathf.Round(18f * ui));
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

	private static Label MakeLabel(string text, int fontSize, Color color)
	{
		var label = new Label
		{
			Text = text
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private void Recenter()
	{
		if (_box == null)
		{
			return;
		}
		Vector2 vp = GetViewport().GetVisibleRect().Size;
		_box.ResetSize();
		_box.Position = new Vector2(vp.X * 0.5f - _box.Size.X * 0.5f, vp.Y * 0.5f - _box.Size.Y * 0.5f);
	}
}