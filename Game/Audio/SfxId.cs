namespace SwampFrog;

/// <summary>
/// Звуковые эффекты игры. Один идентификатор — один короткий звук
/// (см. <see cref="SoundBank"/>): событие игры вызывает <see cref="Sfx.Play"/>.
/// </summary>
public enum SfxId
{
	/// <summary>Поимка обычного фрукта.</summary>
	Catch,

	/// <summary>Поимка золотого фрукта.</summary>
	Golden,

	/// <summary>Подобрана хилка.</summary>
	Heal,

	/// <summary>Потеря жизни (мусор или промах).</summary>
	Damage,

	/// <summary>Мусор отброшен перком или рассыпался.</summary>
	Deflect,

	/// <summary>Новый уровень.</summary>
	LevelUp,

	/// <summary>Открылся выбор перка.</summary>
	PerkOffer,

	/// <summary>Перк выбран.</summary>
	PerkPick,

	/// <summary>Вырос множитель комбо.</summary>
	Combo,

	/// <summary>Конец игры (классический режим и Suika).</summary>
	GameOver,

	/// <summary>Бросок фрукта в Suika.</summary>
	SuikaThrow,

	/// <summary>Слияние фруктов в Suika.</summary>
	SuikaMerge,

	/// <summary>Нажатие кнопки интерфейса.</summary>
	UiTap,

	/// <summary>Поимка мухи.</summary>
	FlyCatch,
}
