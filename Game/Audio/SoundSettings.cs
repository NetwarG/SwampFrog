using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Настройки звука игры: громкость и полное отключение на шине Master.
/// Значения хранятся в user://save.cfg и применяются ко всем звукам сразу.
/// </summary>
public static class SoundSettings
{
	private const string SavePath = "user://save.cfg";
	private const string Section = "audio";
	private const string VolumeKey = "volume";
	private const string MutedKey = "muted";

	/// <summary>Индекс шины Master: громкость и mute покрывают музыку и эффекты.</summary>
	private const int MasterBus = 0;

	/// <summary>Тишина на шине: когда громкость нулевая.</summary>
	private const float SilentDb = -80f;

	private static bool _loaded;
	private static float _volume = 1f;
	private static bool _muted;

	/// <summary>Настройки изменились: интерфейс синхронизирует вид.</summary>
	public static event Action? Changed;

	public static float Volume
	{
		get
		{
			Load();
			return _volume;
		}
	}

	public static bool Muted
	{
		get
		{
			Load();
			return _muted;
		}
	}

	/// <summary>Загружает сохранённые настройки и применяет их к шине (один раз).</summary>
	public static void Load()
	{
		if (_loaded)
			return;
		_loaded = true;
		var config = new ConfigFile();
		if (config.Load(SavePath) == Error.Ok)
		{
			_volume = Mathf.Clamp((float)config.GetValue(Section, VolumeKey, 1f), 0f, 1f);
			_muted = (bool)config.GetValue(Section, MutedKey, false);
		}
		Apply();
	}

	/// <summary>Меняет громкость; persist=false — без записи на диск (перетаскивание ползунка).</summary>
	public static void SetVolume(float value, bool persist = true)
	{
		Load();
		_volume = Mathf.Clamp(value, 0f, 1f);
		Apply();
		if (persist)
			Save();
		Changed?.Invoke();
	}

	/// <summary>Включает или полностью отключает звук.</summary>
	public static void SetMuted(bool muted)
	{
		Load();
		_muted = muted;
		Apply();
		Save();
		Changed?.Invoke();
	}

	public static void ToggleMute() => SetMuted(!Muted);

	private static void Apply()
	{
		AudioServer.SetBusMute(MasterBus, _muted);
		AudioServer.SetBusVolumeDb(MasterBus, _muted || _volume <= 0f ? SilentDb : Mathf.LinearToDb(_volume));
	}

	private static void Save()
	{
		var config = new ConfigFile();
		config.Load(SavePath);
		config.SetValue(Section, VolumeKey, _volume);
		config.SetValue(Section, MutedKey, _muted);
		config.Save(SavePath);
	}
}
