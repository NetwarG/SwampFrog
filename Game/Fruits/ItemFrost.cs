namespace SwampFrog;

/// <summary>Вид обездвиживания предмета: лёд заморозки или камень вместо мусора.</summary>
public enum ItemFrost
{
	/// <summary>Предмет движется как обычно.</summary>
	None,

	/// <summary>Ледяная кромка «Взгляда василиска».</summary>
	Ice,

	/// <summary>Мусор окаменел и стал безопасным.</summary>
	Stone,
}
