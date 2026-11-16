using System;

namespace SwampFrog;

/// <summary>
/// Контракт ролика рекламы с наградой: интерфейс не знает, какая сеть за ним стоит.
/// Адаптер AdMob добавляется отдельным файлом (см. docs/ADS.md).
/// </summary>
public interface IRewardedAdAdapter
{
	/// <summary>Сеть готова выдать ролик с наградой.</summary>
	bool IsAvailable { get; }

	/// <summary>Показывает ролик: rewarded — досмотрен до конца, closed — закрыт раньше.</summary>
	void Show(Action rewarded, Action closed);

	/// <summary>Освобождает нативные ресурсы ролика.</summary>
	void Destroy();
}