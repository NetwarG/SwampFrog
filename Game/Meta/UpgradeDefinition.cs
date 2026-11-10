using Godot;

namespace SwampFrog;

/// <summary>Категория постоянного улучшения: определяет, что именно меняет эффект.</summary>
public enum UpgradeKind
{
	/// <summary>Слизь за забег и раунд Suika.</summary>
	SlimeGain,

	/// <summary>Шанс двойного фрукта в запас для Suika.</summary>
	SuikaStock,

	/// <summary>Скидка на скины в магазине.</summary>
	ShopDiscount,

	/// <summary>Скорость падения предметов в классическом режиме.</summary>
	SlowFalling,

	/// <summary>Стартовые жизни забега.</summary>
	StartLives,

	/// <summary>Реролл предложения перков.</summary>
	PerkReroll,
}

/// <summary>Все постоянные улучшения.</summary>
public enum UpgradeId
{
	Wallet,
	Supply,
	Merchant,
	Slow,
	Vitality,
	Reroll,
}

/// <summary>
/// Описание постоянного улучшения: категория, цена, максимум уровней, иконка и тексты
/// эффектов по уровням (русский и английский, как у перков).
/// </summary>
public sealed record UpgradeDefinition(
	UpgradeId Id,
	UpgradeKind Kind,
	string NameRu,
	string NameEn,
	int BasePrice,
	string IconName,
	string[] EffectsRu,
	string[] EffectsEn)
{
	/// <summary>Имя улучшения на текущем языке интерфейса.</summary>
	public string Name => Loc.Pick(NameRu, NameEn);

	/// <summary>Сколько уровней у улучшения.</summary>
	public int MaxLevel => EffectsRu.Length;

	/// <summary>Путь иконки улучшения в Game/assets/ui.</summary>
	public string IconPath => $"res://Game/assets/ui/{IconName}.png";

	/// <summary>Описание эффекта уровня (1 — первый уровень).</summary>
	public string EffectText(int level)
	{
		int index = Mathf.Clamp(level, 1, MaxLevel) - 1;
		return Loc.Pick(EffectsRu, EffectsEn)[index];
	}

	/// <summary>Цена перехода на уровень <paramref name="level"/> (1 — первый).</summary>
	public int PriceFor(int level) => (int)Mathf.Round(BasePrice * (1f + 0.6f * (level - 1)));
}
