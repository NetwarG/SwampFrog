using Godot;

namespace SwampFrog;

/// <summary>Общая фоновая тема меню и игровых режимов.</summary>
public partial class BackgroundMusic : AudioStreamPlayer
{
	private const string MusicPath = "res://Game/assets/audio/music/swamp_groove.wav";

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		VolumeDb = -6f;
		AudioStreamWav stream = GD.Load<AudioStreamWav>(MusicPath);
		stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		stream.LoopBegin = 0;
		stream.LoopEnd = Mathf.RoundToInt((float)(stream.GetLength() * stream.MixRate));
		Stream = stream;
		Play();
	}
}
