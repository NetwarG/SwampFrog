using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Выдвижное меню паузы: маленький полукруглый язычок вплотную к правому краю экрана
/// по центру высоты. По нажатию язычок сменяется выезжающей панелью, внутри которой
/// появляется круглая кнопка паузы (см. <see cref="PausePanel"/>).
/// </summary>
public partial class PauseMenu : Control
{
	private MenuArrowButton? _tab;
	private PausePanel? _panel;
	private bool _available;
	private bool _open;

	/// <summary>Игрок нажал круглую кнопку паузы.</summary>
	public event Action? PausePressed;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_tab = new MenuArrowButton { ZIndex = 30 };
		_tab.Pressed += OpenPanel;
		AddChild(_tab);

		_panel = new PausePanel { ZIndex = 30 };
		_panel.PausePressed += () => PausePressed?.Invoke();
		_panel.BackgroundPressed += Collapse;
		AddChild(_panel);

		Resized += UpdatePositions;
		UpdatePositions();
		UpdateTabVisibility();
	}

	/// <summary>Доступно ли меню: показывается только в идущей партии, оверлеи его вытесняют.</summary>
	public void SetAvailable(bool available)
	{
		_available = available;
		if (!available) Collapse();
		UpdateTabVisibility();
	}

	/// <summary>Сворачивает панель: вызывается при касании поля и смене экрана.</summary>
	public void Collapse()
	{
		if (!_open) return;
		_open = false;
		_panel?.SlideOut(RightEdge);
		UpdateTabVisibility();
	}

	private float RightEdge => Size.X;

	private void OpenPanel()
	{
		if (!_available || _open) return;
		_open = true;
		_panel?.SlideIn(RightEdge);
		UpdateTabVisibility();
	}

	/// <summary>Язычок и панель занимают одно место по центру правого края экрана.</summary>
	private void UpdatePositions()
	{
		if (_tab == null || _panel == null) return;
		_tab.ResetSize();
		Vector2 tabSize = _tab.Size;
		_tab.Position = new Vector2(RightEdge - tabSize.X, Size.Y * 0.5f - tabSize.Y * 0.5f);

		// Панель выезжает из-за края, поэтому в закрытом состоянии прячется за ним.
		Vector2 panelSize = _panel.Size;
		_panel.Position = new Vector2(
			_open ? RightEdge - panelSize.X : RightEdge,
			Size.Y * 0.5f - panelSize.Y * 0.5f);
	}

	private void UpdateTabVisibility()
	{
		if (_tab != null) _tab.Visible = _available && !_open;
	}
}
