using System;

namespace SwampFrog;

/// <summary>
/// Модель опыта и уровней. Чистый C# класс без привязки к Godot-нодам,
/// чтобы логику можно было легко тестировать и расширять.
///
/// Расширение функционала:
/// - новые источники опыта — добавить строку в <see cref="XpFor"/>;
/// - награды за уровень — подписаться на <see cref="LevelUp"/>;
/// - бонусы/перки, зависящие от уровня — добавить методы чтения свойств здесь.
/// </summary>
public sealed class XpSystem
{
	/// <summary>Количество опыта, необходимое для перехода на следующий уровень.</summary>
	public const int XpPerLevel = 10;

	private int _totalXp;

	/// <summary>Текущий уровень (начинается с 1).</summary>
	public int Level { get; private set; } = 1;

	/// <summary>Суммарный опыт за текущую партию.</summary>
	public int TotalXp => _totalXp;

	/// <summary>Опыт, накопленный внутри текущего уровня.</summary>
	public int CurrentLevelXp => _totalXp % XpPerLevel;

	/// <summary>Прогресс заполнения шкалы текущего уровня, 0..1.</summary>
	public float LevelProgress => (float)CurrentLevelXp / XpPerLevel;

	/// <summary>Происходит при повышении уровня. Аргумент — новый уровень (начиная с 2).</summary>
	public event Action<int>? LevelUp;

	/// <summary>
	/// Единое место, где типу предмета сопоставляется награда опытом.
	/// Для новых типов достаточно добавить строку здесь.
	/// </summary>
	public static int XpFor(ItemType type)
	{
		switch (type)
		{
			case ItemType.Fruit:
				return 1;
			case ItemType.GoldenFruit:
				return 5;
			default:
				return 0;
		}
	}

	/// <summary>
	/// Добавляет опыт, обрабатывая переходы через несколько уровней разом.
	/// Для каждого нового уровня поднимается событие <see cref="LevelUp"/>.
	/// </summary>
	public void AddXp(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		int oldLevel = Level;
		_totalXp += amount;
		int newLevel = _totalXp / XpPerLevel + 1;

		if (newLevel > oldLevel)
		{
			Level = newLevel;
			for (int lvl = oldLevel + 1; lvl <= newLevel; lvl++)
			{
				LevelUp?.Invoke(lvl);
			}
		}
	}

	/// <summary>Сбрасывает прогресс (например, при рестарте партии).</summary>
	public void Reset()
	{
		_totalXp = 0;
		Level = 1;
	}
}