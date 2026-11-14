using Godot;

namespace SwampFrog;

/// <summary>
/// Муха: летает по полю и садится на фрукт, чтобы испортить его. Фазы:
/// Wander (блуждание) → Approach (подлёт к цели) → Sit (порча) → Wander.
/// Рисуется процедурно (ассетов нет) и ловится ладонью лягушки — см. <see cref="FlySwarm"/>.
/// </summary>
public partial class Fly : Node2D
{
	private enum Phase
	{
		Wander,
		Approach,
		Sit,
	}

	/// <summary>Радиус хитбокса мухи в мировых единицах.</summary>
	public const float HitRadius = 15f;

	private Phase _phase = Phase.Wander;
	private Vector2 _velocity;
	private float _wanderTimer;
	private float _sitTimer;
	private float _age;
	private float _heading;

	/// <summary>Фрукт, который муха собирается испортить (null — цель ещё не выбрана).</summary>
	public FallingItem? Target { get; private set; }

	/// <summary>Хитбокс мухи: с ним сравнивается круговая ладонь лягушки.</summary>
	public CircleHitbox Hitbox => new(GlobalPosition, HitRadius);

	/// <summary>Муха сидит на фрукте: цель уже не меняется.</summary>
	public bool IsSitting => _phase == Phase.Sit;

	public override void _Ready()
	{
		_heading = (float)GD.RandRange(0.0, Mathf.Tau);
		_velocity = Vector2.FromAngle(_heading) * FlyTuning.WanderSpeed;
	}

	/// <summary>Назначает цель: муха летит к фрукту и испортит его, когда сядет.</summary>
	public void SetTarget(FallingItem fruit)
	{
		Target = fruit;
		_phase = Phase.Approach;
		_sitTimer = 0f;
	}

	/// <summary>Сбрасывает цель (фрукт исчез или уже протух) и возвращает муху к блужданию.</summary>
	public void ClearTarget()
	{
		Target = null;
		_phase = Phase.Wander;
		_wanderTimer = 0f;
	}

	/// <summary>
	/// Один шаг полёта. Возвращает true в кадре, когда муха досидела до порчи фрукта:
	/// решение о порче принимает <see cref="FlySwarm"/>, чтобы эффекты шли через слой игры.
	/// </summary>
	public bool Update(double delta, Vector2 fieldSize)
	{
		float dt = (float)delta;
		_age += dt;
		bool spoilDue = false;

		switch (_phase)
		{
			case Phase.Sit:
				if (!IsTargetValid())
				{
					ClearTarget();
					Wander(dt);
					break;
				}
				FollowTarget();
				_sitTimer += dt;
				spoilDue = _sitTimer >= FlyTuning.SitTime;
				break;
			case Phase.Approach:
				if (!IsTargetValid())
				{
					ClearTarget();
					break;
				}
				SteerTo(Target!.GlobalPosition, dt);
				if (GlobalPosition.DistanceTo(Target!.GlobalPosition) <= FlyTuning.LandDistance)
				{
					_phase = Phase.Sit;
					_sitTimer = 0f;
				}
				break;
			default:
				Wander(dt);
				break;
		}

		if (_phase != Phase.Sit)
			Integrate(dt, fieldSize);

		// Тело нарисовано головой вверх: к курсу его доворачивает четверть оборота.
		if (_velocity.LengthSquared() > 4f)
			_heading = Mathf.LerpAngle(_heading, _velocity.Angle(), 0.25f);
		Rotation = _heading + Mathf.Pi * 0.5f;
		QueueRedraw();
		return spoilDue;
	}

	/// <summary>
	/// Цель ещё жива и её можно портить: обычный фрукт на поле, не пойманный,
	/// не протухший и не обездвиженный.
	/// </summary>
	private bool IsTargetValid() =>
		Target != null && IsInstanceValid(Target) && !Target.IsQueuedForDeletion() && !Target.IsCaught
		&& Target.ItemType == ItemType.Fruit && !Target.Rotten && !Target.Frozen;

