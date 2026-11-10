using System;
using System.Collections.Generic;
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
	private const string SkinsKey = "skins";
	private const string UpgradesKey = "upgrades";
	private const string HatKey = "skin_hat";
	private const string GlovesKey = "skin_gloves";
	private const string StandKey = "skin_stand";

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

	/// <summary>Читает список купленных/открытых скинов; неизвестные id игнорируются.</summary>
	public static List<SkinId> LoadOwnedSkins()
	{
		var result = new List<SkinId>();
		var config = new ConfigFile();
		if (config.Load(Path) != Error.Ok)
			return result;
		foreach (string token in config.GetValue(Section, SkinsKey, string.Empty).ToString()
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (Enum.TryParse(token, out SkinId id) && !result.Contains(id))
				result.Add(id);
		}
		return result;
	}

	/// <summary>Читает выбранный скин слота; неизвестный id даёт скин по умолчанию.</summary>
	public static SkinId LoadSelectedSkin(SkinSlot slot)
	{
		var config = new ConfigFile();
		if (config.Load(Path) != Error.Ok)
			return SkinCatalog.DefaultFor(slot).Id;
		string raw = config.GetValue(Section, KeyFor(slot), string.Empty).ToString().Trim();
		return Enum.TryParse(raw, out SkinId id) ? SkinCatalog.Normalize(slot, id) : SkinCatalog.DefaultFor(slot).Id;
	}

	/// <summary>Читает уровни постоянных улучшений; неизвестные id игнорируются.</summary>
	public static List<KeyValuePair<UpgradeId, int>> LoadUpgrades()
	{
		var result = new List<KeyValuePair<UpgradeId, int>>();
		var config = new ConfigFile();
		if (config.Load(Path) != Error.Ok)
			return result;
		foreach (string token in config.GetValue(Section, UpgradesKey, string.Empty).ToString()
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			int colon = token.IndexOf(':');
			if (colon < 0)
				continue;
			if (Enum.TryParse(token.Substring(0, colon).Trim(), out UpgradeId id)
				&& int.TryParse(token.Substring(colon + 1).Trim(), out int level) && level > 0)
			{
				result.Add(new KeyValuePair<UpgradeId, int>(id, level));
			}
		}
		return result;
	}

	/// <summary>Записывает статистику, слизь, скины и улучшения одним проходом.</summary>
	public static void Save(MetaStats stats, int coins, IReadOnlyCollection<SkinId> owned,
		SkinId hat, SkinId gloves, SkinId stand, IReadOnlyDictionary<UpgradeId, int> upgrades)
	{
		var config = new ConfigFile();
		config.Load(Path);
		config.SetValue(Section, StatsKey, stats.ToSaveString());
		config.SetValue(Section, CoinsKey, coins);
		var ids = new List<string>();
		foreach (SkinId id in owned) ids.Add(id.ToString());
		ids.Sort();
		config.SetValue(Section, SkinsKey, string.Join(",", ids));
		config.SetValue(Section, HatKey, hat.ToString());
		config.SetValue(Section, GlovesKey, gloves.ToString());
		config.SetValue(Section, StandKey, stand.ToString());
		var levels = new List<string>();
		foreach (KeyValuePair<UpgradeId, int> pair in upgrades)
			levels.Add($"{pair.Key}:{pair.Value}");
		levels.Sort();
		config.SetValue(Section, UpgradesKey, string.Join(",", levels));
		config.Save(Path);
	}

	private static string KeyFor(SkinSlot slot) => slot switch
	{
		SkinSlot.Gloves => GlovesKey,
		SkinSlot.Stand => StandKey,
		_ => HatKey,
	};
}

