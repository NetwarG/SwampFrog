using Godot;

namespace SwampFrog;

/// <summary>
/// Детектор встряхивания: пик гироскопа за окно, резерв по акселерометру
/// и быстрый свайп для платформ без сенсоров.
/// </summary>
public sealed class SuikaShakeInput
{
	private bool _armed;
	private float _time;
	private float _peak;
	private Vector3 _previousAcceleration;

	/// <summary>Идёт ли ожидание встряхивания.</summary>
	public bool Armed => _armed;

	/// <summary>Начинает ожидание; запоминает ускорение для первого замера.</summary>
	public void Arm(Vector3 acceleration)
	{
		_armed = true;
		_time = 0f;
		_peak = 0f;
		_previousAcceleration = acceleration;
	}

	/// <summary>Прерывает ожидание без засчитывания жеста.</summary>
	public void Cancel()
	{
		_armed = false;
		_peak = 0f;
	}

	/// <summary>
	/// Замер гироскопа и акселерометра. Возвращает силу жеста 0.5…1.5
	/// или 0, пока встряхивание не засчитано и пока окно не истекло.
	/// </summary>
	public float SampleGyro(Vector3 gyro, Vector3 acceleration, float dt)
	{
		if (!_armed) return 0f;
		_time += Mathf.Max(dt, 0f);
		if (_time >= SuikaMechanicsTuning.ShakeWindow)
		{
			Cancel();
			return 0f;
		}
		float rotation = gyro.Length();
		float jerk = (acceleration - _previousAcceleration).Length() / Mathf.Max(dt, 0.001f);
		_previousAcceleration = acceleration;
		// Пик считается в долях порога: единица — жест уже засчитан.
		float peak = Mathf.Max(rotation / SuikaMechanicsTuning.ShakeGyroThreshold,
			jerk / SuikaMechanicsTuning.ShakeAccelThreshold);
		_peak = Mathf.Max(_peak, peak);
		if (_peak < 1f) return 0f;
		_armed = false;
		float strength = SuikaMechanicsTuning.ShakeMinStrength
			+ (_peak - 1f) * SuikaMechanicsTuning.ShakeStrengthPerPeak;
		return Mathf.Clamp(strength,
			SuikaMechanicsTuning.ShakeMinStrength, SuikaMechanicsTuning.ShakeMaxStrength);
	}

	/// <summary>Быстрый свайп по полю как альтернатива датчикам на ПК.</summary>
	public float SampleSwipe(float distance, float duration)
	{
		if (!_armed || duration <= 0f || duration > SuikaMechanicsTuning.ShakeSwipeDuration) return 0f;
		if (Mathf.Abs(distance) < SuikaMechanicsTuning.ShakeSwipeDistance) return 0f;
		_armed = false;
		float speed = Mathf.Abs(distance) / duration;
		return Mathf.Clamp(speed / SuikaMechanicsTuning.ShakeSwipeReference,
			SuikaMechanicsTuning.ShakeMinStrength, SuikaMechanicsTuning.ShakeMaxStrength);
	}
}
