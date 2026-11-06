using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Ползунок громкости: скруглённый трек и круглый бегунок. Нажатие или перетаскивание
/// задают громкость от 0 до 1. Собственный Control: трек и бегунок рисуются кодом.
/// </summary>
public partial class VolumeSlider : TouchArea
{
	/// <summary>Размер ползунка в логических единицах.</summary>
	public static readonly Vector2 SliderSize = new(150f, 40f);

	private const float KnobRadius = 15f;
	private const float TrackHeight = 10f;

	private static readonly Color Track = new(0.02f, 0.12f, 0.09f, 0.6f);
	private static readonly Color TrackOutline = new("9ee4c3", 0.35f);
	private static readonly Color Fill = new("b4e863");
	private static readonly Color Knob = new(0.93f, 1f, 0.97f);
	private static readonly Color KnobOutline = new("173c34");
	private static readonly Color NoOutline = new(0f, 0f, 0f, 0f);

	private float _value = 1f;
	private bool _dragging;
	private bool _muted;

	/// <summary>Громкость изменилась во время нажатия или перетаскивания.</summary>
	public event Action<float>? ValueChanged;

	/// <summary>Игрок отпустил ползунок: значение можно сохранять.</summary>
	public event Action<float>? ValueCommitted;

	public override void _Ready()
	{
		CustomMinimumSize = SliderSize;
		Size = SliderSize;
	}

	/// <summary>Задаёт значение извне; во время перетаскивания игнорируется, чтобы бегунок не дрожал.</summary>
	public void SetValue(float value)
	{
		if (_dragging)
			return;
		_value = Mathf.Clamp(value, 0f, 1f);
		QueueRedraw();
	}

	/// <summary>Приглушает бегунок, когда звук полностью отключён.</summary>
	public void SetMuted(bool muted)
	{
		if (_muted == muted)
			return;
		_muted = muted;
		QueueRedraw();
	}

	public override void _GuiInput(InputEvent @event)
	{
		base._GuiInput(@event);
		bool press = @event is InputEventScreenTouch { Pressed: true }
			or InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left };
		bool release = @event is InputEventScreenTouch { Pressed: false }
			or InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left };
		if (press)
			_dragging = true;
		if (press || IsDrag(@event))
			SetFromEvent(@event);
		if (release && _dragging)
		{
			_dragging = false;
			ValueCommitted?.Invoke(_value);
		}
	}

	private static bool IsDrag(InputEvent @event) =>
		@event is InputEventScreenDrag
		|| (@event is InputEventMouseMotion motion && motion.ButtonMask.HasFlag(MouseButtonMask.Left));

	private void SetFromEvent(InputEvent @event)
	{
		float x = @event switch
		{
			InputEventScreenTouch touch => touch.Position.X,
			InputEventScreenDrag drag => drag.Position.X,
			InputEventMouseButton button => button.Position.X,
			InputEventMouseMotion motion => motion.Position.X,
			_ => float.NaN
		};
		if (float.IsNaN(x))
			return;
		float usable = Mathf.Max(1f, Size.X - KnobRadius * 2f);
		float value = Mathf.Clamp((x - KnobRadius) / usable, 0f, 1f);
		if (Mathf.IsEqualApprox(value, _value))
			return;
		_value = value;
		QueueRedraw();
		ValueChanged?.Invoke(value);
	}

	public override void _Draw()
	{
		float cy = Size.Y * 0.5f;
		float trackLeft = KnobRadius;
		float trackRight = Size.X - KnobRadius;
		UiStyle.DrawRoundedPanel(this, new Rect2(trackLeft, cy - TrackHeight * 0.5f, trackRight - trackLeft, TrackHeight),
			TrackHeight * 0.5f, Track, TrackOutline, 1.5f);

		float knobX = trackLeft + _value * (trackRight - trackLeft);
		float fillWidth = knobX - trackLeft;
		if (fillWidth > 1f)
		{
			Color fill = _muted ? new Color(Fill, 0.35f) : Fill;
			UiStyle.DrawRoundedPanel(this, new Rect2(trackLeft, cy - TrackHeight * 0.5f, fillWidth, TrackHeight),
				TrackHeight * 0.5f, fill, NoOutline, 0f);
		}

		Color knob = _muted ? new Color(Knob, 0.5f) : Knob;
		DrawCircle(new Vector2(knobX, cy), KnobRadius, knob);
		DrawCircle(new Vector2(knobX, cy), KnobRadius, KnobOutline, false, 2f);
	}
}
