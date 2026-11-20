using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Награда фруктами в запас: количество за ролик с рекламой и случайный набор видов
/// из каталога. Чистые функции без узлов, чтобы правила было легко проверить тестом.
/// </summary>
public static class FruitReward
{
	/// <summary>Сколько случайных фруктов выдаётся за досмотр ролика с наградой.</summary>
	public const int AdCount = 30;

	/// <summary>Случайный набор из count фруктов: каждый вид каталога равновероятен.</summary>
	public static Dictionary<FruitKind, int> RandomBag(int count, RandomNumberGenerator rng)
	{
		var bag = new Dictionary<FruitKind, int>();
		FruitSpec[] pool = FruitCatalog.All;
		if (count <= 0 || pool.Length == 0)
			return bag;

		for (int i = 0; i < count; i++)
		{
			FruitKind kind = pool[rng.RandiRange(0, pool.Length - 1)].Kind;
			bag[kind] = (bag.TryGetValue(kind, out int amount) ? amount : 0) + 1;
		}
		return bag;
	}
}
