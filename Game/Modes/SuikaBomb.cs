using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Бомба Suika: лягушка бросает её вместо фрукта, бомба ждёт остановку и взрывается,
/// смещая соседние фрукты. Часть класса SuikaGame: см. SuikaBlast.cs и SuikaBombView.cs.
/// </summary>
public partial class SuikaGame
{
	private readonly List<Piece> _bombs = new();
	private SuikaBombView? _heldBomb;
	private bool _bombArmed;

	/// <summary>Лягушка держит бомбу вместо фрукта.</summary>
	public bool BombArmed => _bombArmed;

	/// <summary>Сколько бомб сейчас в кувшине.</summary>
	public int BombCount => _bombs.Count;

	/// <summary>Сколько взрывов произошло за раунд.</summary>
	public int BlastCount { get; private set; }

	/// <summary>Визуал бомбы в лапе: создаётся один раз вместе с остальным интерфейсом раунда.</summary>
	private void BuildBombVisual()
	{
		_heldBomb = new SuikaBombView { Visible = false, ZIndex = 4, Radius = 16f };
		AddChild(_heldBomb);
	}

	/// <summary>Кнопка «Бомба»: взять бомбу вместо фрукта или вернуть фрукт.</summary>
	private void ToggleBomb()
	{
		if (_bombArmed)
		{
			_bombArmed = false;
			UpdateBombVisuals();
			UpdateHint();
			UpdateAbilityButtons();
			return;
		}
		if (!_active || _gameOver || _helpOpen || _stockExhausted) return;
		if (!_abilities.IsReady(SuikaAbility.Bomb)) return;
		_shakeInput.Cancel();
		_bombArmed = true;
		UpdateBombVisuals();
		UpdateHint();
		UpdateAbilityButtons();
	}

	/// <summary>Показывает либо фрукт в лапе, либо бомбу.</summary>
	private void UpdateBombVisuals()
	{
		if (_heldBomb != null) _heldBomb.Visible = _bombArmed && !_stockExhausted;
		if (_heldFruit != null) _heldFruit.Visible = !_bombArmed && !_stockExhausted;
	}

	/// <summary>Бомба в лапе висит там же, где висел бы фрукт.</summary>
	private void UpdateHeldBomb(float x)
	{
		if (_heldBomb == null) return;
		_heldBomb.Position = new Vector2(x, _bowl.Position.Y - ReleaseAboveRim);
		_heldBomb.Visible = _bombArmed && !_stockExhausted;
	}

	/// <summary>Бросок бомбы: заряд тратится только здесь.</summary>
	private void DropBomb()
	{
		if (!_active || _gameOver || _dropCooldown > 0f || _stockExhausted) return;
		if (!_abilities.TrySpend(SuikaAbility.Bomb))
		{
			_bombArmed = false;
			UpdateBombVisuals();
			UpdateHint();
			UpdateAbilityButtons();
			return;
		}
		float radius = SuikaMechanicsTuning.BombRadius;
		float x = ClampDropX(_aimX, radius);
		Vector2 velocity = new(
			_rng.RandfRange(-SuikaMechanicsTuning.BombThrowScatter, SuikaMechanicsTuning.BombThrowScatter),
			ThrowSpeed);
		CreateBomb(new Vector2(x, _bowl.Position.Y - ReleaseAboveRim), velocity);
		_bombArmed = false;
		_throwTimer = 0.24f;
		_dropCooldown = 0.3f;
		_hintDismissed = true;
		_abilityGuard = SuikaMechanicsTuning.AbilityGuardTime;
		Sfx.Play(SfxId.SuikaThrow);
		UpdateBombVisuals();
		UpdateHint();
		UpdateAbilityButtons();
	}

	private Piece CreateBomb(Vector2 position, Vector2 velocity)
	{
		float radius = SuikaMechanicsTuning.BombRadius;
		var body = new RigidBody2D
		{
			Name = "Bomb",
			CollisionLayer = SuikaCollisionLayer,
			CollisionMask = SuikaCollisionLayer,
			Mass = Mathf.Pow(radius / FruitRadius(FruitKind.Cherry), MassExponent)
				* SuikaMechanicsTuning.BombMassScale,
			PhysicsMaterialOverride = _fruitMaterial,
			LinearDampMode = RigidBody2D.DampMode.Replace,
			LinearDamp = FruitLinearDamp,
			AngularDampMode = RigidBody2D.DampMode.Replace,
			AngularDamp = 0.6f,
			ContinuousCd = RigidBody2D.CcdMode.CastShape,
			CanSleep = true,
			LinearVelocity = velocity
		};
		var piece = new Piece
		{
			Body = body,
			Kind = FruitKind.Cherry,
			Radius = radius,
			IsBomb = true
		};
		var collider = new CollisionShape2D { Shape = new CircleShape2D { Radius = radius } };
		body.AddChild(collider);
		piece.Collider = collider;
		body.Position = FitPieceInsideBowl(piece, position);
		_pieces.Add(piece);
		_piecesByBody.Add(body.GetInstanceId(), piece);
		AddChild(body);
		var visual = new SuikaBombView { Radius = radius };
		body.AddChild(visual);
		piece.Visual = visual;
		_bombs.Add(piece);
		return piece;
	}

