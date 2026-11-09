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

	/// <summary>Читает статистику профиля; отсутствие секции или ключа даёт пустые счётчики.</summary>
	public static MetaStats LoadStats()
	{
		var config = new ConfigFile();
		if (config.Load(Path) != Error.Ok)
			return new MetaStats();
		return MetaStats.Parse(config.GetValue(Section, StatsKey, string.Empty).ToString());
	}

	/// <summary>Записывает статистику профиля, не трогая остальные секции save.cfg.</summary>
	public static void SaveStats(MetaStats stats)
	{
		var config = new ConfigFile();
		config.Load(Path);
		config.SetValue(Section, StatsKey, stats.ToSaveString());
		config.Save(Path);
	}
}
