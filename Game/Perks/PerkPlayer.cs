using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>Рантайм-логика перков забега: таймеры, награды, взрывы, заморозки и выбор перка.
/// Мир читается через публичные доступы <see cref="Main"/>, «чистая» математика — в <see cref="PerkEffects"/>.</summary>
public sealed class PerkPlayer
{
	/// <summary>Ссылка на корень игры (устанавливается из Main._Ready).</summary>
	public Main? Game { get; set; }

	private readonly PerkState _perks = new();

	/// <summary>Отсчёт авто-ловли «Языка-молнии».</summary>
	private float _autoCatchTimer;
	/// <summary>Отсчёт до заморозки «Взгляда василиска».</summary>
	private float _gazeTimer;
	/// <summary>Заморозка всего поля («Зона замедления» ур. 5 при золотом).</summary>
	private float _worldFreezeTimer;
	/// <summary>Текущая заморозка «Взгляда василиска».</summary>
	private float _gazeFreezeTimer;
	/// <summary>Неуязвимость («Панцирь» ур. 5, возрождение).</summary>
	private float _invulnTimer;
	/// <summary>Очки комбо текущей серии.</summary>
	private int _combo;
	/// <summary>Сколько возрождений осталось («Вторая жизнь»).</summary>
	private int _revivesLeft;
	/// <summary>Фрукты с момента последнего золотого (гарантия «Золотой лихорадки»).</summary>
	private int _fruitsSinceLastGolden;

	/// <summary>Состояние перков (уровни и занятые слоты).</summary>
	public PerkState State => _perks;

	// ---------- Сброс забега ----------

	public void Reset()
	{
		_perks.Reset();
		_revivesLeft = PerkEffects.ReviveCount(_perks);
		_combo = 0;
		_fruitsSinceLastGolden = 0;
		_autoCatchTimer = PerkEffects.LightningInterval(_perks);
		_gazeTimer = PerkEffects.GazeInterval(_perks);
		_gazeFreezeTimer = 0f;
		_worldFreezeTimer = 0f;
		_invulnTimer = 0f;
	}

	// ---------- Выбор перка ----------

	/// <summary>Обработка уровня жабы: предложить перк или показать уведомление.</summary>
	public void OnLevelUp(int newLevel)
	{
		if (Game == null || !Game.IsPlaying)
		{
			return;
		}
		if (_perks.WantsOffer(newLevel))
		{
			OpenSelection();
			return;
		}
		Game.HudNode?.ShowLevelUp(newLevel);
	}

	/// <summary>
	/// Останавливает игру и показывает выбор из трёх случайных перков.
	/// Игра возобновляется только после выбора (OnPicked).
	/// </summary>
	private void OpenSelection()
	{
		if (Game == null)
		{
			return;
		}
		PerkId[] offers = _perks.OfferCandidates(3, Game.GameRng);
		if (offers.Length == 0)
		{
			// Все перки максимальны или кончились слоты — поздравляем с уровнем.
			Game.HudNode?.ShowLevelUp(Game.Xp.Level);
			return;
		}
		Game.StartPerkPause();
		ShowCards(Game.Xp.Level, offers);
	}

	private void ShowCards(int level, PerkId[] offers)
	{
		if (Game == null)
		{
			return;
		}
		var cards = new List<PerkOffer>();
		foreach (PerkId id in offers)
		{
			PerkDefinition def = PerkCatalog.Find(id);
			int lvl = _perks.LevelOf(id);
			cards.Add(new PerkOffer(id, def.Name, def.Tagline, lvl, def.LevelEffects[lvl], def.MaxLevel, def.Accent));
		}
		Game.HudNode?.ShowPerkSelection(level, cards.ToArray());
	}

