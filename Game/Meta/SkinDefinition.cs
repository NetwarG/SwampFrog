namespace SwampFrog;

/// <summary>Слот косметики лягушки: шляпа, перчатки или подставка.</summary>
public enum SkinSlot
{
	Hat,
	Gloves,
	Stand,
}

/// <summary>
/// Все скины-варианты. Значение перечисления — только «имя»; цена, слот и условие
/// открытия лежат в <see cref="SkinCatalog"/>, а отрисовка — в Game/Cosmetics.
/// </summary>
public enum SkinId
{
	// Шляпа
	HatNone,
	PartyHat,
	TopHat,
	FlowerHat,
	MushroomHat,

	// Перчатки
	GlovesNone,
	BoxingGloves,
	MintGloves,
	CaptainGloves,
	StripedGloves,

	// Подставка
	StandNone,
	LilyPadStand,
	CarpetStand,
	StoneStand,
	StumpStand,
}

/// <summary>
/// Описание скина: слот, имя (русское и английское), цена в слизи и пороги рекорда
/// для открытия. Цена 0 и нулевые пороги — скин доступен сразу.
/// </summary>
public sealed record SkinDefinition(
	SkinId Id,
	SkinSlot Slot,
	string NameRu,
	string NameEn,
	int Price,
	int RequiredBestClassic = 0,
	int RequiredBestSuika = 0)
{
	/// <summary>Имя скина на текущем языке интерфейса.</summary>
	public string Name => Loc.Pick(NameRu, NameEn);
}
