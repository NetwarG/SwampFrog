using Godot;

namespace SwampFrog;

/// <summary>
/// Аура комбо вокруг лягушки: кольцо растёт и меняет цвет по множителю серии, на высоких
/// тирах по нему бегут искры. Множитель 1 (серии нет) прячет ауру, сброс комбо её гасит.
/// </summary>
public partial class ComboAuraView : Node2D
{
	/// <summary>Базовый радиус кольца.</summary>
	private const float AuraRadius = 46f;

	private int _tier = 1;
	private bool _visible;
	private float _phase;
	private float _pop;

	/// <summary>Показывает ли оверлей серию прямо сейчас (проверки).</summary>
	public bool Showing => _visible;

	/// <summary>Множитель, который показывает аура (1 — комбо нет).</summary>
	public int Tier => _tier;

	public override void _Process(double delta)
	{
		if (!_visible)
		{
			return;
		}
		float dt = (float)delta;
		_phase += dt;
		_pop = Mathf.Max(0f, _pop - dt * 2.2f);
		QueueRedraw();
	}

	/// <summary>Позиция лягушки и текущий множитель комбо: меньше двух — аура не показывается.</summary>
	public void SetState(Vector2 frogPosition, int multiplier)
	{
		GlobalPosition = frogPosition;
		bool visible = multiplier >= 2;
		if (visible && multiplier > _tier)
		{
			_pop = 1f;
		}
		_tier = visible ? multiplier : 1;
		_visible = visible;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_visible)
		{
			return;
		}
		Color color = AuraColor(_tier);
		float radius = AuraRadius * (1f + 0.06f * (_tier - 2) + 0.22f * _pop);
		EffectStyle.Ring(this, Vector2.Zero, radius, color, 3f + 0.4f * _tier + 3f * _pop, 0.30f + 0.05f * _tier);

		// На высоких тирах по кольцу бегут искры: видно, что серия держится.
		int sparks = Mathf.Min(4, _tier - 2);
		for (int i = 0; i < sparks; i++)
		{
			float angle = _phase * (0.9f + 0.15f * i) + Mathf.Tau * i / sparks;
			EffectStyle.Dot(this, Vector2.FromAngle(angle) * radius, 4f, color, 0.8f);
		}
	}

	/// <summary>Цвет ауры по множителю: жёлтый → оранжевый → красный.</summary>
	private static Color AuraColor(int tier)
	{
		if (tier >= 5)
		{
			return new Color("ff6b3c");
		}
		if (tier >= 3)
		{
			return EffectStyle.Spark;
		}
		return EffectStyle.Gold;
	}
}
