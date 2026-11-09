using System;
using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Локализация интерфейса: единая точка доступа к строкам по ключу (<see cref="T(string)"/>)
/// и к переводам, лежащим рядом с данными (<see cref="Pick(string, string)"/>).
///
/// Язык хранится в user://save.cfg (секция locale) и применяется сразу ко всему интерфейсу
/// через событие <see cref="Changed"/>. При первом запуске язык определяется по локали ОС
/// (ru* — русский, иначе английский) и не сохраняется, пока игрок не выберет язык вручную.
/// Тексты двух языков лежат в <see cref="LocText"/>.
/// </summary>
public static class Loc
{
	private const string SavePath = "user://save.cfg";
	private const string Section = "locale";
	private const string LanguageKey = "language";

	private static bool _initialized;
	private static Language _current = Language.Russian;

	/// <summary>Язык интерфейса изменился: подписчики перечитывают свои подписи.</summary>
	public static event Action? Changed;

	/// <summary>Текущий язык интерфейса. При первом обращении загружает сохранённый выбор.</summary>
	public static Language Current
	{
		get
		{
			Initialize();
			return _current;
		}
	}

	/// <summary>Текущий язык интерфейса — английский.</summary>
	public static bool IsEnglish => Current == Language.English;

	/// <summary>Читает сохранённый язык или определяет его по локали системы (один раз).</summary>
	public static void Initialize()
	{
		if (_initialized)
			return;
		_initialized = true;
		_current = LoadSaved() ?? DetectFromSystem();
	}

	/// <summary>
	/// Меняет язык и оповещает интерфейс. При <paramref name="persist"/> = true выбор
	/// сохраняется в save.cfg (тесты вызывают с false, чтобы не трогать файл игрока).
	/// </summary>
	public static void SetLanguage(Language language, bool persist = true)
	{
		Initialize();
		if (_current == language)
			return;
		_current = language;
		if (persist)
			Save(language);
		Changed?.Invoke();
	}

	/// <summary>Переключает язык между русским и английским.</summary>
	public static void Toggle()
	{
		SetLanguage(IsEnglish ? Language.Russian : Language.English);
	}

	/// <summary>Строка интерфейса по ключу. Неизвестный ключ возвращается как есть — видно в тестах.</summary>
	public static string T(string key)
	{
		Dictionary<string, string> map = IsEnglish ? LocText.En : LocText.Ru;
		return map.TryGetValue(key, out string? value) ? value : key;
	}

	/// <summary>Строка интерфейса с подстановкой значений (плейсхолдеры вида {0}).</summary>
	public static string T(string key, params object[] args)
	{
		return string.Format(T(key), args);
	}

	/// <summary>Выбор перевода для строк, лежащих рядом с данными (имена, слоганы, эффекты).</summary>
	public static string Pick(string ru, string en)
	{
		return IsEnglish ? en : ru;
	}

	/// <summary>Выбор перевода из парных массивов (например, эффекты уровней перка).</summary>
	public static string[] Pick(string[] ru, string[] en)
	{
		return IsEnglish ? en : ru;
	}

	private static Language? LoadSaved()
	{
		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
			return null;
		string raw = config.GetValue(Section, LanguageKey, string.Empty).AsString().Trim();
		if (raw.Equals("en", StringComparison.OrdinalIgnoreCase))
			return Language.English;
		if (raw.Equals("ru", StringComparison.OrdinalIgnoreCase))
			return Language.Russian;
		return null;
	}

	private static Language DetectFromSystem()
	{
		string locale = OS.GetLocale();
		return locale.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? Language.Russian : Language.English;
	}

	private static void Save(Language language)
	{
		var config = new ConfigFile();
		config.Load(SavePath);
		config.SetValue(Section, LanguageKey, language == Language.English ? "en" : "ru");
		config.Save(SavePath);
	}
}