	/// <summary>Игрок выбрал перк: прокачиваем, применяем мгновенные эффекты и снимаем паузу.</summary>
	public void OnPicked(PerkId id)
	{
		if (Game == null || !Game.IsChoosingPerk)
		{
			return;
		}
		_perks.Increment(id);

		// «Панцирь» тут же даёт новую жизнь (до нового максимума).
		if (id == PerkId.Shell)
		{
			Game.SetLivesFromPerk(Mathf.Min(Game.MaxLives, Game.Lives + 1));
		}

		// Возрождения и таймеры пересчитываются под новый набор перков.
		_revivesLeft = PerkEffects.ReviveCount(_perks);
		_gazeTimer = PerkEffects.GazeInterval(_perks);
		_autoCatchTimer = PerkEffects.LightningInterval(_perks);

		Game.SetMaxHeartsFromPerk();
		Game.HudNode?.HidePerkSelection();
		Game.EndPerkPause();
		Game.HudNode?.SetXp(Game.Xp.Level, Game.Xp.LevelProgress);
	}

	// ---------- Ежекадровое обновление ----------

	/// <summary>Обновляет модификаторы лягушки и таймеры перков.</summary>
	public void Update(float dt, Frog? frog)
	{
		if (frog != null)
		{
			frog.CatchRadiusMultiplier = PerkEffects.CatchRadiusMultiplier(_perks);
			frog.ExtendSpeedMultiplier = PerkEffects.ExtendSpeedMultiplier(_perks);
			frog.RetractSpeedMultiplier = PerkEffects.RetractSpeedMultiplier(_perks);
			frog.MultiCatch = PerkEffects.MultiCatch(_perks);
		}

		if (_invulnTimer > 0f)
		{
			_invulnTimer = Mathf.Max(0f, _invulnTimer - dt);
		}
		if (_worldFreezeTimer > 0f)
		{
			_worldFreezeTimer = Mathf.Max(0f, _worldFreezeTimer - dt);
		}
		if (_gazeFreezeTimer > 0f)
		{
			_gazeFreezeTimer = Mathf.Max(0f, _gazeFreezeTimer - dt);
		}

		// Взгляд василиска: периодическая заморозка всего.
		float gaze = PerkEffects.GazeInterval(_perks);
		if (gaze > 0f)
		{
			_gazeTimer -= dt;
			if (_gazeTimer <= 0f)
			{
				_gazeTimer = gaze;
				_gazeFreezeTimer = Mathf.Max(_gazeFreezeTimer, PerkEffects.GazeFreezeDuration(_perks));
			}
		}

		// Язык-молния: авто-ловля ближайшего фрукта.
		float lightning = PerkEffects.LightningInterval(_perks);
		if (lightning > 0f)
		{
			_autoCatchTimer -= dt;
			if (_autoCatchTimer <= 0f)
			{
				_autoCatchTimer = lightning;
				LightningAutoCatch();
			}
		}
	}

	/// <summary>Применяет к предмету заморозку/замедление/магнит перков в этом кадре.</summary>
	public void ApplyWorldEffect(FallingItem item, float dt, Vector2 viewSize, Vector2[]? hands)
	{
		bool frozen = IsItemFrozen(item);
		item.Frozen = frozen;
		if (!frozen)
		{
			ApplySlowZone(item, viewSize, dt);
			ApplyMagnet(item, hands);
		}
	}

	// ---------- Спавн (Золотая лихорадка / Зона замедления) ----------

	/// <summary>Итоговый порог шанса золотого фрукта при спавне.</summary>
	public float GoldenSpawnChance()
	{
		return 0.14f + PerkEffects.GoldenChanceBonus(_perks);
	}

	/// <summary>Сейчас должен выпасть гарантированный золотой фрукт (каждый N-й).</summary>
	public bool IsGoldenGuaranteed()
	{
		int n = PerkEffects.GoldenEveryN(_perks);
		return n > 0 && _fruitsSinceLastGolden >= n;
	}

