using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Карточка перка с выбором по удержанию: чтобы выбрать, нужно зажать карточку
/// и не отпускать в течение <see cref="HoldFor"/> секунд. Случайный короткий клик
/// выбор не срабатывает. Прогресс удержания заполняет карточку слева направо
/// (см. <see cref="HoldProgressFill"/>), поэтому заливка видна и под пальцем.
/// </summary>
public partial class HoldCardButton : Button
{
	/// <summary>Сколько секунд нужно удерживать карточку, чтобы выбрать перк.</summary>
	public float HoldFor = 1f;

	/// <summary>Коэффициент масштаба UI, от которого зависит размер заливки.</summary>
	public float UiScale { get; set; } = 1f;

	/// <summary>Цвет заливки карточки при удержании.</summary>
	public Color FillColor { get; set; } = new Color("ffd45e");

	/// <summary>Прогресс удержания от 0 до 1.</summary>
	public float HoldProgress => HoldFor <= 0f ? 1f : Mathf.Clamp(_holdTime / HoldFor, 0f, 1f);

	/// <summary>Удержание завершено, карточка выбрана.</summary>
	public event Action? Held;

	private HoldProgressFill? _holdFill;
	private float _holdTime;
	private bool _holding;
	private Vector2 _holdPointerPosition;
	private int _holdPointerIndex = -1;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_holdFill = new HoldProgressFill { UiScale = UiScale, FillColor = FillColor };
		AddChild(_holdFill);
		_holdFill.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed && !touch.Canceled)
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

	public override void _Input(InputEvent input)
	{
		if (!_holding) return;
		if (input is InputEventScreenTouch touch && touch.Index == _holdPointerIndex && (!touch.Pressed || touch.Canceled))
			CancelHold();
		else if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } && _holdPointerIndex == -1)
			CancelHold();
		else if (input is InputEventScreenDrag drag && drag.Index == _holdPointerIndex)
			UpdateHoldPointer(GetGlobalTransformWithCanvas().AffineInverse() * drag.Position, drag.Index);
		else if (input is InputEventMouseMotion motion && _holdPointerIndex == -1)
			UpdateHoldPointer(GetGlobalTransformWithCanvas().AffineInverse() * motion.Position, -1);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut || what == NotificationScrollBegin)
			CancelHold();
	}

	public override void _Process(double delta)
	{
		if (!_holding)
		{
			return;
		}

		if (!IsVisibleInTree() || !new Rect2(Vector2.Zero, Size).HasPoint(_holdPointerPosition))
		{
			CancelHold();
			return;
		}

		_holdTime += (float)delta;
		UpdateHoldFill(HoldProgress);
		if (_holdTime >= HoldFor)
		{
			CancelHold();
			Held?.Invoke();
		}
	}

	private void BeginHold(Vector2 pointerPosition, int pointerIndex)
	{
		if (_holding || !IsVisibleInTree())
		{
			return;
		}

		_holding = true;
		_holdTime = 0f;
		_holdPointerPosition = pointerPosition;
		_holdPointerIndex = pointerIndex;
		UpdateHoldFill(0f);
	}

	private void UpdateHoldPointer(Vector2 pointerPosition, int pointerIndex)
	{
		if (!_holding || pointerIndex != _holdPointerIndex)
		{
			return;
		}

		_holdPointerPosition = pointerPosition;
		UpdateHoldFill(HoldProgress);
	}

	private void CancelHold()
	{
		_holding = false;
		_holdTime = 0f;
		_holdPointerIndex = -1;
		UpdateHoldFill(0f);
	}

	private void UpdateHoldFill(float progress)
	{
		if (_holdFill == null)
		{
			return;
		}

		_holdFill.Progress = progress;
		_holdFill.FillColor = FillColor;
		_holdFill.UiScale = UiScale;
		_holdFill.Visible = _holding;
		_holdFill.QueueRedraw();
	}
}
