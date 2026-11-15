using Godot;

namespace SwampFrog;

/// <summary>
/// Настройки блока рекламы: размеры баннера, идентификаторы AdMob и общий выключатель.
/// Подключение настоящего SDK описано в docs/ADS.md.
/// </summary>
public static class AdConfig
{
	/// <summary>Стандартный баннер 320×50 dp — маленькая полоса у нижней кромки меню.</summary>
	public const int BannerWidthDp = 320;
	public const int BannerHeightDp = 50;

	/// <summary>Тестовые unit id AdMob: обязательны при разработке, иначе аккаунт блокируют.</summary>
	public const string TestBannerAndroid = "ca-app-pub-3940256099942544/6300978111";
	public const string TestBannerIos = "ca-app-pub-3940256099942544/2934735716";

	/// <summary>Рабочие unit id приложения: пустая строка — реклама ещё не настроена.</summary>
	public const string ProductionBannerAndroid = "";
	public const string ProductionBannerIos = "";

	/// <summary>Границы высоты полосы в логических единицах: экран любой пропорции не ломает меню.</summary>
	public const float MinHeightUnits = 44f;
	public const float MaxHeightUnits = 110f;

	/// <summary>Общий выключатель: false полностью убирает блок рекламы из меню.</summary>
	public static bool Enabled { get; set; } = true;

	/// <summary>В отладочной сборке и в редакторе всегда показываются тестовые объявления.</summary>
	public static bool UseTestUnits => OS.IsDebugBuild() || OS.HasFeature("editor");

	/// <summary>Unit id баннера для текущей платформы; пустая строка — запрашивать нечего.</summary>
	public static string BannerUnitId
	{
		get
		{
			bool android = OS.HasFeature("android");
			bool ios = OS.HasFeature("ios");
			if (!android && !ios)
				return string.Empty;
			if (UseTestUnits)
				return android ? TestBannerAndroid : TestBannerIos;
			return android ? ProductionBannerAndroid : ProductionBannerIos;
		}
	}

	/// <summary>Плотность экрана в множителе dp (160 dpi = 1): на телефонах обычно 2–3.</summary>
	public static float Density
	{
		get
		{
			int dpi = DisplayServer.ScreenGetDpi();
			return dpi <= 0 ? 1f : Mathf.Clamp(dpi / 160f, 1f, 3.5f);
		}
	}

	/// <summary>
	/// Высота полосы баннера в логических единицах интерфейса. Нативные объявления задаются
	/// в dp, поэтому размер переводится через плотность экрана и масштаб окна.
	/// </summary>
	/// <param name="pixelsPerUnit">Пикселей на логическую единицу интерфейса.</param>
	public static float HeightUnits(float pixelsPerUnit)
	{
		float pixels = BannerHeightDp * Density;
		float units = pixelsPerUnit > 0f ? pixels / pixelsPerUnit : pixels;
		return Mathf.Clamp(units, MinHeightUnits, MaxHeightUnits);
	}
}
