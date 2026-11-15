using System;

namespace SwampFrog;

/// <summary>
/// Заглушка адаптера рекламной сети: нативных вызовов не делает и сообщает
/// <see cref="AdBannerState.Unsupported"/>, поэтому в меню виден дом-баннер.
/// </summary>
public sealed class MockBannerAdapter : IAdBannerAdapter
{
	/// <summary>Настоящего SDK нет — реклама недоступна.</summary>
	public bool IsAvailable => false;

	public event Action<AdBannerState>? StateChanged;

	public void Load() => StateChanged?.Invoke(AdBannerState.Unsupported);

	public void Show() => StateChanged?.Invoke(AdBannerState.Unsupported);

	public void Hide() => StateChanged?.Invoke(AdBannerState.Hidden);

	public void Destroy() => StateChanged?.Invoke(AdBannerState.Hidden);
}
