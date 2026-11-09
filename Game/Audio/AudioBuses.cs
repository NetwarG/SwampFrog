using Godot;

namespace SwampFrog;

/// <summary>
/// Звуковые шины игры: «Music» и «SFX» идут в Master, поэтому общий mute и громкость
/// (см. <see cref="SoundSettings"/>) действуют на музыку и эффекты сразу.
/// Шины создаются в рантайме: отдельного bus layout в проекте нет.
/// </summary>
public static class AudioBuses
{
	/// <summary>Имя шины музыки.</summary>
	public const string MusicName = "Music";

	/// <summary>Имя шины звуковых эффектов.</summary>
	public const string SfxName = "SFX";

	/// <summary>Корневая шина: в неё посылают Music и SFX.</summary>
	private const string MasterName = "Master";

	/// <summary>Индекс шины музыки (после <see cref="Ensure"/>).</summary>
	public static int MusicIndex { get; private set; }

	/// <summary>Индекс шины эффектов (после <see cref="Ensure"/>).</summary>
	public static int SfxIndex { get; private set; }

	/// <summary>Создаёт недостающие шины. Повторный вызов ничего не меняет.</summary>
	public static void Ensure()
	{
		MusicIndex = EnsureBus(MusicName);
		SfxIndex = EnsureBus(SfxName);
	}

	private static int EnsureBus(string name)
	{
		int index = AudioServer.GetBusIndex(name);
		if (index >= 0)
		{
			return index;
		}
		index = AudioServer.BusCount;
		AudioServer.AddBus(index);
		AudioServer.SetBusName(index, name);
		AudioServer.SetBusSend(index, MasterName);
		return index;
	}
}
