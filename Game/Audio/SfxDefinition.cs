using Godot;

namespace SwampFrog;

/// <summary>
/// Описание звукового эффекта: путь к WAV и параметры ненавязчивого воспроизведения
/// (базовая громкость, разброс питча, минимальный интервал между повторами).
/// Поток грузится лениво и кешируется.
/// </summary>
public sealed class SfxDefinition
{
	private AudioStream? _stream;

	public SfxDefinition(string path, float volumeDb, float pitchMin, float pitchMax, float minInterval)
	{
		Path = path;
		VolumeDb = volumeDb;
		PitchMin = pitchMin;
		PitchMax = pitchMax;
		MinInterval = minInterval;
	}

	/// <summary>Путь к WAV.</summary>
	public string Path { get; }

	/// <summary>Базовая громкость плеера: эффекты тише музыки.</summary>
	public float VolumeDb { get; }

	/// <summary>Нижняя граница случайного множителя питча.</summary>
	public float PitchMin { get; }

	/// <summary>Верхняя граница случайного множителя питча.</summary>
	public float PitchMax { get; }

	/// <summary>Минимум секунд между двумя запусками: защита от «пулемёта».</summary>
	public float MinInterval { get; }

	/// <summary>Загруженный поток или null, если ассет недоступен.</summary>
	public AudioStream? Stream => _stream ??= ResourceLoader.Load<AudioStream>(Path);
}