	/// <summary>Множитель скорости золотого фрукта (ур. 4 «Золотой лихорадки»).</summary>
	public float GoldenFallSpeedFactor()
	{
		return PerkEffects.GoldenFallsSlower(_perks) ? 0.75f : 1f;
	}

	/// <summary>Учёт спавна предмета: счётчик золотых и заморозка «Зоны замедления» ур. 5.</summary>
	public void OnItemSpawned(ItemType type)
	{
		if (type == ItemType.Fruit)
		{
			_fruitsSinceLastGolden++;
		}
		else if (type == ItemType.GoldenFruit)
		{
			_fruitsSinceLastGolden = 0;
			float freeze = PerkEffects.GoldFreezeDuration(_perks);
			if (freeze > 0f)
			{
				_worldFreezeTimer = Mathf.Max(_worldFreezeTimer, freeze);
			}
		}
	}

	// ---------- Урон, мусор, возрождение ----------

	/// <summary>Ладонь коснулась мусора: отскок, «камень» василиска или урон.</summary>
	public void HandleTrashTouch(FallingItem item)
	{
		if (Game == null)
		{
			return;
		}
		// Каменный (замороженный) мусор василиска безопасен и разбивается.
		if (item.Frozen)
		{
			Game.HudNode?.SpawnPopup(item.GlobalPosition, "Камень!");
			item.QueueFree();
			return;
		}

		float chance = PerkEffects.TrashDeflectChance(_perks);
		if (chance > 0f && Game.GameRng.Randf() < chance)
		{
			// Отброс: мусор улетает вверх, урона нет.
			item.Velocity = new Vector2(item.Velocity.X * 0.4f, -Mathf.Abs(item.Velocity.Y) - 170f);
			Game.HudNode?.SpawnPopup(item.GlobalPosition, "Отскок!");
			return;
		}

		TakeDamage();
		item.QueueFree();
	}

	/// <summary>Фрукт/золотой упал мимо: штраф за промах и сброс комбо.</summary>
	public void OnItemMissed(FallingItem item)
	{
		if (item.ItemType == ItemType.Fruit || item.ItemType == ItemType.GoldenFruit)
		{
			Game?.HudNode?.SpawnPopup(item.GlobalPosition, "Мимо!");
			TakeDamage();
			if (!PerkEffects.ComboKeepsOnMiss(_perks))
			{
				_combo = 0;
			}
		}
		item.QueueFree();
	}

	/// <summary>Урон по лягушке: неуязвимость, «Панцирь», возрождение.</summary>
	public void TakeDamage()
	{
		if (Game == null || _invulnTimer > 0f)
		{
			return;
		}
		Game.ReduceLife();
		if (PerkEffects.ShellInvulnerableOnDamage(_perks))
		{
			_invulnTimer = PerkEffects.ShellInvulnDuration;
		}
		if (Game.Lives <= 0)
		{
			TryRevive();
		}
	}

	/// <summary>Возрождение «Второй жизни» вместо Game Over.</summary>
	private bool TryRevive()
	{
		if (Game == null || _revivesLeft <= 0)
		{
			Game?.GameOverFromPerk();
			return false;
		}
		_revivesLeft--;
		Game.SetLivesFromPerk(PerkEffects.ReviveHp(_perks));
		_invulnTimer = Mathf.Max(_invulnTimer, PerkEffects.ReviveInvulnDuration(_perks));
		Game.HudNode?.SpawnPopup(Game.FrogNode?.GlobalPosition ?? Vector2.Zero, "Возрождение!");

		// lvl5: полная очистка экрана от мусора.
		if (PerkEffects.ReviveClearsTrash(_perks))
		{
			ClearTrashOnScreen();
		}
		return true;
	}

	/// <summary>Убирает весь мусор с поля («Вторая жизнь» ур. 5).</summary>
	private void ClearTrashOnScreen()
	{
		Node2D? items = Game?.ItemsNode;
		if (items == null)
		{
			return;
		}
		foreach (Node child in items.GetChildren())
		{
			if (child is FallingItem item && item.ItemType == ItemType.Trash)
			{
				item.QueueFree();
			}
		}
	}

