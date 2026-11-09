namespace SwampFrog;

/// <summary>
/// Точка вызова звуковых эффектов: короткая обёртка над <see cref="SfxPlayer"/>.
/// Если проигрывателя в дереве нет (например, юнит-контекст), вызов просто игнорируется.
/// </summary>
public static class Sfx
{
	/// <summary>Проигрывает эффект. pitchScale — множитель к случайному питчу.</summary>
	public static void Play(SfxId id, float pitchScale = 1f) => SfxPlayer.Instance?.Play(id, pitchScale);
}
