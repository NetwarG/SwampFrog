using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Карточка перка с выбором по удержанию: чтобы выбрать, нужно зажать карточку
/// и не отпускать в течение <see cref="HoldFor"/> секунд. Случайный короткий клик
/// выбор не срабатывает. Прогресс удержания показывает полоса внизу карточки.
/// </summary>
public partial class HoldCardButton : Button
{
	/// <summary>Сколько секунд нужно удерживать карточку, чтобы выбрать перк.</summary>
	public float HoldFor = 1f;

	/// <summary>Коэффициент масштаба UI (от него зависят размеры полосы прогресса).</summary>
	public float UiScale { get; set; } = 1f;

	/// <summary>Цвет полосы прогресса удержания.</summary>
	public Color BarColor { get; set; } = new Color("ffd45e");

	/// <summary>Прогресс удержания 0..1 (для отрисовки полосы).</summary>
	public float HoldProgress => HoldFor <= 0f ? 1f : Mathf.Clamp(_holdTime / HoldFor, 0f, 1f);

	/// <summary>Удержание завершено — карточку можно считать выбранной.</summary>
	public event Action? Held;

	private ColorRect? _holdBar;
	private float _holdTime;
	private bool _holding;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_holdBar = new ColorRect
		{
			Color = BarColor,
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_holdBar);

		MouseExited += CancelHold;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				_holding = true;
				_holdTime = 0f;
			}
			else if (_holding)
				CancelHold();
		}
		else if (@event is InputEventScreenDrag drag && _holding)
		{
			// Палец съехал за пределы карточки — удержание отменяем.
			if (!GetGlobalRect().HasPoint(drag.Position))
				CancelHold();
		}

		base._GuiInput(@event);
	}

	public override void _Process(double delta)
	{
		if (!_holding)
			return;

		_holdTime += (float)delta;
		UpdateHoldBar(HoldProgress);
		if (_holdTime >= HoldFor)
		{
			CancelHold();
			Held?.Invoke();
		}
	}

	private void CancelHold()
	{
		_holding = false;
		_holdTime = 0f;
		UpdateHoldBar(0f);
	}

	/// <summary>Показывает полосу прогресса удержания внизу карточки.</summary>
	private void UpdateHoldBar(float progress)
	{
		if (_holdBar == null)
			return;

		float ui = Mathf.Max(1f, UiScale);
		float inset = 10f * ui;
		float thickness = 6f * ui;
		float width = Mathf.Max(0f, (Size.X - inset * 2f) * progress);
		_holdBar.Position = new Vector2(inset, Size.Y - thickness - 8f * ui);
		_holdBar.Size = new Vector2(width, thickness);
	}
}