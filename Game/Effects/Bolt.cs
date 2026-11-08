using Godot;

namespace SwampFrog;

/// <summary>
/// Ломаная молния от лягушки к цели («Язык-молния»): зигзаг с ярким ядром,
/// вспышка в точке удара и быстрое затухание.
/// </summary>
public partial class Bolt : Node2D
{
	/// <summary>Начало молнии в мировых координатах.</summary>
	public Vector2 From { get; set; }

	/// <summary>Конец молнии в мировых координатах.</summary>
	public Vector2 To { get; set; }

	/// <summary>Сколько секунд живёт разряд.</summary>
	public float Duration { get; set; } = 0.22f;

	/// <summary>Цвет разряда.</summary>
	public Color Color { get; set; } = EffectStyle.Gold;

	/// <summary>Толщина линии в начале.</summary>
	public float Width { get; set; } = 7f;

	/// <summary>Насколько сильно зигзаг отклоняется от прямой (доля длины).</summary>
	public float Jaggedness { get; set; } = 0.16f;

	/// <summary>Сколько звеньев у ломаной.</summary>
	public int Segments { get; set; } = 7;

	private float _age;
	private Vector2[]? _points;

	public override void _Process(double delta)
	{
		_age += (float)delta;
		QueueRedraw();
		if (_age >= Duration)
			QueueFree();
	}

	public override void _Draw()
	{
		_points ??= BuildPoints();
		float t = Mathf.Clamp(_age / Duration, 0f, 1f);
		float alpha = 1f - t;
		EffectStyle.Polyline(this, _points, Color, Width * (1f - 0.5f * t), alpha);
		EffectStyle.Ring(this, To, 16f * (1f - 0.5f * t), EffectStyle.White, 3f, alpha * 0.9f);
	}

	/// <summary>Зигзаг: концы прямые, к середине отклонение растёт как синус.</summary>
	private Vector2[] BuildPoints()
	{
		int count = Mathf.Max(3, Segments);
		var points = new Vector2[count];
		Vector2 direction = To - From;
		Vector2 normal = new(-direction.Y, direction.X);
		float length = normal.Length();
		if (length > 0.01f)
			normal /= length;
		for (int i = 0; i < count; i++)
		{
			float f = i / (float)(count - 1);
			float sway = (i == 0 || i == count - 1) ? 0f : Mathf.Sin(f * Mathf.Pi) * direction.Length() * Jaggedness;
			points[i] = From.Lerp(To, f) + normal * sway * (i % 2 == 0 ? 1f : -1f);
		}
		return points;
	}
}
