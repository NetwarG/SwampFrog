using Godot;

namespace SwampFrog;

/// <summary>
/// Разлёт искр от точки: осколки взрыва, золотые брызги, распад мусора.
/// Направления задаются детерминированно, поэтому эффект не зависит от случайности.
/// </summary>
public partial class Sparks : Node2D
{
	/// <summary>Сколько искр разлетается.</summary>
	public int Count { get; set; } = 10;

	/// <summary>Скорость разлёта в мировых единицах в секунду.</summary>
	public float Speed { get; set; } = 230f;

	/// <summary>Сколько секунд живут искры.</summary>
	public float Duration { get; set; } = 0.5f;

	/// <summary>Цвет искр.</summary>
	public Color Color { get; set; } = EffectStyle.Spark;

	/// <summary>Радиус искры в начале.</summary>
	public float Radius { get; set; } = 5f;

	/// <summary>Насколько искры затягивает вниз.</summary>
	public float Gravity { get; set; } = 420f;

	private float _age;
	private Vector2[]? _directions;

	public override void _Process(double delta)
	{
		_age += (float)delta;
		QueueRedraw();
		if (_age >= Duration)
			QueueFree();
	}

	public override void _Draw()
	{
		_directions ??= BuildDirections();
		float t = Mathf.Clamp(_age / Duration, 0f, 1f);
		float alpha = 1f - t;
		float drop = Gravity * t * t * 0.5f;
		foreach (Vector2 direction in _directions)
		{
			Vector2 at = direction * (Speed * t) + new Vector2(0f, drop);
			EffectStyle.Dot(this, at, Radius * (1f - 0.45f * t), Color, alpha);
		}
	}

	/// <summary>Равномерный круг направлений с разной длиной: без Random для повторяемости.</summary>
	private Vector2[] BuildDirections()
	{
		int count = Mathf.Max(3, Count);
		var directions = new Vector2[count];
		for (int i = 0; i < count; i++)
		{
			float angle = Mathf.Pi * 2f * i / count + (i % 3) * 0.21f;
			float length = 0.6f + 0.4f * ((i * 7) % 5) / 4f;
			directions[i] = Vector2.FromAngle(angle) * length;
		}
		return directions;
	}
}
