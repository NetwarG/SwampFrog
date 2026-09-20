using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Карточка перка с выбором по удержанию: чтобы выбрать, нужно зажать карточку
/// и не отпускать в течение <see cref="HoldFor"/> секунд. Случайный короткий клик
/// выбор не срабатывает. Прогресс удержания показывается кольцом вокруг указателя.
/// </summary>
public partial class HoldCardButton : Button
{
	/// <summary>Сколько секунд нужно удерживать карточку, чтобы выбрать перк.</summary>
	public float HoldFor = 1f;

	/// <summary>Коэффициент масштаба UI, от которого зависит размер кольца прогресса.</summary>
	public float UiScale { get; set; } = 1f;

	/// <summary>Цвет кольца прогресса удержания.</summary>
	public Color BarColor { get; set; } = new Color("ffd45e");

	/// <summary>Прогресс удержания от 0 до 1.</summary>
	public float HoldProgress => HoldFor <= 0f ? 1f : Mathf.Clamp(_holdTime / HoldFor, 0f, 1f);

	/// <summary>Удержание завершено, карточка выбрана.</summary>
	public event Action? Held;

	private HoldProgressRing? _holdRing;
	private float _holdTime;
	private bool _holding;
	private Vector2 _holdPointerPosition;
	private int _holdPointerIndex = -1;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_holdRing = new HoldProgressRing
		{
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false
		};
		AddChild(_holdRing);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				BeginHold(touch.Position, touch.Index);
			}
			else if (_holding && _holdPointerIndex == touch.Index)
			{
				CancelHold();
			}
		}
		else if (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left)
		{
			if (mouse.Pressed)
			{
				BeginHold(mouse.Position, -1);
			}
			else if (_holding && _holdPointerIndex == -1)
			{
				CancelHold();
			}
		}

		base._GuiInput(@event);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				if (!_holding && GetGlobalRect().HasPoint(touch.Position))
				{
					BeginHold(touch.Position, touch.Index);
				}
			}
			else if (_holding && _holdPointerIndex == touch.Index)
			{
				CancelHold();
			}
			return;
		}

		if (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left)
		{
			if (mouse.Pressed)
			{
				if (!_holding && GetGlobalRect().HasPoint(mouse.Position))
				{
					BeginHold(mouse.Position, -1);
				}
			}
			else if (_holding && _holdPointerIndex == -1)
			{
				CancelHold();
			}
			return;
		}

		if (!_holding)
		{
			return;
		}

		if (_holdPointerIndex == -1)
		{
			_holdPointerPosition = GetGlobalMousePosition();
		}

		if (@event is InputEventScreenDrag drag && drag.Index == _holdPointerIndex)
		{
			UpdateHoldPointer(drag.Position, drag.Index);
		}
		else if (@event is InputEventMouseMotion motion && _holdPointerIndex == -1)
		{
			UpdateHoldPointer(motion.Position, -1);
		}
	}

	public override void _Process(double delta)
	{
		if (!_holding)
		{
			return;
		}

		if (!GetGlobalRect().HasPoint(_holdPointerPosition))
		{
			CancelHold();
			return;
		}

		_holdTime += (float)delta;
		UpdateHoldRing(HoldProgress);
		if (_holdTime >= HoldFor)
		{
			CancelHold();
			Held?.Invoke();
		}
	}

	private void BeginHold(Vector2 pointerPosition, int pointerIndex)
	{
		if (_holding)
		{
			return;
		}

		_holding = true;
		_holdTime = 0f;
		_holdPointerPosition = pointerPosition;
		_holdPointerIndex = pointerIndex;
		UpdateHoldRing(0f);
	}

	private void UpdateHoldPointer(Vector2 pointerPosition, int pointerIndex)
	{
		if (!_holding || pointerIndex != _holdPointerIndex)
		{
			return;
		}

		_holdPointerPosition = pointerPosition;
		UpdateHoldRing(HoldProgress);
	}

	private void CancelHold()
	{
		_holding = false;
		_holdTime = 0f;
		_holdPointerIndex = -1;
		UpdateHoldRing(0f);
	}

	private void UpdateHoldRing(float progress)
	{
		if (_holdRing == null)
		{
			return;
		}

		float ui = Mathf.Max(0.85f, UiScale);
		float diameter = 64f * ui;
		_holdRing.Size = new Vector2(diameter, diameter);
		_holdRing.GlobalPosition = _holdPointerPosition - new Vector2(diameter, diameter) * 0.5f;
		_holdRing.Progress = progress;
		_holdRing.RingColor = BarColor;
		_holdRing.Visible = _holding;
		_holdRing.QueueRedraw();
	}

	private partial class HoldProgressRing : Control
	{
		public float Progress { get; set; }
		public Color RingColor { get; set; } = new Color("ffd45e");

		public override void _Draw()
		{
			float width = 4f;
			Vector2 center = Size * 0.5f;
			float radius = Mathf.Max(1f, Size.X * 0.5f - width);
			float startAngle = -Mathf.Pi * 0.5f;
			float endAngle = startAngle + Mathf.Tau * Mathf.Clamp(Progress, 0f, 1f);

			DrawArc(center, radius, startAngle, startAngle + Mathf.Tau, 64, new Color(RingColor, 0.28f), width, true);
			if (Progress > 0f)
			{
				DrawArc(center, radius, startAngle, endAngle, 64, RingColor, width, true);
			}
		}
	}
}