	// ---------- Награды ----------

	/// <summary>Награда за собранный предмет (руки, авто-ловля).</summary>
	public void AwardCaught(FallingItem item)
	{
		if (item == null || item.IsQueuedForDeletion())
		{
			return;
		}
		AwardCaught(item, false);
	}

	/// <summary>
	/// Единая точка начисления очков, комбо и XP. fromExplosion — сборы
	/// «Фруктового взрыва»: очки без XP и без пополнения запаса Suika.
	/// </summary>
	private void AwardCaught(FallingItem item, bool fromExplosion)
	{
		if (Game == null)
		{
			item.QueueFree();
			return;
		}
		if (item.ItemType == ItemType.Fruit && !fromExplosion)
		{
			Game.RegisterFruitFromPerk(item.Kind);
		}
		if (!Game.IsPlaying)
		{
			item.QueueFree();
			return;
		}

		int basePoints = 0;
		switch (item.ItemType)
		{
			case ItemType.Fruit:
				basePoints = 10;
				break;
			case ItemType.GoldenFruit:
				basePoints = 10 * PerkEffects.GoldenScoreMultiplier(_perks);
				break;
		}

		// Замороженные фрукты «Взгляда василиска» (lvl5) дают x2.
		if (PerkEffects.GazeFrozenDoubleScore(_perks) && item.Frozen && basePoints > 0)
		{
			basePoints *= 2;
		}

		// «Резиновый хват»: +1 очко за каждый пойманный фрукт.
		if (!fromExplosion && item.ItemType == ItemType.Fruit)
		{
			basePoints += PerkEffects.BonusScorePerFruit(_perks);
		}

		// «Фруктовый взрыв» ур. 1: +1 очко, если рядом другой фрукт.
		if (!fromExplosion && item.ItemType == ItemType.Fruit && PerkEffects.ExplosionNearBonus(_perks) && HasFruitNearby(item, PerkEffects.ExplosionNearRadius(_perks)))
		{
			basePoints += 1;
		}

		// Комбо растёт только от «ручных» поимок.
		if (!fromExplosion)
		{
			int gain = PerkEffects.ComboGain(_perks);
			if (gain > 0)
			{
				AddCombo(gain);
			}
		}

		if (basePoints > 0)
		{
			int points = basePoints * PerkEffects.ComboMultiplier(_combo, _perks);
			Game.AddScore(points);
			Game.HudNode?.SpawnPopup(Game.FrogNode?.GlobalPosition ?? item.GlobalPosition, "+" + points);
		}

		if (!fromExplosion)
		{
			Game.Xp.AddXp(XpSystem.XpFor(item.ItemType));
			Game.HudNode?.SetXp(Game.Xp.Level, Game.Xp.LevelProgress);
		}

		// «Фруктовый взрыв» — после обычной поимки.
		if (!fromExplosion && PerkEffects.ExplosionRadius(_perks) > 0f)
		{
			TriggerExplosion(item, PerkEffects.ExplosionRadius(_perks), PerkEffects.ExplosionCascades(_perks));
		}

		item.QueueFree();
	}

	/// <summary>Добавляет очки комбо и проверяет награду +1 HP («Комбо-мастер» ур. 4).</summary>
	private void AddCombo(int amount)
	{
		int hpEvery = PerkEffects.ComboHpEvery(_perks);
		if (hpEvery <= 0)
		{
			_combo += amount;
			return;
		}
		int oldTier = (int)(_combo / hpEvery);
		_combo += amount;
		int newTier = (int)(_combo / hpEvery);
		if (newTier > oldTier)
		{
			GiveBonusHp();
		}
	}

