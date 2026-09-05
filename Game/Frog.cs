using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Лягушка с вытягивающимися руками. Пока палец зажат — руки растут в направлении
/// точки касания (направление можно менять, двигая палец). Отпустил — руки втягиваются.
/// Вся геометрия рук хранится в «мировых» единицах (как и границы экрана), поэтому руки
/// всегда дотягиваются до любого угла экрана на любом разрешении.
/// </summary>
public partial class Frog : Node2D
{
	public const float CatchRadius = 44f;

	/// <summary>На сколько дольше максимально возможной дистанции тянутся руки.</summary>
	private const float ReachMargin = 1.06f;
	private const float MinReach = 340f;

	/// <summary>Время полного вытягивания рук от нуля до максимума, секунды.</summary>
	private const float FullExtendTime = 0.8f;
	private const float RetractScale = 1.55f;
	private const float HandSpreadDeg = 13f;
	private const float MinCatchLengthPx = 50f;

	private static readonly Color Skin = new("55a82e");
	private static readonly Color SkinDark = new("2f7d2f");
	private static readonly Color Belly = new("eaf3a6");
	private static readonly Color Hand = new("b4e863");
	private static readonly Color HandDark = new("3a8a2a");

	/// <summary>Ссылка на корень игры (устанавливается из Main).</summary>
	public Main Game { get; set; } = null!;

	private Vector2 _direction = Vector2.Right;
	/// <summary>Длины рук в «мировых» единицах экрана.</summary>
	private float _armLengthWorld;
	private float _armGrowSpeedWorld;
	private float _armRetractSpeedWorld;
	private bool _holding;
	private float _flash;
	private bool _pulsing;
	private float _pulseT;
	private float _currentUiScale = 1f;

	private bool CanAct => Game.State == GameState.Playing;

	/// <summary>Текущий коэффициент масштаба UI/мира, чтобы лягушка не «худела» на больших экранах.</summary>
	public float UiScale => _currentUiScale;

	/// <summary>Радиус «ловли» ладонью в мировых единицах (учитывает масштаб ноды).</summary>
	public float CatchRadiusWorld => CatchRadius * _currentUiScale;

	/// <summary>Обновляет текущий масштаб ноды из корня игры.</summary>
	public void SyncUiScale(float uiScale)
	{
		_currentUiScale = uiScale;
	}

	/// <summary>Идёт ли ловля прямо сейчас (палец зажат и руки достаточно вытянуты).</summary>
	public bool IsCatching => _holding && _armLengthWorld >= MinCatchLengthPx * _currentUiScale;

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Пересчитываем длины рук под текущее разрешение экрана.
		UpdateArmLengths(ComputeMaxArmLength(Game.ViewSize));

		if (_holding && CanAct)
		{
			_armLengthWorld = Mathf.Min(ComputeMaxArmLength(Game.ViewSize), _armLengthWorld + _armGrowSpeedWorld * dt);
		}
		else if (_armLengthWorld > 0f)
		{
			_armLengthWorld = Mathf.Max(0f, _armLengthWorld - _armRetractSpeedWorld * dt);
		}

		_flash = Mathf.Max(0f, _flash - dt);

