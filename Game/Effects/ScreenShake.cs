using Godot;

namespace SwampFrog;

/// <summary>
/// Затухающая тряска экрана: смещение отрисовки мира без влияния на физику и ввод.
/// </summary>
public sealed class ScreenShake
{
	private float _strength;
	private float _duration;
	private float _time;
	private float _phase;

	/// <summary>Идёт ли тряска сейчас.</summary>
	public bool Active => _duration > 0f && _time < _duration && _strength > 0f;

	/// <summary>Текущее смещение экрана.</summary>
	public Vector2 Offset { get; private set; }

	/// <summary>Добавляет толчок: сильнейший из текущего остатка и нового.</summary>
	public void Kick(float strength, float duration)
	{
		if (strength <= 0f || duration <= 0f) return;
		float remainder = Active ? _strength * (1f - _time / _duration) : 0f;
		_strength = Mathf.Max(remainder, strength);
		_duration = duration;
		_time = 0f;
		_phase += 1.3f;
	}

	/// <summary>Продвигает тряску и возвращает смещение кадра.</summary>
	public Vector2 Update(float dt)
	{
		if (!Active)
		{
			Clear();
			return Vector2.Zero;
		}
		_time += dt;
		if (_time >= _duration)
		{
			Clear();
			return Vector2.Zero;
		}
		float fade = 1f - _time / _duration;
		fade *= fade;
		_phase += dt * 52f;
		Offset = new Vector2(Mathf.Sin(_phase * 1.7f), Mathf.Cos(_phase * 2.3f)) * (_strength * fade);
		return Offset;
	}

	/// <summary>Сбрасывает тряску.</summary>
	public void Clear()
	{
		_strength = 0f;
		_duration = 0f;
		_time = 0f;
		Offset = Vector2.Zero;
	}
}
