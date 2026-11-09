using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Проигрыватель звуковых эффектов: пул голосов на шине SFX, ограничение частоты
/// повторов и случайный питч. Работает и на паузе (ProcessMode.Always), поэтому
/// звуки кнопок меню и паузы слышны. Доступ — через <see cref="Sfx"/>.
/// </summary>
public partial class SfxPlayer : Node
{
	/// <summary>Число голосов: ограничивает плотность звучания.</summary>
	private const int VoiceCount = 12;

	/// <summary>Активный проигрыватель: ставится в _Ready, снимается в _ExitTree.</summary>
	public static SfxPlayer? Instance { get; private set; }

	/// <summary>Сколько эффектов реально запущено (после фильтра cooldown): для диагностики.</summary>
	public int PlayCount { get; private set; }

	private readonly AudioStreamPlayer[] _voices = new AudioStreamPlayer[VoiceCount];
	private readonly float[] _cooldown = new float[Enum.GetValues<SfxId>().Length];
	private readonly RandomNumberGenerator _rng = new();
	private int _next;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		AudioBuses.Ensure();
		for (int i = 0; i < VoiceCount; i++)
		{
			var voice = new AudioStreamPlayer
			{
				Name = $"Voice{i}",
				Bus = AudioBuses.SfxName,
				ProcessMode = ProcessModeEnum.Always
			};
			AddChild(voice);
			_voices[i] = voice;
		}
		_rng.Randomize();
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < _cooldown.Length; i++)
		{
			if (_cooldown[i] > 0f)
			{
				_cooldown[i] = Mathf.Max(0f, _cooldown[i] - (float)delta);
			}
		}
	}

	/// <summary>
	/// Проигрывает эффект, если он не сработал только что. pitchScale — множитель к
	/// случайному питчу (питч-«лестница» комбо и слияний Suika).
	/// </summary>
	public void Play(SfxId id, float pitchScale = 1f)
	{
		SfxDefinition? definition = SoundBank.Find(id);
		if (definition?.Stream == null)
		{
			return;
		}
		int index = (int)id;
		if (_cooldown[index] > 0f)
		{
			return;
		}
		_cooldown[index] = definition.MinInterval;
		PlayCount++;

		AudioStreamPlayer voice = NextVoice();
		voice.Stream = definition.Stream;
		voice.VolumeDb = definition.VolumeDb;
		voice.PitchScale = (definition.PitchMin + (definition.PitchMax - definition.PitchMin) * _rng.Randf()) * pitchScale;
		voice.Play();
	}

	/// <summary>Свободный голос по кругу; если все заняты — забирает самый старый.</summary>
	private AudioStreamPlayer NextVoice()
	{
		for (int i = 0; i < VoiceCount; i++)
		{
			int index = (_next + i) % VoiceCount;
			if (!_voices[index].Playing)
			{
				_next = (index + 1) % VoiceCount;
				return _voices[index];
			}
		}
		AudioStreamPlayer stolen = _voices[_next];
		_next = (_next + 1) % VoiceCount;
		return stolen;
	}
}
