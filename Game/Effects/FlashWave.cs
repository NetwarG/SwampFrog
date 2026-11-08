using Godot;

namespace SwampFrog;

/// <summary>
/// Волна по всему полю: короткая «изморозь» и расходящееся кольцо от лягушки.
/// Показывает момент заморозки «Взгляда василиска» и полной остановки «Зоны замедления».
/// </summary>
public partial class FlashWave : Node2D
{
	/// <summary>Размер поля: заливка накрывает его целиком.</summary>
	public Vector2 FieldSize { get; set; } = Playfield.Size;

	/// <summary>Центр кольца в локальных координатах поля.</summary>
	public Vector2 OriginPoint { get; set; }

	/// <summary>Радиус, до которого расходится кольцо.</summary>
	public float MaxRadius { get; set; } = 900f;

	/// <summary>Сколько секунд длится волна.</summary>
	public float Duration { get; set; } = 0.7f;

	/// <summary>Цвет волны и заливки.</summary>
	public Color Color { get; set; } = EffectStyle.Frost;

	/// <summary>Плотность заливки поля в начале.</summary>
	public float Tint { get; set; } = 0.12f;

	private float _age;

	public override void _Process(double delta)
	{
		_age += (float)delta;
		QueueRedraw();
		if (_age >= Duration)
			QueueFree();
	}

	public override void _Draw()
	{
		float t = Mathf.Clamp(_age / Duration, 0f, 1f);
		float alpha = 1f - t;
		DrawRect(new Rect2(Vector2.Zero, FieldSize), new Color(Color, Tint * alpha));
		EffectStyle.Ring(this, OriginPoint, MaxRadius * Mathf.Sqrt(t), Color, 9f * (1f - 0.4f * t), alpha * 0.85f);
	}
}
