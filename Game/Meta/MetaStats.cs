using System;

namespace SwampFrog;

/// <summary>
/// Пожизненная статистика игрока: копится между партиями и перезапусками.
/// Чистый C# класс без привязки к Godot-нодам, чтобы счётчики было легко
/// тестировать и расширять (см. docs/META.md).
/// </summary>
public sealed class MetaStats
{
	/// <summary>Сколько партий классического режима завершено.</summary>
	public int RunsPlayed { get; private set; }

	/// <summary>Сколько фруктов поймано за всё время.</summary>
	public int FruitsCaught { get; private set; }

	/// <summary>Сколько золотых фруктов поймано за всё время.</summary>
	public int GoldenCaught { get; private set; }

	/// <summary>Сколько слияний произошло в мини-игре Suika за всё время.</summary>
	public int SuikaMerges { get; private set; }

	/// <summary>Партия завершена.</summary>
	public void OnRunFinished() => RunsPlayed++;

	/// <summary>Пойман фрукт; золотой считается отдельным счётчиком.</summary>
	public void OnFruitCaught(bool golden)
	{
		FruitsCaught++;
		if (golden)
			GoldenCaught++;
	}

	/// <summary>В Suika слились два фрукта.</summary>
	public void OnSuikaMerge() => SuikaMerges++;

	/// <summary>Компактная строка для save.cfg: «runs:0,caught:0,golden:0,merges:0».</summary>
	public string ToSaveString()
	{
		return $"runs:{RunsPlayed},caught:{FruitsCaught},golden:{GoldenCaught},merges:{SuikaMerges}";
	}

	/// <summary>
	/// Разбирает строку из save.cfg. Неизвестные ключи и мусор игнорируются —
	/// старые и частично записанные сохранения не ломают статистику.
	/// </summary>
	public static MetaStats Parse(string raw)
	{
		var stats = new MetaStats();
		if (string.IsNullOrWhiteSpace(raw))
			return stats;

		foreach (string token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			int colon = token.IndexOf(':');
			if (colon < 0)
				continue;
			string key = token.Substring(0, colon).Trim();
			if (!int.TryParse(token.Substring(colon + 1).Trim(), out int value) || value < 0)
				continue;
			stats.Apply(key, value);
		}
		return stats;
	}

	private void Apply(string key, int value)
	{
		switch (key)
		{
			case "runs":
				RunsPlayed = value;
				break;
			case "caught":
				FruitsCaught = value;
				break;
			case "golden":
				GoldenCaught = value;
				break;
			case "merges":
				SuikaMerges = value;
				break;
		}
	}
}
