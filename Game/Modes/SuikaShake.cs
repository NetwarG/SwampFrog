using Godot;

namespace SwampFrog;

/// <summary>
/// Тряска кувшина: кнопка ждёт жест, сила берётся с гироскопа (резерв — акселерометр),
/// на платформах без датчиков работает быстрый свайп.
/// Часть класса SuikaGame: см. SuikaMechanics.cs и SuikaShakeInput.cs.
/// </summary>
public partial class SuikaGame
{
	private int _shakeJoltsLeft;
	private float _shakeJoltTimer;
	private float _shakeJoltStrength;
	private int _shakeJoltSide = -1;
	private bool _shakeSwipeActive;
	private float _shakeSwipeStartX;
	private float _shakeSwipeElapsed;

	/// <summary>Идёт ли ожидание встряхивания.</summary>
	public bool ShakeArmed => _shakeInput.Armed;

	/// <summary>Сила последнего засчитанного встряхивания.</summary>
	public float LastShakeStrength { get; private set; }

	/// <summary>Кнопка «Тряска»: включить ожидание жеста или отменить его.</summary>
	private void ToggleShake()
	{
		if (_shakeInput.Armed)
		{
			CancelShake();
			return;
		}
		if (!_active || _gameOver || _helpOpen || _stockExhausted) return;
		if (!_abilities.IsReady(SuikaAbility.Shake)) return;
		_bombArmed = false;
		_shakeInput.Arm(Input.GetAccelerometer());
		_shakeSwipeActive = false;
		UpdateBombVisuals();
		UpdateAbilityButtons();
		UpdateHint();
	}

	private void CancelShake()
	{
		_shakeInput.Cancel();
		_shakeSwipeActive = false;
		UpdateAbilityButtons();
		UpdateHint();
	}

	/// <summary>Опрос датчиков, пока идёт ожидание жеста.</summary>
	private void UpdateShake(float dt)
	{
		if (!_shakeInput.Armed) return;
		float strength = _shakeInput.SampleGyro(Input.GetGyroscope(), Input.GetAccelerometer(), dt);
		if (strength > 0f)
		{
			ApplyShake(strength);
			return;
		}
		if (!_shakeInput.Armed)
		{
			UpdateAbilityButtons();
			UpdateHint();
		}
	}

	/// <summary>Применяет встряхивание: точка входа для датчика, свайпа и проверок.</summary>
	public void ApplyShake(float strength)
	{
		if (!_active || _gameOver) return;
		if (!_abilities.TrySpend(SuikaAbility.Shake))
		{
			CancelShake();
			return;
		}
		_shakeInput.Cancel();
		_shakeSwipeActive = false;
		LastShakeStrength = Mathf.Clamp(strength,
			SuikaMechanicsTuning.ShakeMinStrength, SuikaMechanicsTuning.ShakeMaxStrength);
		_shakeJoltStrength = LastShakeStrength;
		_shakeJoltsLeft = Mathf.Clamp(
			SuikaMechanicsTuning.ShakeJoltsBase + Mathf.RoundToInt(LastShakeStrength),
			SuikaMechanicsTuning.ShakeJoltsBase, SuikaMechanicsTuning.ShakeJoltsMax);
		_shakeJoltTimer = 0f;
		_shakeJoltSide = -1;
		_settleTime = 0f;
		_completionTimer = 0f;
		_abilityGuard = SuikaMechanicsTuning.AbilityGuardTime;
		Game?.ShakeScreen(SuikaMechanicsTuning.ShakeScreenStrength * LastShakeStrength,
			SuikaMechanicsTuning.ShakeScreenDuration);
		Sfx.Play(SfxId.SuikaShake);
		UpdateAbilityButtons();
		UpdateHint();
	}

	/// <summary>Серия знакопеременных толчков: горка перемешивается без подброса вверх.</summary>
	private void UpdateShakeJolts(float dt)
	{
		if (_shakeJoltsLeft <= 0) return;
		_shakeJoltTimer -= dt;
		if (_shakeJoltTimer > 0f) return;
		_shakeJoltTimer = SuikaMechanicsTuning.ShakeJoltInterval;
		_shakeJoltsLeft--;
		_shakeJoltSide = -_shakeJoltSide;
		ApplyShakeImpulses(_shakeJoltSide, _shakeJoltStrength);
		_abilityGuard = SuikaMechanicsTuning.AbilityGuardTime;
	}

	private void ApplyShakeImpulses(int side, float strength)
	{
		foreach (Piece piece in _pieces)
		{
			if (piece.Removed || piece.IsBomb) continue;
			float mass = piece.Body.Mass;
			piece.Body.Sleeping = false;
			piece.Body.ApplyCentralImpulse(new Vector2(
				side * SuikaMechanicsTuning.ShakeJoltSpeed * strength * mass,
				-SuikaMechanicsTuning.ShakeJoltSpeed * strength * mass * SuikaMechanicsTuning.ShakeJoltLift));
			piece.Body.ApplyTorqueImpulse(side * SuikaMechanicsTuning.ShakeJoltTorque * strength * mass * piece.Radius);
		}
	}

	/// <summary>Свайп-фолбэк, пока кнопка ждёт встряхивание.</summary>
	private void HandleShakeGesture(InputEvent input)
	{
		Vector2? position = input switch
		{
			InputEventScreenTouch touch => touch.Position,
			InputEventScreenDrag drag => drag.Position,
			InputEventMouseMotion motion => motion.Position,
			InputEventMouseButton mouse => mouse.Position,
			_ => null
		};
		if (position == null) return;
		if (input is InputEventScreenTouch { Pressed: true } or InputEventMouseButton { Pressed: true })
		{
			_shakeSwipeActive = true;
			_shakeSwipeStartX = position.Value.X;
			_shakeSwipeElapsed = 0f;
			return;
		}
		if (_shakeSwipeActive && (input is InputEventScreenDrag or InputEventMouseMotion))
			_shakeSwipeElapsed += (float)GetProcessDeltaTime();
		if (!_shakeSwipeActive) return;
		if (input is not (InputEventScreenTouch { Pressed: false } or InputEventMouseButton { Pressed: false }))
			return;
		_shakeSwipeActive = false;
		float strength = _shakeInput.SampleSwipe(position.Value.X - _shakeSwipeStartX, _shakeSwipeElapsed);
		if (strength > 0f) ApplyShake(strength);
	}
}
