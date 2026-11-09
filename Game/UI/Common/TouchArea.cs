using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Область интерфейса поверх поля: реагирует на нажатие один раз за жест и поглощает
/// события мыши и касания. На ПК с включённым `pointing/emulate_touch_from_mouse`
/// каждое нажатие приходит дважды (мышь и эмулированное касание), а непоглощённые
/// события уходят в игровой ввод и мешают интерфейсу.
/// </summary>
public abstract partial class TouchArea : Control
{
	private ulong _lastPressFrame = ulong.MaxValue;

	/// <summary>Нажатие принято.</summary>
	public event Action? Pressed;

	protected TouchArea()
	{
		MouseFilter = MouseFilterEnum.Stop;
		MouseDefaultCursorShape = CursorShape.PointingHand;
	}

	public override void _GuiInput(InputEvent @event)
	{
		bool press = @event is InputEventScreenTouch { Pressed: true }
			or InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left };
		bool release = @event is InputEventScreenTouch { Pressed: false }
			or InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left };
		if (press || release) AcceptEvent();
		if (!press) return;
		// Дубль того же нажатия (эмулированное касание) не должен переключать состояние.
		if (@event.Device == InputEvent.DeviceIdEmulation) return;
		ulong frame = Engine.GetProcessFrames();
		if (frame == _lastPressFrame) return;
		_lastPressFrame = frame;
		Pressed?.Invoke();
	}
}
