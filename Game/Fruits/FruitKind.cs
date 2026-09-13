namespace SwampFrog;

/// <summary>
/// Все виды фруктов в игре. Значение перечисления — это только «имя вида».
/// Вся остальная информация (размер, имя, активность в режимах) находится
/// в <see cref="FruitCatalog"/>, а графическая отрисовка — в FallingItem (DrawFruitByKind).
///
/// Как добавлять фрукт:
/// 1) добавить значение сюда;
/// 2) добавить запись в FruitCatalog.All;
/// 3) добавить метод отрисовки в FallingItem.DrawFruitByKind.
/// Как убрать фрукт с поля: поменять ActiveInClassic в FruitCatalog.
/// </summary>
public enum FruitKind
{
	Cherry,
	Strawberry,
	Grape,
	Mandarin,
	Apple,
	Pear,
	Peach,
	Pineapple,
	Melon,
	Watermelon,
}