		float uiScale = UiScale;
		if (_pulsing)
		{
			_pulseT = Mathf.Min(1f, _pulseT + dt * 5f);
			Scale = Vector2.One * uiScale * Mathf.Lerp(1.18f, 1f, Mathf.SmoothStep(0f, 1f, _pulseT));
			if (_pulseT >= 1f)
			{
				_pulsing = false;
			}
		}
		else
		{
			Scale = Vector2.One * uiScale;
		}

		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				if (!CanAct)
				{
					return;
				}
				_holding = true;
				UpdateDirection(touch.Position);
			}
			else
			{
				_holding = false;
			}
		}
		else if (@event is InputEventScreenDrag drag)
		{
			if (_holding && CanAct)
			{
				UpdateDirection(drag.Position);
			}
		}
	}

	/// <summary>
	/// Максимальная длина рук (мировые единицы), которая гарантирует доставание
	/// до самого дальнего угла видимой области.
	/// </summary>
	public float ComputeMaxArmLength(Vector2 viewSize)
	{
		float maxViewDist = GlobalPosition.DistanceTo(Vector2.Zero);
		maxViewDist = Mathf.Max(maxViewDist, GlobalPosition.DistanceTo(new Vector2(viewSize.X, 0f)));
		maxViewDist = Mathf.Max(maxViewDist, GlobalPosition.DistanceTo(new Vector2(0f, viewSize.Y)));
		maxViewDist = Mathf.Max(maxViewDist, GlobalPosition.DistanceTo(new Vector2(viewSize.X, viewSize.Y)));
		return Mathf.Max(MinReach, maxViewDist * ReachMargin);
	}

	/// <summary>Мировые позиции обеих ладоней (в координатах сцены).</summary>
	public Vector2[] GetHandPositions()
	{
		if (_armLengthWorld <= 2f)
		{
			return Array.Empty<Vector2>();
		}

		float spread = Mathf.DegToRad(HandSpreadDeg);
		Vector2 upper = _direction.Rotated(spread);
		Vector2 lower = _direction.Rotated(-spread);
		return new[]
		{
			GlobalPosition + upper * _armLengthWorld,
			GlobalPosition + lower * _armLengthWorld,
		};
	}

	/// <summary>Белый «всполох» при попадании мусором.</summary>
	public void Flash() => _flash = 0.45f;

	/// <summary>Маленький подпрыг, когда что-то поймали (без конфликта с масштабом).</summary>
	public void Pulse()
	{
		_pulsing = true;
		_pulseT = 0f;
	}

	private void UpdateArmLengths(float maxWorld)
	{
		_armGrowSpeedWorld = maxWorld / FullExtendTime;
		_armRetractSpeedWorld = _armGrowSpeedWorld * RetractScale;
	}

	private void UpdateDirection(Vector2 target)
	{
		Vector2 to = target - GlobalPosition;
		if (to.LengthSquared() > 4f)
		{
			_direction = to.Normalized();
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		float ui = _currentUiScale;
		float bob = Mathf.Sin((float)Time.GetTicksMsec() / 380f) * 2.2f * ui;
		Vector2 o = new(0f, bob);

		// --- Руки и ладони (за телом) ---
		if (_armLengthWorld > 2f)
		{
			float spread = Mathf.DegToRad(HandSpreadDeg);
			Vector2 upperDir = _direction.Rotated(spread);
			Vector2 lowerDir = _direction.Rotated(-spread);
			// Локальная длина руки в координатах ноды (одна и та же для обеих рук).
			float armLocal = _armLengthWorld / ui;

			foreach (Vector2 dir in new[] { upperDir, lowerDir })
			{
				Vector2 handLocal = dir * armLocal;
				DrawLine(Vector2.Zero, handLocal, SkinDark, 10f * ui);
				DrawLine(Vector2.Zero, handLocal, Skin, 5f * ui);

				DrawCircle(handLocal, 15f * ui, HandDark);
				DrawCircle(handLocal, 12f * ui, Hand);

				// Пальчики вокруг ладони.
				float angle = dir.Angle();
				for (int i = -1; i <= 1; i++)
				{
					Vector2 finger = Vector2.FromAngle(angle + i * 0.75f);
					DrawCircle(handLocal + finger * (14f * ui), 4.5f * ui, Hand);
				}
			}
		}

		// --- Ножки ---
		DrawCircle(o + new Vector2(-18f, 30f) * ui, 11f * ui, SkinDark);
		DrawCircle(o + new Vector2(6f, 32f) * ui, 11f * ui, SkinDark);
		DrawCircle(o + new Vector2(-17f, 30f) * ui, 7f * ui, Skin);
		DrawCircle(o + new Vector2(7f, 32f) * ui, 7f * ui, Skin);

		// --- Тело ---
		DrawCircle(o, 42f * ui, SkinDark);
		DrawCircle(o, 40f * ui, Skin);
		DrawCircle(o + new Vector2(2f, 12f) * ui, 24f * ui, Belly);

		// --- Глаза ---
		Vector2 pupil = _direction * (5f * ui);
		foreach (Vector2 eye in new[] { o + new Vector2(-11f, -27f) * ui, o + new Vector2(12f, -27f) * ui })
		{
			DrawCircle(eye, 15f * ui, new Color("ffffff"));
			DrawCircle(eye + pupil, 7f * ui, new Color("1d1d1d"));
		}

		// --- Румянец ---
		DrawCircle(o + new Vector2(-26f, 8f) * ui, 6f * ui, new Color(1f, 0.5f, 0.4f, 0.35f));
		DrawCircle(o + new Vector2(26f, 8f) * ui, 6f * ui, new Color(1f, 0.5f, 0.4f, 0.35f));

		// --- Вспышка при уроне ---
		if (_flash > 0f)
		{
			DrawCircle(o, 44f * ui, new Color(1f, 0.35f, 0.3f, 0.55f * Mathf.Min(1f, _flash * 3f)));
		}
	}
}