using System;

namespace SwampFrog;

/// <summary>
/// Заглушка ролика с наградой: сети нет, поэтому ролик недоступен, а вместо него игра
/// показывает дом-ролик (см. RewardedAdOverlay).
/// </summary>
public sealed class MockRewardedAdapter : IRewardedAdAdapter
{
	/// <summary>Настоящего SDK нет — ролик недоступен.</summary>
	public bool IsAvailable => false;

	/// <summary>Сети нет: сообщает о закрытии ролика без награды.</summary>
	public void Show(Action rewarded, Action closed) => closed();

	public void Destroy()
	{
	}
}