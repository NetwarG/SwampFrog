using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Настройки звука игры: раздельная громкость музыки и эффектов и полное отключение
/// на шине Master. Значения хранятся в user://save.cfg: mute глушит всё сразу,
/// а ползунки задают баланс музыки и эффектов.
/// </summary>
public static class SoundSettings
{
	private const string SavePath = "user://save.cfg";
	private const string Section = "audio";
	private const string MusicKey = "music";
	private const string SfxKey = "sfx";
	private const string MutedKey = "muted";

	/// <summary>Устаревший ключ единственной громкости: переносится в музыку и эффекты.</summary>
	private const string LegacyVolumeKey = "volume";

	/// <summary>Индекс шины Master: mute покрывает музыку и эффекты сразу.</summary>
	private const int MasterBus = 0;

	/// <summary>Громкость шины, при которой звука не слышно.</summary>
	private const float SilentDb = -80f;

	private static bool _loaded;
	private static float _music = 1f;
	private static float _sfx = 1f;
	private static bool _muted;

	/// <summary>Настройки изменились: интерфейс синхронизирует вид.</summary>
	public static event Action? Changed;

	/// <summary>Громкость музыки, 0..1.</summary>
	public static float MusicVolume
	{
		get
		{
			Load();
			return _music;
		}
	}

	/// <summary>Громкость звуковых эффектов, 0..1.</summary>
	public static float SfxVolume
	{
		get
		{
			Load();
			return _sfx;
		}
	}

	/// <summary>Полностью ли отключён звук.</summary>
	public static bool Muted
	{
		get
		{
			Load();
			return _muted;
		}
	}

	/// <summary>Загружает сохранённые настройки и применяет их к шинам (один раз).</summary>
	public static void Load()
	{
		if (_loaded)
			return;
		_loaded = true;
		var config = new ConfigFile();
		if (config.Load(SavePath) == Error.Ok)
		{
			// Старые сейвы хранили одну громкость: переносим её в обе шины.
			float legacy = Mathf.Clamp((float)config.GetValue(Section, LegacyVolumeKey, 1f), 0f, 1f);
			_music = Mathf.Clamp((float)config.GetValue(Section, MusicKey, legacy), 0f, 1f);
			_sfx = Mathf.Clamp((float)config.GetValue(Section, SfxKey, legacy), 0f, 1f);
			_muted = (bool)config.GetValue(Section, MutedKey, false);
		}
		Apply();
	}

	/// <summary>Меняет громкость музыки; persist=false — без записи на диск.</summary>
	public static void SetMusicVolume(float value, bool persist = true)
	{
		Load();
		_music = Mathf.Clamp(value, 0f, 1f);
		Apply();
		if (persist)
			Save();
		Changed?.Invoke();
	}

	/// <summary>Меняет громкость эффектов; persist=false — без записи на диск.</summary>
	public static void SetSfxVolume(float value, bool persist = true)
	{
		Load();
		_sfx = Mathf.Clamp(value, 0f, 1f);
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
		AudioBuses.Ensure();
		AudioServer.SetBusMute(MasterBus, _muted);
		AudioServer.SetBusVolumeDb(AudioBuses.MusicIndex, BusDb(_music));
		AudioServer.SetBusVolumeDb(AudioBuses.SfxIndex, BusDb(_sfx));
	}

	private static float BusDb(float value) => value <= 0f ? SilentDb : Mathf.LinearToDb(value);

	private static void Save()
	{
		var config = new ConfigFile();
		config.Load(SavePath);
		config.SetValue(Section, MusicKey, _music);
		config.SetValue(Section, SfxKey, _sfx);
		config.SetValue(Section, MutedKey, _muted);
		config.Save(SavePath);
	}
}
