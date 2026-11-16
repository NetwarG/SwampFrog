using System;

namespace SwampFrog;

/// <summary>
/// Точка входа рекламы с наградой для интерфейса: настоящий ролик рекламной сети или
/// дом-ролик, пока сети нет (см. docs/ADS.md).
/// </summary>
public sealed class AdRewardService
{
	private readonly IRewardedAdAdapter _adapter;

	/// <param name="adapter">Адаптер рекламной сети; без него — заглушка без нативных вызовов.</param>
	public AdRewardService(IRewardedAdAdapter? adapter = null)
	{
		_adapter = adapter ?? new MockRewardedAdapter();
	}

	/// <summary>Сеть может выдать настоящий ролик.</summary>
	public bool HasRealAd => AdConfig.Enabled && AdPlatform.IsMobile && AdPlatform.HasSdk && _adapter.IsAvailable;

	/// <summary>Играет дом-ролик: рекламной сети нет.</summary>
	public bool UsesHouseAd => !HasRealAd;

	/// <summary>Показывает ролик сети: rewarded — досмотрен, closed — закрыт раньше.</summary>
	public void Show(Action rewarded, Action closed) => _adapter.Show(rewarded, closed);

	/// <summary>Освобождает ресурсы ролика.</summary>
	public void Destroy() => _adapter.Destroy();
}