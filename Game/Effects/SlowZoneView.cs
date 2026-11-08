using Godot;

namespace SwampFrog;

/// <summary>
/// Индикатор «Зоны замедления»: полупрозрачная полоса от верхней границы зоны до дна
/// с волнистой кромкой. Живёт всё время, пока перк активен, и растёт с его уровнем.
/// </summary>
public partial class SlowZoneView : Node2D
{
	/// <summary>Сколько сегментов у волнистой кромки.</summary>
	private const int WaveSegments = 36;

	/// <summary>Высота волны кромки в мировых единицах.</summary>
	private const float WaveHeight = 7f;

	/// <summary>Скорость бега волны.</summary>
	private const float WaveSpeed = 1.1f;

	/// <summary>Верхняя граница зоны в мировых координатах (0 — зоны нет).</summary>
	public float ZoneTop { get; private set; }

	/// <summary>Размер поля для заливки зоны.</summary>
	public Vector2 FieldSize { get; private set; } = Playfield.Size;

	/// <summary>Плотность заливки: чем выше уровень перка, тем заметнее зона.</summary>
	public float Strength { get; private set; } = 0.1f;

	private float _phase;

	/// <summary>Задаёт границы зоны: 0 высоты или 0 верхней границы выключают индикатор.</summary>
	public void SetZone(float zoneTop, Vector2 fieldSize, float strength)
	{
		bool visible = zoneTop > 1f && fieldSize.Y > 1f;
		ZoneTop = visible ? zoneTop : 0f;
		FieldSize = fieldSize;
		Strength = Mathf.Clamp(strength, 0.02f, 0.3f);
		if (Visible != visible)
			Visible = visible;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		_phase += (float)delta * WaveSpeed;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (ZoneTop <= 1f)
			return;
		float height = FieldSize.Y - ZoneTop;
		if (height <= 1f)
			return;
		DrawRect(new Rect2(0f, ZoneTop, FieldSize.X, height), new Color(EffectStyle.Syrup, Strength));
		var edge = new Vector2[WaveSegments + 1];
		for (int i = 0; i <= WaveSegments; i++)
		{
			float f = i / (float)WaveSegments;
			float y = ZoneTop + Mathf.Sin(f * Mathf.Pi * 3f + _phase) * WaveHeight;
			edge[i] = new Vector2(FieldSize.X * f, y);
		}
		DrawPolyline(edge, new Color(EffectStyle.Syrup, Strength * 6f), 3f);
	}
}
