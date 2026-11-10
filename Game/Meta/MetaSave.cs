using Godot;

namespace SwampFrog;

/// <summary>
/// Чтение и запись постоянного профиля в user://save.cfg (секция meta).
/// Отдельный файл, чтобы доступ к сохранению профиля был в одном месте (см. docs/META.md).
/// </summary>
public static class MetaSave
{
	private const string Path = "user://save.cfg";
	private const string Section = "meta";
	private const string StatsKey = "stats";
	private const string CoinsKey = "coins";

	/// <summary>Читает статистику профиля; отсутствие секции или ключа даёт пустые счётчики.</summary>
	public static MetaStats LoadStats()
	{
		var config = new ConfigFile();
		if (config.Load(Path) != Error.Ok)
			return new MetaStats();
		return MetaStats.Parse(config.GetValue(Section, StatsKey, string.Empty).ToString());
	}

	/// <summary>Читает баланс слизи; отсутствие секции или ключа даёт ноль.</summary>
	public static int LoadCoins()
	{
		var config = new ConfigFile();
		if (config.Load(Path) != Error.Ok)
			return 0;
		return (int)config.GetValue(Section, CoinsKey, 0);
	}

	/// <summary>Записывает статистику и баланс слизи одним проходом, не трогая остальные секции.</summary>
	public static void Save(MetaStats stats, int coins)
	{
		var config = new ConfigFile();
		config.Load(Path);
		config.SetValue(Section, StatsKey, stats.ToSaveString());
		config.SetValue(Section, CoinsKey, coins);
		config.Save(Path);
	}
}
