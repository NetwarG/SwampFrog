using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Лягушка с вытягивающимися руками. Пока палец зажат — руки растут в направлении
/// точки касания (направление можно менять, двигая палец). Отпустил — руки втягиваются.
/// </summary>
public partial class Frog : Node2D
{
	public const float MaxArmLength = 660f;
	public const float CatchRadius = 44f;

	private const float ArmGrowSpeed = 760f;
	private const float ArmRetractSpeed = 1150f;
	private const float HandSpreadDeg = 13f;
	private const float MinCatchLength = 50f;

	private static readonly Color Skin = new("55a82e");
	private static readonly Color SkinDark = new("2f7d2f");
	private static readonly Color Belly = new("eaf3a6");
	private static readonly Color Hand = new("b4e863");
	private static readonly Color HandDark = new("3a8a2a");

	/// <summary>Ссылка на корень игры (устанавливается из Main).</summary>
	public Main Game { get; set; } = null!;

	private Vector2 _direction = Vector2.Right;
	private float _armLength;
	private bool _holding;
	private float _flash;

	private bool CanAct => Game.State == GameState.Playing;

	/// <summary>Идёт ли ловля прямо сейчас (палец зажат и руки достаточно вытянуты).</summary>
	public bool IsCatching => _holding && _armLength >= MinCatchLength;

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_holding && CanAct)
		{
			_armLength = Mathf.Min(MaxArmLength, _armLength + ArmGrowSpeed * dt);
		}
		else if (_armLength > 0f)
		{
			_armLength = Mathf.Max(0f, _armLength - ArmRetractSpeed * dt);
		}

		_flash = Mathf.Max(0f, _flash - dt);
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

	/// <summary>Мировые позиции обеих ладоней (в координатах сцены).</summary>
	public Vector2[] GetHandPositions()
	{
		if (_armLength <= 2f)
		{
			return Array.Empty<Vector2>();
		}

		float spread = Mathf.DegToRad(HandSpreadDeg);
		Vector2 upper = _direction.Rotated(spread);
		Vector2 lower = _direction.Rotated(-spread);
		return new[]
		{
			GlobalPosition + upper * _armLength,
			GlobalPosition + lower * _armLength,
		};
	}

	/// <summary>Белый «всполох» при попадании мусором.</summary>
	public void Flash() => _flash = 0.45f;

	/// <summary>Маленький подпрыг, когда что-то поймали.</summary>
	public void Pulse()
	{
		Scale = new Vector2(1.18f, 1.18f);
		Tween tw = CreateTween();
		tw.TweenProperty(this, "scale", Vector2.One, 0.18f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
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
		float bob = Mathf.Sin((float)Time.GetTicksMsec() / 380f) * 2.2f;
		Vector2 o = new(0f, bob);

		// --- Руки и ладони (за телом) ---
		if (_armLength > 2f)
		{
			float spread = Mathf.DegToRad(HandSpreadDeg);
			Vector2 upperDir = _direction.Rotated(spread);
			Vector2 lowerDir = _direction.Rotated(-spread);

			foreach (Vector2 dir in new[] { upperDir, lowerDir })
			{
				Vector2 shoulder = dir.Y < 0f ? new Vector2(12f, -16f) : new Vector2(12f, 18f);
				Vector2 handPos = dir * _armLength;

				DrawLine(shoulder, handPos, SkinDark, 10f);
				DrawLine(shoulder, handPos, Skin, 5f);

				DrawCircle(handPos, 15f, HandDark);
				DrawCircle(handPos, 12f, Hand);

				// Пальчики вокруг ладони.
				float angle = dir.Angle();
				for (int i = -1; i <= 1; i++)
				{
					Vector2 finger = Vector2.FromAngle(angle + i * 0.75f);
					DrawCircle(handPos + finger * 14f, 4.5f, Hand);
				}
			}
		}

		// --- Ножки ---
		DrawCircle(o + new Vector2(-18f, 30f), 11f, SkinDark);
		DrawCircle(o + new Vector2(6f, 32f), 11f, SkinDark);
		DrawCircle(o + new Vector2(-17f, 30f), 7f, Skin);
		DrawCircle(o + new Vector2(7f, 32f), 7f, Skin);

		// --- Тело ---
		DrawCircle(o, 42f, SkinDark);
		DrawCircle(o, 40f, Skin);
		DrawCircle(o + new Vector2(2f, 12f), 24f, Belly);

		// --- Глаза ---
		Vector2 pupil = _direction * 5f;
		foreach (Vector2 eye in new[] { o + new Vector2(-11f, -27f), o + new Vector2(12f, -27f) })
		{
			DrawCircle(eye, 15f, new Color("ffffff"));
			DrawCircle(eye + pupil, 7f, new Color("1d1d1d"));
		}

		// --- Румянец ---
		DrawCircle(o + new Vector2(-26f, 8f), 6f, new Color(1f, 0.5f, 0.4f, 0.35f));
		DrawCircle(o + new Vector2(26f, 8f), 6f, new Color(1f, 0.5f, 0.4f, 0.35f));

		// --- Вспышка при уроне ---
		if (_flash > 0f)
		{
			DrawCircle(o, 44f, new Color(1f, 0.35f, 0.3f, 0.55f * Mathf.Min(1f, _flash * 3f)));
		}
	}
}