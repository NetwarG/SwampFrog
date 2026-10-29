using Godot;

namespace SwampFrog;

/// <summary>
/// Модель провиса рук лягушки: чем длиннее вытянута рука, тем ниже опускается ладонь.
/// Чистая геометрия без узлов — точка ладони, изгиб нарисованной руки и предел длины
/// у кромок поля. Провис считается в «мировых» единицах, как и вся геометрия рук.
/// </summary>
public static class ArmSag
{
	/// <summary>Максимальное опускание ладони в мировых единицах на полной длине руки.</summary>
	public const float MaxDropWorld = 100f;

	/// <summary>Доля максимальной длины руки, до которой провиса ещё нет.</summary>
	public const float StartFraction = 0.15f;

	/// <summary>Доля провиса, добавляемая к изгибу руки в её середине.</summary>
	public const float BowFactor = 1f;

	/// <summary>Максимальный наклон кисти вниз на полном провисе, градусы.</summary>
	public const float PalmTiltDeg = 40f;

	/// <summary>Сегментов ломаной, которой рисуется изогнутая рука.</summary>
	public const int Segments = 10;

	/// <summary>Грубых шагов поиска предела длины руки по высоте.</summary>
	private const int HeightSteps = 16;

	/// <summary>Уточняющих делений внутри найденного шага.</summary>
	private const int HeightRefinements = 6;

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

		/// <summary>Опускание ладони в мировых единицах.</summary>
		public float Drop(float armLength) => MaxDropWorld * UiScale * Amount(armLength);

		/// <summary>Мировая точка ладони: направление руки плюс опускание вниз.</summary>
		public Vector2 HandPosition(Vector2 origin, Vector2 direction, Vector2 down, float armLength) =>
			origin + direction * armLength + down * Drop(armLength);

		/// <summary>
		/// Предел длины руки по высоте: провис опускает ладонь тем сильнее, чем длиннее рука,
		/// поэтому предел ищется шагами от cap вниз и уточняется делением найденного шага.
		/// </summary>
		public float FitLengthInsideHeight(Vector2 origin, Vector2 direction, Vector2 down, float top, float bottom, float cap)
		{
			if (cap <= 0f)
			{
				return 0f;
			}
			float step = cap / HeightSteps;
			for (int i = 0; i <= HeightSteps; i++)
			{
				float length = Mathf.Max(0f, cap - step * i);
				if (!IsInsideHeight(origin, direction, down, length, top, bottom))
				{
					continue;
				}
				return RefineLength(origin, direction, down, length, Mathf.Min(cap, length + step), top, bottom);
			}
			return 0f;
		}

		/// <summary>Уточняет предел в интервале, где ладонь ещё внутри границ.</summary>
		private float RefineLength(Vector2 origin, Vector2 direction, Vector2 down, float inside, float outside, float top, float bottom)
		{
			for (int i = 0; i < HeightRefinements; i++)
			{
				float middle = (inside + outside) * 0.5f;
				if (IsInsideHeight(origin, direction, down, middle, top, bottom))
				{
					inside = middle;
					continue;
				}
				outside = middle;
			}
			return inside;
		}

		/// <summary>Ладонь с провисом остаётся внутри горизонтальных границ поля.</summary>
		private bool IsInsideHeight(Vector2 origin, Vector2 direction, Vector2 down, float armLength, float top, float bottom)
		{
			float y = HandPosition(origin, direction, down, armLength).Y;
			return y >= top && y <= bottom;
		}
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
