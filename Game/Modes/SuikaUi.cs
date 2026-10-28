using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Интерфейс мини-игры: подписи, кнопки и окна поверх кувшина.
/// Часть класса SuikaGame: см. SuikaGame.cs (раунд и ввод) и SuikaPile.cs (фрукты в кувшине).
/// </summary>
public partial class SuikaGame
{
	private void BuildUi()
	{
		var root = new Control { Name = "SuikaUI", MouseFilter = Control.MouseFilterEnum.Ignore };
		_uiRoot = root;
		if (Game?.UiHost != null) Game.UiHost.AddChild(root);
		else
		{
			var layer = new CanvasLayer { Layer = 20 };
			AddChild(layer);
			layer.AddChild(root);
		}
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		root.Resized += UpdateVisuals;

		_scoreLabel = MakeLabel("Счёт  0", 26, new Color("ffffff"));
		_scoreLabel.Position = new Vector2(18f, 16f);
		root.AddChild(_scoreLabel);
		_bestLabel = MakeLabel($"Рекорд  {_bestScore}", 20, new Color(1f, 1f, 1f, 0.62f));
		_bestLabel.Position = new Vector2(20f, 50f);
		root.AddChild(_bestLabel);

		_stockLabel = MakeLabel("Фрукты: 0", 20, new Color("ffd45e"));
		_stockLabel.Position = new Vector2(20f, 78f);
		root.AddChild(_stockLabel);

		_nextLabel = MakeLabel("Следующий", 22, new Color("b4e863"));
		_nextLabel.Position = new Vector2(0f, 15f);
		_nextLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_nextLabel.AnchorLeft = 1f;
		_nextLabel.AnchorRight = 1f;
		_nextLabel.OffsetLeft = -162f;
		_nextLabel.OffsetRight = -18f;
		_nextLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(_nextLabel);

		Button back = MakeButton("В меню", 24, new Color("ffd45e"), new Vector2(138f, 80f));
		back.Position = new Vector2(16f, 112f);
		back.Pressed += () => ExitRequested?.Invoke();
		root.AddChild(back);

		_hintLabel = MakeLabel("Отпусти, чтобы бросить", 18, new Color(1f, 1f, 1f, 0.78f));
		_hintLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_hintLabel.AnchorTop = 1f;
		_hintLabel.AnchorBottom = 1f;
		_hintLabel.OffsetTop = -50f;
		_hintLabel.OffsetBottom = -18f;
		_hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(_hintLabel);

		BuildStockDialog(root);
		BuildGameOver(root);
	}

	private void BuildGameOver(Control parent)
	{
		_gameOverRoot = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
		_gameOverRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		parent.AddChild(_gameOverRoot);
		var shade = new ColorRect { Color = new Color(0.02f, 0.08f, 0.1f, 0.82f), MouseFilter = Control.MouseFilterEnum.Ignore };
		shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_gameOverRoot.AddChild(shade);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 14);
		ModalContent.Mount(_gameOverRoot, box);
		_gameOverTitle = MakeLabel("Кувшин полон", 42, new Color("ffd45e"));
		box.AddChild(_gameOverTitle);
		_gameOverScore = MakeLabel("Счёт: 0", 28, new Color("ffffff"));
		box.AddChild(_gameOverScore);
		_retryButton = MakeButton("Играть ещё", 22, new Color("b4e863"), new Vector2(300f, 54f));
		_retryButton!.Pressed += Restart;
		box.AddChild(_retryButton!);
		Button menu = MakeButton("В меню", 22, new Color("ffd45e"), new Vector2(300f, 54f));
		menu.Pressed += () => ExitRequested?.Invoke();
		box.AddChild(menu);
	}

	/// <summary>Строит окно со списком собранных фруктов и кнопкой старта раунда.</summary>
	private void BuildStockDialog(Control parent)
	{
		_stockRoot = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
		_stockRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		parent.AddChild(_stockRoot);

		var shade = new ColorRect { Color = new Color(0.02f, 0.08f, 0.1f, 0.82f), MouseFilter = Control.MouseFilterEnum.Ignore };
		shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_stockRoot.AddChild(shade);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 12);
		ModalContent.Mount(_stockRoot, box);

		Label title = MakeLabel("Собранные фрукты", 34, new Color("ffd45e"));
		box.AddChild(title);

		_stockListBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_stockListBox.AddThemeConstantOverride("separation", 6);
		box.AddChild(_stockListBox);

		foreach (FruitSpec spec in FruitCatalog.All)
		{
			Label row = MakeLabel($"{spec.Name} — 0", 24, new Color("ffffff"));
			row.Visible = false;
			_stockListBox!.AddChild(row);
			_stockRows.Add(row);
		}

		_stockTotal = MakeLabel("Всего: 0", 20, new Color("b4e863"));
		box.AddChild(_stockTotal);

		Button start = MakeButton("Начать игру", 22, new Color("b4e863"), new Vector2(300f, 54f));
		start.Pressed += StartFromStockDialog;
		box.AddChild(start);
	}

	/// <summary>Обновляет и показывает окно со списком фруктов перед началом раунда.</summary>
	private void ShowStockDialog()
	{
		if (_stockRoot == null) return;
		int total = 0;
		int rowIndex = 0;
		foreach (FruitSpec spec in FruitCatalog.All)
		{
			Label row = _stockRows[rowIndex++];
			int count = Game?.FruitStock(spec.Kind) ?? 0;
			row.Text = $"{spec.Name} — {count}";
			row.Visible = count > 0;
			total += count;
		}
		if (_stockTotal != null) _stockTotal.Text = $"Всего: {total}";
		_stockRoot.Visible = true;
	}

	/// <summary>Игрок подтвердил список — скрываем окно и начинаем раунд.</summary>
	private void StartFromStockDialog()
	{
		if (_stockRoot != null) _stockRoot.Visible = false;
		_active = true;
		// Небольшая пауза, чтобы клик по кнопке случайно не бросил фрукт.
		_dropCooldown = Mathf.Max(_dropCooldown, 0.35f);
	}

	private Label MakeLabel(string text, int size, Color color)
	{
		var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		return label;
	}

	private Button MakeButton(string text, int size, Color accent, Vector2 minSize)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(minSize.X, Mathf.Max(80f, minSize.Y)),
			MouseFilter = Control.MouseFilterEnum.Pass,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		button.AddThemeFontSizeOverride("font_size", size);
		button.AddThemeColorOverride("font_color", new Color("173c34"));
		button.AddThemeColorOverride("font_pressed_color", new Color("ffffff"));
		button.AddThemeStyleboxOverride("normal", ButtonStyle(new Color(accent, 0.9f), accent));
		button.AddThemeStyleboxOverride("hover", ButtonStyle(accent, new Color("ffffff")));
		button.AddThemeStyleboxOverride("pressed", ButtonStyle(new Color(accent, 0.72f), new Color("ffffff")));
		button.AddThemeStyleboxOverride("focus", ButtonStyle(new Color(accent, 0.9f), accent));
		return button;
	}

	private static StyleBoxFlat ButtonStyle(Color background, Color border)
	{
		var style = new StyleBoxFlat { BgColor = background, BorderColor = new Color(border, 0.72f) };
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(12);
		style.ShadowColor = new Color(0f, 0f, 0f, 0.18f);
		style.ShadowSize = 4;
		return style;
	}

	/// <summary>Обновляет индикатор остатка фруктов в запасе.</summary>
	private void UpdateStockLabel()
	{
		if (_stockLabel == null) return;
		int total = Game?.TotalFruitStock() ?? 0;
		_stockLabel.Text = $"Фрукты: {total}";
	}
}
