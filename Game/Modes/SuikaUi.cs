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

		_scoreLabel = MakeLabel(Loc.T(L.SuikaScore, 0), 26, new Color("ffffff"));
		_scoreLabel.Position = new Vector2(18f, 16f);
		root.AddChild(_scoreLabel);
		_bestLabel = MakeLabel(Loc.T(L.SuikaBest, _bestScore), 20, new Color(1f, 1f, 1f, 0.62f));
		_bestLabel.Position = new Vector2(20f, 50f);
		root.AddChild(_bestLabel);

		_stockLabel = MakeLabel(Loc.T(L.SuikaStock, 0), 20, new Color("ffd45e"));
		_stockLabel.Position = new Vector2(20f, 78f);
		root.AddChild(_stockLabel);

		_nextLabel = MakeLabel(Loc.T(L.SuikaNext), 22, new Color("b4e863"));
		_nextLabel.Position = new Vector2(0f, 15f);
		_nextLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_nextLabel.AnchorLeft = 1f;
		_nextLabel.AnchorRight = 1f;
		_nextLabel.OffsetLeft = -162f;
		_nextLabel.OffsetRight = -18f;
		_nextLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(_nextLabel);

		_backButton = MakeButton(Loc.T(L.ToMenu), 24, new Color("ffd45e"), new Vector2(138f, 80f));
		_backButton.Position = new Vector2(16f, 112f);
		_backButton.Pressed += () => ExitRequested?.Invoke();
		root.AddChild(_backButton);

		_hintLabel = MakeLabel(Loc.T(L.SuikaHintDrop), 18, new Color(1f, 1f, 1f, 0.78f));
		_hintLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_hintLabel.AnchorTop = 1f;
		_hintLabel.AnchorBottom = 1f;
		_hintLabel.OffsetTop = -50f;
		_hintLabel.OffsetBottom = -18f;
		_hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(_hintLabel);

		BuildStockDialog(root);
		BuildGameOver(root);
		BuildHelp(root);
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
		_gameOverTitle = MakeLabel(Loc.T(L.SuikaOverflow), 42, new Color("ffd45e"));
		box.AddChild(_gameOverTitle);
		_gameOverScore = MakeLabel(Loc.T(L.SuikaGameOverScore, 0), 28, new Color("ffffff"));
		box.AddChild(_gameOverScore);
		_retryButton = MakeButton(Loc.T(L.SuikaRetry), 22, new Color("b4e863"), new Vector2(300f, 54f));
		_retryButton!.Pressed += Restart;
		box.AddChild(_retryButton!);
		_gameOverMenuButton = MakeButton(Loc.T(L.ToMenu), 22, new Color("ffd45e"), new Vector2(300f, 54f));
		_gameOverMenuButton.Pressed += () => ExitRequested?.Invoke();
		box.AddChild(_gameOverMenuButton);
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

		_stockTitle = MakeLabel(Loc.T(L.SuikaStockTitle), 34, new Color("ffd45e"));
		box.AddChild(_stockTitle);

		_stockListBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_stockListBox.AddThemeConstantOverride("separation", 6);
		box.AddChild(_stockListBox);

		foreach (FruitSpec spec in FruitCatalog.All)
		{
			Label row = MakeLabel(Loc.T(L.SuikaStockRow, spec.Name, 0), 24, new Color("ffffff"));
			row.Visible = false;
			_stockListBox!.AddChild(row);
			_stockRows.Add(row);
		}

		_stockTotal = MakeLabel(Loc.T(L.SuikaStockTotal, 0), 20, new Color("b4e863"));
		box.AddChild(_stockTotal);

		_startButton = MakeButton(Loc.T(L.SuikaStart), 22, new Color("b4e863"), new Vector2(300f, 54f));
		_startButton.Pressed += StartFromStockDialog;
		box.AddChild(_startButton);
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
			row.Text = Loc.T(L.SuikaStockRow, spec.Name, count);
			row.Visible = count > 0;
			total += count;
		}
		if (_stockTotal != null) _stockTotal.Text = Loc.T(L.SuikaStockTotal, total);
		_stockRoot.Visible = true;
	}

	/// <summary>Игрок подтвердил список — скрываем окно и начинаем раунд.</summary>
	private void StartFromStockDialog()
	{
		if (_stockRoot != null) _stockRoot.Visible = false;
		_active = true;
		// Небольшая пауза, чтобы клик по кнопке случайно не бросил фрукт.
		_dropCooldown = Mathf.Max(_dropCooldown, 0.35f);
		UpdateHelpButton();
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

	/// <summary>Кнопки мини-игры — общий стиль игры (см. <see cref="UiStyle.MakeButton"/>).</summary>
	private Button MakeButton(string text, int size, Color accent, Vector2 minSize)
	{
		// Размер шрифта задаёт масштаб стиля: 24 — базовая подпись кнопки.
		float ui = size / 24f;
		return UiStyle.MakeButton(text, ui, accent, new Vector2(minSize.X, Mathf.Max(80f, minSize.Y)));
	}

	/// <summary>Обновляет индикатор остатка фруктов в запасе.</summary>
	private void UpdateStockLabel()
	{
		if (_stockLabel == null) return;
		int total = Game?.TotalFruitStock() ?? 0;
		_stockLabel.Text = Loc.T(L.SuikaStock, total);
	}

	/// <summary>
	/// Перечитывает статические подписи мини-игры на текущем языке: вызывается при открытии
	/// раунда и после смены языка в меню (кнопка языка только в главном меню).
	/// </summary>
	private void LocalizeUi()
	{
		if (_scoreLabel != null) _scoreLabel.Text = Loc.T(L.SuikaScore, _score);
		if (_bestLabel != null) _bestLabel.Text = Loc.T(L.SuikaBest, _bestScore);
		if (_nextLabel != null) _nextLabel.Text = Loc.T(L.SuikaNext);
		if (_hintLabel != null) _hintLabel.Text = Loc.T(L.SuikaHintDrop);
		if (_backButton != null) _backButton.Text = Loc.T(L.ToMenu);
		if (_gameOverTitle != null) _gameOverTitle.Text = _stockEmpty ? Loc.T(L.SuikaNoFruits) : Loc.T(L.SuikaOverflow);
		if (_gameOverScore != null) _gameOverScore.Text = Loc.T(L.SuikaGameOverScore, _score);
		if (_retryButton != null) _retryButton.Text = Loc.T(L.SuikaRetry);
		if (_gameOverMenuButton != null) _gameOverMenuButton.Text = Loc.T(L.ToMenu);
		if (_stockTitle != null) _stockTitle.Text = Loc.T(L.SuikaStockTitle);
		if (_startButton != null) _startButton.Text = Loc.T(L.SuikaStart);
		UpdateStockLabel();
		LocalizeHelp();
	}
}
