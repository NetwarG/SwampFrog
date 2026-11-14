using System.Collections.Generic;

namespace SwampFrog;

/// <summary>
/// Таблица звуковых эффектов: идентификатор → файл и параметры воспроизведения.
/// Значения подобраны так, чтобы эффекты не заглушали музыку и не раздражали:
/// короткие звуки, тише музыки, с ограничением частоты повторов.
/// </summary>
public static class SoundBank
{
	private const string Dir = "res://Game/assets/audio/sfx/";

	private static readonly Dictionary<SfxId, SfxDefinition> Definitions = new()
	{
		[SfxId.Catch] = new(Dir + "catch.wav", -10f, 0.96f, 1.06f, 0.05f),
		[SfxId.Golden] = new(Dir + "golden.wav", -8f, 0.99f, 1.01f, 0.10f),
		[SfxId.Heal] = new(Dir + "heal.wav", -8f, 0.99f, 1.02f, 0.20f),
		[SfxId.Damage] = new(Dir + "damage.wav", -9f, 0.98f, 1.02f, 0.15f),
		[SfxId.Deflect] = new(Dir + "deflect.wav", -12f, 0.95f, 1.08f, 0.06f),
		[SfxId.LevelUp] = new(Dir + "level_up.wav", -8f, 0.99f, 1.01f, 0.30f),
		[SfxId.PerkOffer] = new(Dir + "perk_offer.wav", -9f, 0.99f, 1.01f, 0.30f),
		[SfxId.PerkPick] = new(Dir + "perk_pick.wav", -9f, 0.99f, 1.02f, 0.15f),
		[SfxId.Combo] = new(Dir + "combo.wav", -9f, 0.98f, 1.05f, 0.12f),
		[SfxId.GameOver] = new(Dir + "game_over.wav", -8f, 0.99f, 1.01f, 1.00f),
		[SfxId.SuikaThrow] = new(Dir + "suika_throw.wav", -11f, 0.96f, 1.06f, 0.08f),
		[SfxId.SuikaMerge] = new(Dir + "suika_merge.wav", -9f, 0.97f, 1.03f, 0.04f),
		[SfxId.UiTap] = new(Dir + "ui_tap.wav", -14f, 0.98f, 1.03f, 0.06f),
		[SfxId.FlyCatch] = new(Dir + "fly_catch.wav", -11f, 0.97f, 1.05f, 0.06f),
	};

	/// <summary>Все идентификаторы эффектов с описаниями (для проверок и прогрева).</summary>
	public static IReadOnlyDictionary<SfxId, SfxDefinition> All => Definitions;

	/// <summary>Описание эффекта или null, если он не заведён.</summary>
	public static SfxDefinition? Find(SfxId id) => Definitions.TryGetValue(id, out SfxDefinition? definition) ? definition : null;
}