	/// <summary>Муха держится на верхушке фрукта и едет вместе с ним.</summary>
	private void FollowTarget()
	{
		Vector2 at = Target!.GlobalPosition;
		GlobalPosition = at - new Vector2(0f, Target.VisualRadius * 0.7f + HitRadius * 0.5f);
		_velocity = Vector2.Zero;
	}

	/// <summary>Плавный доворот к точке: муха не разворачивается мгновенно.</summary>
	private void SteerTo(Vector2 point, float dt)
	{
		Vector2 desired = (point - GlobalPosition).Normalized() * FlyTuning.ApproachSpeed;
		_velocity = _velocity.MoveToward(desired, FlyTuning.ArriveAcceleration * dt);
	}

	/// <summary>Блуждание: курс меняется случайно с паузой между разворотами.</summary>
	private void Wander(float dt)
	{
		_wanderTimer -= dt;
		if (_wanderTimer > 0f)
			return;
		_wanderTimer = (float)GD.RandRange(FlyTuning.WanderTurnMin, FlyTuning.WanderTurnMax);
		_velocity = Vector2.FromAngle((float)GD.RandRange(0.0, Mathf.Tau)) * FlyTuning.WanderSpeed;
	}

	/// <summary>Сдвиг и отражение от границ поля: муха остаётся в игре и не уходит в HUD.</summary>
	private void Integrate(float dt, Vector2 fieldSize)
	{
		Vector2 pos = GlobalPosition + _velocity * dt;
		float right = Mathf.Max(FlyTuning.SideMargin + 1f, fieldSize.X - FlyTuning.SideMargin);
		float bottom = Mathf.Max(FlyTuning.TopMargin + 1f, fieldSize.Y - FlyTuning.BottomMargin);
		if (pos.X < FlyTuning.SideMargin)
		{
			pos.X = FlyTuning.SideMargin;
			_velocity.X = Mathf.Abs(_velocity.X);
		}
		else if (pos.X > right)
		{
			pos.X = right;
			_velocity.X = -Mathf.Abs(_velocity.X);
		}
		if (pos.Y < FlyTuning.TopMargin)
		{
			pos.Y = FlyTuning.TopMargin;
			_velocity.Y = Mathf.Abs(_velocity.Y);
		}
		else if (pos.Y > bottom)
		{
			pos.Y = bottom;
			_velocity.Y = -Mathf.Abs(_velocity.Y);
		}
		GlobalPosition = pos;
	}

	public override void _Draw()
	{
		float flap = Mathf.Sin(_age * FlyTuning.WingFlapSpeed) * 3f;
		DrawWing(-1f, flap);
		DrawWing(1f, -flap);
		// Лапки.
		DrawLine(new Vector2(-6f, 6f), new Vector2(-10f, 13f), FlyStyle.Body, 2f);
		DrawLine(new Vector2(6f, 6f), new Vector2(10f, 13f), FlyStyle.Body, 2f);
		// Тельце с полосой, голова и крупные глаза: муху легко узнать на фоне пруда.
		DrawCircle(Vector2.Zero, 7f, EffectStyle.Outline);
		DrawCircle(Vector2.Zero, 5.5f, FlyStyle.Body);
		DrawCircle(new Vector2(0f, 0.5f), 4.2f, FlyStyle.Stripe);
		DrawCircle(new Vector2(0f, -6f), 4.5f, EffectStyle.Outline);
		DrawCircle(new Vector2(0f, -6f), 3.4f, FlyStyle.Body);
		DrawCircle(new Vector2(-1.6f, -7f), 1.3f, EffectStyle.White);
		DrawCircle(new Vector2(1.6f, -7f), 1.3f, EffectStyle.White);
	}

	/// <summary>Полупрозрачное крыло-четырёхугольник, взмах задаёт сдвиг внешних углов.</summary>
	private void DrawWing(float side, float flap)
	{
		Vector2[] quad =
		{
			new(0f, -2f),
			new(side * 16f, -9f + flap),
			new(side * 20f, 4f + flap),
			new(0f, 4f),
		};
		DrawColoredPolygon(quad, new Color(FlyStyle.Wing, 0.5f));
		DrawPolyline(new[] { quad[0], quad[1], quad[2], quad[3], quad[0] },
			new Color(EffectStyle.Outline, 0.5f), 1.5f);
	}
}
