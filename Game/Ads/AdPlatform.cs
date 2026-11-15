using Godot;

namespace SwampFrog;

/// <summary>
/// Правила доступности рекламы: платформа и наличие SDK. Единственное место, где решается,
/// можно ли спрашивать объявление у рекламной сети.
/// </summary>
public static class AdPlatform
{
	/// <summary>Мобильная платформа, где нативные баннеры вообще возможны.</summary>
	public static bool IsMobile => OS.HasFeature("android") || OS.HasFeature("ios");

	/// <summary>
	/// Подключён ли рекламный SDK. Пока адаптер рекламной сети не добавлен (docs/ADS.md),
	/// всегда false — в меню показывается дом-баннер.
	/// </summary>
	public static bool HasSdk => false;
}
