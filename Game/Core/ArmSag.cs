using Godot;

namespace SwampFrog;

/// <summary>
/// Модель провиса рук лягушки: чем длиннее вытянута рука, тем сильнее изгибается её середина
/// и наклоняется кисть. Ладонь остаётся у пальца, а провис задаёт только форму нарисованной
/// руки. Чистая геометрия без узлов, в «мировых» единицах, как и вся геометрия рук.
/// </summary>
public static class ArmSag
{
	/// <summary>Максимальное опускание середины руки в мировых единицах на полной длине.</summary>
	public const float MaxDropWorld = 100f;

	/// <summary>Доля максимальной длины руки, до которой провиса ещё нет.</summary>
	public const float StartFraction = 0.15f;

	/// <summary>Доля провиса, добавляемая к изгибу руки в её середине.</summary>
	public const float BowFactor = 1f;

	/// <summary>Максимальный наклон кисти вниз на полном провисе, градусы.</summary>
	public const float PalmTiltDeg = 40f;

	/// <summary>Сегментов ломаной, которой рисуется изогнутая рука.</summary>
	public const int Segments = 10;

	/// <summary>Настройки провиса на текущий кадр: максимальная длина руки и масштаб поля.</summary>
	public readonly record struct Profile(float MaxArmLength, float UiScale)
	{
		/// <summary>Сила провиса 0..1: ноль на короткой руке, единица на полной длине.</summary>
		public float Amount(float armLength)
		{
			float start = MaxArmLength * StartFraction;
			float span = MaxArmLength - start;
			if (span <= 0.0001f)
			{
				return 0f;
			}
			float t = Mathf.Clamp((armLength - start) / span, 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		/// <summary>Опускание середины руки (изгиб) в мировых единицах.</summary>
		public float Drop(float armLength) => MaxDropWorld * UiScale * Amount(armLength);
	}

	/// <summary>Наклон кисти вниз: чем сильнее провис, тем больше ладонь смотрит вниз.</summary>
	public static float PalmTilt(float amount) => Mathf.DegToRad(PalmTiltDeg * amount);

	/// <summary>Середина руки, опущенная на долю провиса: задаёт изгиб нарисованной руки.</summary>
	public static Vector2 ControlPoint(Vector2 body, Vector2 hand, Vector2 down, float drop) =>
		(body + hand) * 0.5f + down * (drop * BowFactor);

	/// <summary>Точка на изогнутой руке: квадратичная кривая Безье от тела к ладони.</summary>
	public static Vector2 Point(Vector2 body, Vector2 control, Vector2 hand, float t)
	{
		float inverse = 1f - t;
		return body * (inverse * inverse) + control * (2f * inverse * t) + hand * (t * t);
	}
}
