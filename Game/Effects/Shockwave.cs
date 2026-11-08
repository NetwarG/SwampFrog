using Godot;

namespace SwampFrog;

/// <summary>
/// Ударная волна: расширяющееся кольцо с яркой кромкой и гаснущим диском.
/// Используется взрывом фруктов, золотой поимкой и возрождением.
/// </summary>
public partial class Shockwave : Node2D
{
	/// <summary>Радиус, до которого расходится волна.</summary>
	public float MaxRadius { get; set; } = 120f;

	/// <summary>Сколько секунд живёт волна.</summary>
	public float Duration { get; set; } = 0.35f;

	/// <summary>Цвет волны.</summary>
	public Color Color { get; set; } = EffectStyle.Spark;

	/// <summary>Толщина кромки в начале.</summary>
	public float Width { get; set; } = 6f;

	/// <summary>Рисовать ли гаснущий диск внутри кольца.</summary>
	public bool Fill { get; set; } = true;

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
		// Квадратный корень: волна вылетает быстро и плавно тормозит.
		float radius = MaxRadius * Mathf.Sqrt(t);
		if (Fill)
			DrawCircle(Vector2.Zero, radius, new Color(Color, 0.16f * alpha));
		EffectStyle.Ring(this, Vector2.Zero, radius, Color, Width * (1f - 0.45f * t), alpha);
	}
}
