namespace SwampFrog;

/// <summary>Позы тела лягушки — соответствуют файлам в Game/assets/frogs.</summary>
public enum FrogPose
{
	Idle,
	Catching,
	Eating,
	Angry,
}

/// <summary>Спрайты поз тела лягушки.</summary>
public static class FrogPoses
{
	/// <summary>Путь спрайта позы в Game/assets/frogs.</summary>
	public static string TexturePath(FrogPose pose) => pose switch
	{
		FrogPose.Catching => "res://Game/assets/frogs/catching.png",
		FrogPose.Eating => "res://Game/assets/frogs/eating.png",
		FrogPose.Angry => "res://Game/assets/frogs/angry.png",
		_ => "res://Game/assets/frogs/idle.png",
	};
}
