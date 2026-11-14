using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Рой мух классического режима: выпускает мух по таймеру, выдаёт им цели-фрукты,
/// следит за порчей и ловит мух ладонями лягушки. Обновляется только из <see cref="Main"/>
/// в партии, поэтому на паузе и в меню жизни мух нет. Настройка — <see cref="FlyTuning"/>.
/// </summary>
public partial class FlySwarm : Node2D
{
	/// <summary>Корень игры: предметы, ладони, эффекты, счёт и профиль.</summary>
	public Main? Game { get; set; }

	private readonly List<Fly> _flies = new();

	/// <summary>Отсчёт до следующей мухи: стартует с паузы «первые секунды без мух».</summary>
	private float _spawnTimer = FlyTuning.FirstFlyDelay;

	/// <summary>Сколько мух сейчас в поле: для проверок.</summary>
	public int ActiveCount => _flies.Count;

	/// <summary>Убирает всех мух: рестарт, конец партии, выход в меню.</summary>
	public void Clear()
	{
		foreach (Fly fly in _flies)
		{
			if (IsInstanceValid(fly))
				fly.QueueFree();
		}
		_flies.Clear();
		_spawnTimer = FlyTuning.FirstFlyDelay;
	}

	/// <summary>Выпускает муху в поле: спавн и проверки используют один путь.</summary>
	public Fly SpawnFly(Vector2 at)
	{
		var fly = new Fly { Position = at };
		AddChild(fly);
		_flies.Add(fly);
		return fly;
	}

	/// <summary>
	/// Кадр роя: выпуск, полёт, порча фруктов и ловля. worldFrozen — поле обездвижено
	/// заморозкой перков: летящие мухи замирают, а новые не появляются.
	/// </summary>
	public void Update(float dt, int score, Vector2 fieldSize, CircleHitbox[]? hands, bool worldFrozen)
	{
		if (Game == null)
			return;

		Cleanup();

		if (!worldFrozen)
		{
			_spawnTimer -= dt;
			if (_spawnTimer <= 0f && _flies.Count < FlyTuning.MaxFlies(score))
			{
				_spawnTimer = FlyTuning.SpawnInterval(score, Game.GameRng);
				Spawn();
			}
		}

		for (int i = _flies.Count - 1; i >= 0; i--)
		{
			Fly fly = _flies[i];
			if (!IsInstanceValid(fly))
			{
				_flies.RemoveAt(i);
				continue;
			}

			if (!worldFrozen)
			{
				AssignTarget(fly);
				if (fly.Update(dt, fieldSize))
					SpoilFruit(fly);
			}

			if (CatchByHand(fly, hands))
				_flies.RemoveAt(i);
		}
	}

	/// <summary>Выпуск у верхней кромки поля, ниже HUD, в случайном столбце.</summary>
	private void Spawn()
	{
		Vector2 size = Game!.ViewSize;
		float right = Mathf.Max(FlyTuning.SideMargin + 1f, size.X - FlyTuning.SideMargin);
		float x = Game.GameRng.RandfRange(FlyTuning.SideMargin, right);
		SpawnFly(new Vector2(x, FlyTuning.TopMargin));
	}

	/// <summary>Свободной мухе ищется ближайший фрукт: занятые цели не дублируются.</summary>
	private void AssignTarget(Fly fly)
	{
		if (fly.Target != null || fly.IsSitting)
			return;
		FallingItem? fruit = NearestFruit(fly.GlobalPosition);
		if (fruit != null)
			fly.SetTarget(fruit);
	}

	/// <summary>Ближайший фрукт, который можно испортить и которого ещё никто не занял.</summary>
	private FallingItem? NearestFruit(Vector2 from)
	{
		Node2D? items = Game!.ItemsNode;
		if (items == null)
			return null;
		FallingItem? best = null;
		float bestDist = float.MaxValue;
		foreach (Node child in items.GetChildren())
		{
			if (child is not FallingItem item || !CanSpoil(item) || Reserved(item))
				continue;
			float dist = item.GlobalPosition.DistanceSquaredTo(from);
			if (dist < bestDist)
			{
				bestDist = dist;
				best = item;
			}
		}
		return best;
	}

	/// <summary>Фрукт можно испортить: он на поле, обычный, ещё не протух и не обездвижен.</summary>
	private static bool CanSpoil(FallingItem item) =>
		!item.IsCaught && !item.IsQueuedForDeletion() && !item.Rotten && !item.Frozen && !item.ManualPhysics
		&& item.ItemType == ItemType.Fruit && item.GlobalPosition.Y >= 0f;

	/// <summary>Фрукт уже выбран целью другой мухи.</summary>
	private bool Reserved(FallingItem item)
	{
		foreach (Fly fly in _flies)
		{
			if (fly.Target == item)
				return true;
		}
		return false;
	}

	/// <summary>Порча фрукта: отметка «протух» и зелёная вспышка плесени.</summary>
	private void SpoilFruit(Fly fly)
	{
		FallingItem? fruit = fly.Target;
		if (fruit == null || !IsInstanceValid(fruit) || fruit.IsQueuedForDeletion())
		{
			fly.ClearTarget();
			return;
		}
		fruit.Rotten = true;
		Vector2 at = fruit.GlobalPosition;
		Game!.HudNode?.SpawnPopup(at, Loc.T(L.PopRotten));
		Game.Effects?.SpawnSparks(at, FlyStyle.Mold, 10, 175f, 0.5f);
		fly.ClearTarget();
	}

	/// <summary>Ладонь коснулась мухи: мгновенная награда без занятия руки.</summary>
	private bool CatchByHand(Fly fly, CircleHitbox[]? hands)
	{
		if (hands == null)
			return false;
		foreach (CircleHitbox hand in hands)
		{
			if (!fly.Hitbox.Overlaps(hand))
				continue;
			CatchFly(fly);
			return true;
		}
		return false;
	}

	/// <summary>Поимка мухи: очки, счётчик профиля, эффект и звук.</summary>
	private void CatchFly(Fly fly)
	{
		Vector2 at = fly.GlobalPosition;
		Game!.AddScore(FlyTuning.CatchScore);
		Game.RegisterFlyCaught();
		Game.HudNode?.SpawnPopup(at, Loc.T(L.PopFly, FlyTuning.CatchScore));
		Game.Effects?.SpawnShockwave(at, 46f, FlyStyle.Mold, 0.28f, 4f, false);
		Game.Effects?.SpawnSparks(at, FlyStyle.Mold, 8, 200f, 0.4f);
		Sfx.Play(SfxId.FlyCatch);
		if (IsInstanceValid(fly))
			fly.QueueFree();
	}

	/// <summary>Убирает мух, которых уже освободили снаружи.</summary>
	private void Cleanup()
	{
		for (int i = _flies.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(_flies[i]))
				_flies.RemoveAt(i);
		}
	}
}