	/// <summary>Ждём остановку бомбы (или предохранитель) и взрываем.</summary>
	private void UpdateBombs(float dt)
	{
		for (int i = _bombs.Count - 1; i >= 0; i--)
		{
			Piece bomb = _bombs[i];
			if (bomb.Removed || bomb.Body.IsQueuedForDeletion())
			{
				_bombs.RemoveAt(i);
				continue;
			}
			bool stopped = bomb.Body.Sleeping
				|| bomb.Body.LinearVelocity.Length() < SuikaMechanicsTuning.BombStopSpeed;
			bomb.RestTime = stopped ? bomb.RestTime + dt : 0f;
			if (bomb.RestTime < SuikaMechanicsTuning.BombSettleDelay
				&& bomb.Age < SuikaMechanicsTuning.BombMaxFuse)
				continue;
			Vector2 at = bomb.Body.Position;
			_bombs.RemoveAt(i);
			RemovePiece(bomb);
			ExplodeAt(at);
		}
	}

	/// <summary>Взрыв: радиальные импульсы фруктам с затуханием и ограниченным подъёмом.</summary>
	private void ExplodeAt(Vector2 center)
	{
		BlastCount++;
		float radius = _bowl.Size.X * SuikaMechanicsTuning.BombBlastRadiusRatio;
		foreach (Piece piece in _pieces)
		{
			if (piece.Removed || piece.IsBomb) continue;
			Vector2 offset = piece.Body.Position - center;
			float raw = SuikaBlast.Falloff(offset.Length(), radius);
			if (raw <= 0f) continue;
			// Дальние фрукты внутри радиуса тоже получают импульс — взрыв не «гаснет» у края.
			float falloff = Mathf.Max(raw, SuikaMechanicsTuning.BombBlastMinFalloff);
			Vector2 direction = SuikaBlast.Direction(offset);
			float mass = piece.Body.Mass;
			Vector2 push = direction * (SuikaMechanicsTuning.BombBlastPower * falloff);
			// Вверх не быстрее лимита: фрукт не должен вылетать из кувшина.
			push.Y = Mathf.Max(push.Y, -SuikaMechanicsTuning.BombBlastMaxLift);
			piece.Body.Sleeping = false;
			piece.Body.ApplyCentralImpulse(push * mass);
			piece.Body.ApplyTorqueImpulse((offset.X >= 0f ? 1f : -1f)
				* SuikaMechanicsTuning.BombBlastTorque * SuikaMechanicsTuning.BombBlastPower
				* falloff * mass * piece.Radius);
		}
		SpawnBlastEffects(center, radius);
		_abilityGuard = SuikaMechanicsTuning.AbilityGuardTime;
		_settleTime = 0f;
		_completionTimer = 0f;
		Game?.ShakeScreen(SuikaMechanicsTuning.BombScreenStrength,
			SuikaMechanicsTuning.BombScreenDuration);
		Sfx.Play(SfxId.SuikaBlast);
		UpdateAbilityButtons();
	}

	/// <summary>Кольца, вспышка и искры взрыва поверх фруктов и под интерфейсом.</summary>
	private void SpawnBlastEffects(Vector2 center, float radius)
	{
		AddChild(new Shockwave
		{
			Position = center,
			MaxRadius = radius * SuikaMechanicsTuning.BombShockwaveScale,
			Color = EffectStyle.Spark,
			Duration = 0.5f,
			Width = 9f,
			ZIndex = 6
		});
		AddChild(new Shockwave
		{
			Position = center,
			MaxRadius = radius * SuikaMechanicsTuning.BombInnerShockwaveScale,
			Color = EffectStyle.White,
			Duration = 0.28f,
			Width = 12f,
			ZIndex = 6
		});
		AddChild(new FlashWave
		{
			OriginPoint = center,
			FieldSize = ViewSize,
			MaxRadius = radius * SuikaMechanicsTuning.BombShockwaveScale,
			Duration = SuikaMechanicsTuning.BombFlashDuration,
			Color = EffectStyle.Gold,
			Tint = SuikaMechanicsTuning.BombFlashTint,
			ZIndex = 6
		});
		AddChild(new Sparks
		{
			Position = center,
			Count = SuikaMechanicsTuning.BombSparksCount,
			Speed = SuikaMechanicsTuning.BombSparksSpeed,
			Duration = SuikaMechanicsTuning.BombSparksDuration,
			Color = EffectStyle.Gold,
			ZIndex = 6
		});
	}
}
