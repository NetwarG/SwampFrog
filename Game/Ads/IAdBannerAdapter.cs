using System;

namespace SwampFrog;

/// <summary>
/// Контракт адаптера рекламной сети: <see cref="AdBannerService"/> и интерфейс не знают,
/// какой SDK за ним стоит. Адаптер AdMob добавляется отдельным файлом.
/// </summary>
public interface IAdBannerAdapter
{
	/// <summary>SDK готов принимать запросы объявлений.</summary>
	bool IsAvailable { get; }

	/// <summary>Запрашивает баннер у нижней кромки экрана.</summary>
	void Load();

	/// <summary>Показывает загруженное объявление.</summary>
	void Show();

	/// <summary>Скрывает объявление, не освобождая ресурсы.</summary>
	void Hide();

	/// <summary>Освобождает нативные ресурсы баннера.</summary>
	void Destroy();

	/// <summary>Состояние загрузки изменилось.</summary>
	event Action<AdBannerState>? StateChanged;
}
