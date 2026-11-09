using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Вертикальный ползунок громкости: скруглённый трек и круглый бегунок.
/// Нажатие или перетаскивание задают громкость от 0 (снизу) до 1 (сверху).
/// Собственный Control: трек, заливка и бегунок рисуются кодом.
/// </summary>
public partial class VolumeSlider : TouchArea
{
	/// <summary>Размер ползунка в логических единицах: узкий и высокий.</summary>
	public static readonly Vector2 SliderSize = new(40f, 170f);

	private const float KnobRadius = 15f;
	private const float TrackWidth = 10f;

	/// <summary>Толщина коричневого контура трека и бегунка.</summary>
	private const float OutlineWidth = 2.5f;

	private static readonly Color TrackLight = new(0.36f, 0.58f, 0.50f, 0.55f);
	private static readonly Color TrackDark = new(0.18f, 0.38f, 0.32f, 0.55f);
	private static readonly Color Knob = new("f4fff0");

	private float _value = 1f;
	private bool _dragging;
	private bool _muted;
	private Color _accent = new("b4e863");

	/// <summary>Цвет заливки: отличает ползунок музыки от ползунка эффектов.</summary>
	public Color Accent
	{
		get => _accent;
		set
		{
			_accent = value;
			QueueRedraw();
		}
	}

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
		float y = @event switch
		{
			InputEventScreenTouch touch => touch.Position.Y,
			InputEventScreenDrag drag => drag.Position.Y,
			InputEventMouseButton button => button.Position.Y,
			InputEventMouseMotion motion => motion.Position.Y,
			_ => float.NaN
		};
		if (float.IsNaN(y))
			return;
		float usable = Mathf.Max(1f, Size.Y - KnobRadius * 2f);
		// Верх — максимум, низ — минимум.
		float value = Mathf.Clamp(1f - (y - KnobRadius) / usable, 0f, 1f);
		if (Mathf.IsEqualApprox(value, _value))
			return;
		_value = value;
		QueueRedraw();
		ValueChanged?.Invoke(value);
	}

	/// <summary>Трек и бегунок в общем стиле кнопок: градиент и коричневый контур.</summary>
	public override void _Draw()
	{
		float cx = Size.X * 0.5f;
		float trackTop = KnobRadius;
		float trackBottom = Size.Y - KnobRadius;
		var track = new Rect2(cx - TrackWidth * 0.5f, trackTop, TrackWidth, trackBottom - trackTop);
		StickerStyle.DrawPanel(this, track, TrackWidth * 0.5f, TrackLight, TrackDark, OutlineWidth, false);

		float knobY = trackBottom - _value * (trackBottom - trackTop);
		// Заливка от бегунка вниз: видно, сколько громкости уже набрано.
		float fillHeight = trackBottom - knobY;
		if (fillHeight > 1f)
		{
			Color fill = _muted ? new Color(_accent, 0.35f) : _accent;
			StickerStyle.DrawPanel(this, new Rect2(cx - TrackWidth * 0.5f, knobY, TrackWidth, fillHeight),
				TrackWidth * 0.5f, StickerStyle.Light(fill), StickerStyle.Dark(fill), 0f, false);
		}

		Color knob = _muted ? new Color(Knob, 0.5f) : Knob;
		StickerStyle.DrawCirclePanel(this, new Vector2(cx, knobY), KnobRadius,
			StickerStyle.Light(knob), StickerStyle.Dark(knob), OutlineWidth, true);
	}
}
