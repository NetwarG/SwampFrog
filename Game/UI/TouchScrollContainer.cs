using Godot;

namespace SwampFrog;

/// <summary>Прокрутка настоящим пальцем без эмуляции мыши (в Godot 4.7 она нужна стандартному ScrollContainer).</summary>
public partial class TouchScrollContainer : ScrollContainer
{
	private int _finger = -1;
	private Vector2 _origin;
	private int _scrollOrigin;
	private bool _dragging;

	public override void _GuiInput(InputEvent input)
	{
		if (input is InputEventScreenTouch touch)
		{
			if (touch.Pressed && !touch.Canceled && _finger == -1)
			{
				_finger = touch.Index;
				_origin = touch.Position;
				_scrollOrigin = ScrollVertical;
			}
			else if (touch.Index == _finger && (!touch.Pressed || touch.Canceled))
				CancelGesture();
		}
		else if (input is InputEventScreenDrag drag && drag.Index == _finger)
		{
			float distance = drag.Position.Y - _origin.Y;
			if (!_dragging && Mathf.Abs(distance) > 14f && GetVScrollBar().MaxValue > GetVScrollBar().Page)
			{
				_dragging = true;
				PropagateNotification((int)NotificationScrollBegin);
				EmitSignal(SignalName.ScrollStarted);
			}
			if (_dragging)
			{
				ScrollVertical = _scrollOrigin - Mathf.RoundToInt(distance);
				AcceptEvent();
			}
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut
			|| (what == NotificationVisibilityChanged && !IsVisibleInTree())) CancelGesture();
	}

	private void CancelGesture()
	{
		_finger = -1;
		if (!_dragging) return;
		_dragging = false;
		PropagateNotification((int)NotificationScrollEnd);
		EmitSignal(SignalName.ScrollEnded);
	}
}
