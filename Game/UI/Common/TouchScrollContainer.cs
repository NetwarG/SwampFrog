using Godot;

namespace SwampFrog;

/// <summary>
/// ScrollContainer со свайпом пальцем: в проекте отключено превращение касаний в мышь
/// (`emulate_mouse_from_touch = false`), поэтому встроенная прокрутка жестом не срабатывает.
/// Свайп двигает список напрямую, а короткое касание проходит к кнопкам карточек.
/// </summary>
public partial class TouchScrollContainer : ScrollContainer
{
	/// <summary>Сдвиг пальца, после которого жест считается прокруткой.</summary>
	private const float DragThreshold = 10f;

	private bool _tracking;
	private bool _dragging;
	private int _touchIndex = -1;
	private float _startY;
	private int _startScroll;

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				_tracking = true;
				_dragging = false;
				_touchIndex = touch.Index;
				_startY = touch.Position.Y;
				_startScroll = ScrollVertical;
			}
			else if (touch.Index == _touchIndex)
			{
				_tracking = false;
				_dragging = false;
				_touchIndex = -1;
			}
			return;
		}
		if (@event is not InputEventScreenDrag drag || !_tracking || drag.Index != _touchIndex)
			return;
		float moved = drag.Position.Y - _startY;
		if (!_dragging && Mathf.Abs(moved) < DragThreshold)
			return;
		_dragging = true;
		ScrollVertical = Mathf.RoundToInt(_startScroll - moved);
		AcceptEvent();
	}
}
