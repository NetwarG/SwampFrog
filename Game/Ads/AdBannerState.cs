namespace SwampFrog;

/// <summary>Состояние баннера рекламы: от отсутствия SDK до показанного объявления.</summary>
public enum AdBannerState
{
	/// <summary>Платформа или SDK не поддерживают рекламу: показывается дом-баннер.</summary>
	Unsupported,

	/// <summary>Запрос отправлен, объявление ещё не пришло.</summary>
	Loading,

	/// <summary>Объявление загружено и показывается.</summary>
	Loaded,

	/// <summary>Загрузка не удалась: нет заполнения или ошибка сети.</summary>
	Failed,

	/// <summary>Полоса скрыта: меню закрыто или реклама выключена.</summary>
	Hidden,
}