	/// <summary>Восстанавливает одну жизнь (не превышая максимума) с попапом.</summary>
	private void GiveBonusHp()
	{
		if (Game == null || Game.Lives >= Game.MaxLives)
		{
			return;
		}
		Game.SetLivesFromPerk(Game.Lives + 1);
		Game.HudNode?.SpawnPopup(Game.FrogNode?.GlobalPosition ?? Vector2.Zero, "+1 HP");
	}

	/// <summary>Есть ли другой фрукт в радиусе от точки (бонус «Фруктового взрыва» ур. 1).</summary>
	private bool HasFruitNearby(FallingItem source, float radius)
	{
		Node2D? items = Game?.ItemsNode;
		if (items == null)
		{
			return false;
		}
		foreach (Node child in items.GetChildren())
		{
			if (child is not FallingItem item || item == source || !IsCatchable(item))
			{
				continue;
			}
			if (item.GlobalPosition.DistanceTo(source.GlobalPosition) < radius)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// «Фруктовый взрыв»: цепная реакция. Собирает фрукты в радиусе, уничтожает мусор (lvl4),
	/// а на lvl5 собранные фрукты тоже создают взрывы (каскад).
	/// </summary>
	private void TriggerExplosion(FallingItem source, float radius, bool cascade)
	{
		Node2D? items = Game?.ItemsNode;
		if (items == null || radius <= 0f)
		{
			return;
		}

		int bonusPerFruit = PerkEffects.ExplosionBonusPerFruit(_perks);
		int bonusTotal = 0;

		// Очередь каскада без удаления элементов — двигаем указатель по списку.
		var pending = new List<Vector2>();
		pending.Add(source.GlobalPosition);
		int cursor = 0;
		while (cursor < pending.Count)
		{
			Vector2 origin = pending[cursor];
			cursor++;

			foreach (Node child in items.GetChildren())
			{
				if (child is not FallingItem item || item == source)
				{
					continue;
				}
				if (item.IsCaught || item.IsQueuedForDeletion() || item.ManualPhysics)
				{
					continue;
				}
				if (item.ItemType != ItemType.Fruit && item.ItemType != ItemType.GoldenFruit && item.ItemType != ItemType.Trash)
				{
					continue;
				}
				if (item.GlobalPosition.DistanceTo(origin) > radius)
				{
					continue;
				}

				if (item.ItemType == ItemType.Trash)
				{
					// lvl4+: взрыв уничтожает мусор рядом.
					if (PerkEffects.ExplosionDestroysTrash(_perks))
					{
						item.QueueFree();
					}
					continue;
				}

				Vector2 at = item.GlobalPosition;
				bonusTotal += bonusPerFruit;
				AwardCaught(item, true);
				if (cascade)
				{
					pending.Add(at);
				}
			}
		}

		if (bonusTotal > 0)
		{
			Game?.AddScore(bonusTotal);
			Game?.HudNode?.SpawnPopup(Game?.FrogNode?.GlobalPosition ?? source.GlobalPosition, "Взрыв +" + bonusTotal);
		}
	}

	/// <summary>«Язык-молния»: мгновенно забирает ближайшие фрукты, иногда бьёт по мусору.</summary>
	private void LightningAutoCatch()
	{
		Node2D? items = Game?.ItemsNode;
		Frog? frog = Game?.FrogNode;
		if (items == null || frog == null || !Game.IsPlaying)
		{
			return;
		}
		Vector2 frogPos = frog.GlobalPosition;
		int wanted = PerkEffects.LightningCatchCount(_perks);

		// Берём ближайшие фрукты по одному.
		var caught = new List<FallingItem>();
		for (int pick = 0; pick < wanted; pick++)
		{
			FallingItem? best = null;
			float bestDist = float.PositiveInfinity;
			foreach (Node child in items.GetChildren())
			{
				if (child is not FallingItem item || !IsCatchable(item) || caught.Contains(item))
				{
					continue;
				}
				float d = item.GlobalPosition.DistanceSquaredTo(frogPos);
				if (d < bestDist)
				{
					bestDist = d;
					best = item;
				}
			}
			if (best == null)
			{
				break;
			}
			caught.Add(best);
		}

		foreach (FallingItem item in caught)
		{
			Game.HudNode?.SpawnPopup(item.GlobalPosition, "⚡");
			AwardCaught(item);
		}

		// lvl4: язык заодно уничтожает ближайший мусор.
		if (PerkEffects.LightningDestroysTrash(_perks))
		{
			FallingItem? trash = null;
			float bestDist = float.PositiveInfinity;
			foreach (Node child in items.GetChildren())
			{
				if (child is not FallingItem item || item.ItemType != ItemType.Trash || item.IsCaught || item.IsQueuedForDeletion() || item.ManualPhysics)
				{
					continue;
				}
				float d = item.GlobalPosition.DistanceSquaredTo(frogPos);
				if (d < bestDist)
				{
					bestDist = d;
					trash = item;
				}
			}
			if (trash != null)
			{
				Game.HudNode?.SpawnPopup(trash.GlobalPosition, "⚡");
				trash.QueueFree();
			}
		}
	}

	/// <summary>Можно ли сейчас «физически» поймать этот предмет (фрукт/золотой на поле).</summary>
	private bool IsCatchable(FallingItem item)
	{
		if (item.IsCaught || item.IsQueuedForDeletion() || item.ManualPhysics)
		{
			return false;
		}
		return item.ItemType == ItemType.Fruit || item.ItemType == ItemType.GoldenFruit;
	}

	// ---------- Заморозка, замедление, магнит ----------

	/// <summary>Должен ли предмет быть обездвижен в этом кадре (заморозки перков).</summary>
	private bool IsItemFrozen(FallingItem item)
	{
		if (_worldFreezeTimer > 0f)
		{
			return true;
		}
		if (_gazeFreezeTimer > 0f)
		{
			if (item.ItemType == ItemType.Fruit || item.ItemType == ItemType.GoldenFruit)
			{
				return true;
			}
			// lvl3+: мусор «каменеет» и становится безопасным.
			return item.ItemType == ItemType.Trash && PerkEffects.GazeStonesTrash(_perks);
		}
		return false;
	}

	/// <summary>«Зона замедления»: плавно замедляет падение предметов в нижней зоне экрана.</summary>
	private void ApplySlowZone(FallingItem item, Vector2 viewSize, float dt)
	{
		float factor = PerkEffects.FallSlowFactor(_perks, item.ItemType);
		if (factor >= 1f)
		{
			return;
		}
		float zoneStart = PerkEffects.SlowZoneCoversHalf(_perks) ? viewSize.Y * 0.5f : viewSize.Y * (2f / 3f);
		if (item.Position.Y < zoneStart)
		{
			return;
		}
		float targetY = item.FallSpeed * factor;
		float t = Mathf.Clamp(dt * 5f, 0f, 1f);
		item.Velocity = new Vector2(item.Velocity.X, Mathf.Lerp(item.Velocity.Y, targetY, t));
	}

	/// <summary>«Липкие ладони»: фрукты в радиусе магнита тянутся к ладони.</summary>
	private void ApplyMagnet(FallingItem item, Vector2[]? hands)
	{
		if (hands == null || item.ItemType != ItemType.Fruit)
		{
			return;
		}
		float radius = PerkEffects.MagnetRadius(_perks);
		if (radius <= 0f)
		{
			return;
		}
		foreach (Vector2 hand in hands)
		{
			Vector2 toHand = hand - item.GlobalPosition;
			float distSq = toHand.LengthSquared();
			if (distSq > radius * radius)
			{
				continue;
			}
			if (distSq <= 0.0001f)
			{
				return;
			}
			item.Velocity = toHand * (220f / Mathf.Sqrt(distSq));
			return;
		}
	}
}