using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Меню паузы: квадратная кнопка с тремя точками сверху вниз в правом верхнем углу поля,
/// утопленная в правый край на четверть. По нажатию кнопка скрывается, а из-за правого края
/// выезжает панель с круглой кнопкой паузы (см. <see cref="PausePanel"/>).
/// </summary>
public partial class PauseMenu : Control
{
	/// <summary>Отступ кнопки от верхнего края поля: как у надписей счёта.</summary>
	private const float MarginY = 16f;

	private PauseDotsButton? _button;
	private PausePanel? _panel;
	private bool _available;
	private bool _open;

	/// <summary>Игрок нажал круглую кнопку паузы.</summary>
	public event Action? PausePressed;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_button = new PauseDotsButton { ZIndex = 30 };
		_button.Pressed += OpenPanel;
		AddChild(_button);

		_panel = new PausePanel { ZIndex = 30 };
		_panel.PausePressed += () => PausePressed?.Invoke();
		_panel.BackgroundPressed += Collapse;
		AddChild(_panel);

		Resized += UpdatePositions;
		UpdatePositions();
		UpdateButtonVisibility();
	}

	/// <summary>Доступно ли меню: показывается только в идущей партии, оверлеи его вытесняют.</summary>
	public void SetAvailable(bool available)
	{
		_available = available;
		if (!available) Collapse();
		UpdateButtonVisibility();
	}

	/// <summary>Сворачивает панель: вызывается при касании поля и смене экрана.</summary>
	public void Collapse()
	{
		if (!_open) return;
		_open = false;
		_panel?.SlideOut(RightEdge);
		UpdateButtonVisibility();
	}

	private float RightEdge => Size.X;

	private void OpenPanel()
	{
		if (!_available || _open) return;
		_open = true;
		_panel?.SlideIn(RightEdge);
		UpdateButtonVisibility();
	}

	/// <summary>Кнопка паузы и панель занимают правый верхний угол поля.</summary>
	private void UpdatePositions()
	{
		if (_button == null || _panel == null) return;
		_button.ResetSize();
		Vector2 buttonSize = _button.Size;
		// Кнопка утоплена в правый край на четверть: видимая часть прижата к кромке поля.
		_button.Position = new Vector2(RightEdge + PauseDotsButton.EdgeSink - buttonSize.X, MarginY);

		// Панель выезжает из-за края, поэтому в закрытом состоянии прячется за ним,
		// а в открытом выравнивается по верхнему краю кнопки.
		Vector2 panelSize = _panel.Size;
		_panel.Position = new Vector2(_open ? RightEdge - panelSize.X : RightEdge, MarginY);
	}

	private void UpdateButtonVisibility()
	{
		if (_button != null) _button.Visible = _available && !_open;
	}
}
