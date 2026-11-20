namespace SwampFrog;

/// <summary>
/// Баланс механик Suika: разблокировка по очкам, заряды, тряска, бомба и уменьшение.
/// Значения собраны здесь, чтобы менять поведение без правок логики.
/// </summary>
public static class SuikaMechanicsTuning
{
	// ---------- Стоимость заряда и максимум зарядов ----------
	public const int ShakeChargeScore = 3000;
	public const int BombChargeScore = 2000;
	public const int ShrinkChargeScore = 8000;
	public const int ShakeCharges = 2;
	public const int BombCharges = 2;
	public const int ShrinkCharges = 1;

	// ---------- Тряска (гироскоп) ----------
	/// <summary>Сколько секунд ждём встряхивание после нажатия кнопки.</summary>
	public const float ShakeWindow = 3f;
	/// <summary>Порог угловой скорости гироскопа, рад/с.</summary>
	public const float ShakeGyroThreshold = 3.5f;
	/// <summary>Порог рывка акселерометра, м/с².</summary>
	public const float ShakeAccelThreshold = 18f;
	public const float ShakeStrengthPerPeak = 0.5f;
	public const float ShakeMinStrength = 0.5f;
	public const float ShakeMaxStrength = 1.5f;
	public const float ShakeSwipeDistance = 140f;
	public const float ShakeSwipeDuration = 0.35f;
	public const float ShakeSwipeReference = 1400f;
	public const float ShakeScreenStrength = 14f;
	public const float ShakeScreenDuration = 0.55f;
	public const float ShakeJoltSpeed = 300f;
	/// <summary>Доля горизонтальной скорости, уходящая в подброс вверх.</summary>
	public const float ShakeJoltLift = 0.2f;
	public const float ShakeJoltTorque = 1.4f;
	public const float ShakeJoltInterval = 0.07f;
	public const int ShakeJoltsBase = 4;
	public const int ShakeJoltsMax = 7;

	// ---------- Бомба и взрыв ----------
	public const float BombRadius = 26f;
	public const float BombMassScale = 2f;
	public const float BombThrowScatter = 40f;
	public const float BombStopSpeed = 24f;
	public const float BombSettleDelay = 0.35f;
	public const float BombMaxFuse = 3f;
	/// <summary>Радиус взрыва в долях ширины кувшина.</summary>
	public const float BombBlastRadiusRatio = 0.85f;
	/// <summary>Скорость фрукта в эпицентре, px/с.</summary>
	public const float BombBlastPower = 1100f;
	/// <summary>Предел вертикальной составляющей направления импульса.</summary>
	public const float BombBlastLiftRatio = 0.35f;
	/// <summary>Минимальная доля импульса внутри радиуса: дальние фрукты тоже отлетают.</summary>
	public const float BombBlastMinFalloff = 0.3f;
	/// <summary>Предел скорости вверх, чтобы фрукты не вылетали из кувшина.</summary>
	public const float BombBlastMaxLift = 220f;
	public const float BombBlastTorque = 0.12f;
	public const float BombScreenStrength = 26f;
	public const float BombScreenDuration = 0.6f;
	public const float BombShockwaveScale = 2f;
	public const float BombInnerShockwaveScale = 0.9f;
	public const float BombFlashTint = 0.1f;
	public const float BombFlashDuration = 0.3f;
	public const int BombSparksCount = 40;
	public const float BombSparksSpeed = 480f;
	public const float BombSparksDuration = 0.7f;

	// ---------- Уменьшение и предохранитель ----------
	/// <summary>Во сколько раз уменьшаются фрукты (0.8 = на 20%).</summary>
	public const float ShrinkFactor = 0.8f;
	/// <summary>Сколько секунд после способности фрукты держатся ниже кромки.</summary>
	public const float AbilityGuardTime = 1.2f;
	public const float AbilityGuardMargin = 2f;
}
