using System.Collections.Generic;
using System;
using Godot;

namespace SwampFrog;

public enum GameState
{
	Menu,
	Playing,
	GameOver,
	Suika,
}

/// <summary>
/// Корень игры: фон, спавн падающих объектов, ловля руками, счёт, жизни,
/// сложность, сохранение рекорда и рестарт.
/// </summary>
public partial class Main : Node2D
{
	private const int MaxLives = 3;
	private const float InitialSpawnInterval = 0.95f;
	private const float MinSpawnInterval = 0.34f;

	/// <summary>Очень маленький шанс выпадения хилки при неполном HP.</summary>
	private const float HealSpawnChance = 0.05f;

	private Frog? _frog;
	private Node2D? _items;
	private HUD? _hud;
	private SuikaGame? _suika;
	private Timer? _spawnTimer;
	private readonly RandomNumberGenerator _rng = new();

	private int _score;
	private int _lives = MaxLives;
	private int _highScore;
	private GameState _state = GameState.Menu;
	private readonly XpSystem _xp = new();
	/// <summary>
	/// Запас фруктов, собранных в классическом режиме: вид → количество штук.
	/// Каждая поимка добавляет одну единицу; каждый бросок в Suika списывает одну.
	/// Остаётся между партиями и перезапусками игры (сохраняется в save.cfg).
	/// </summary>
	private readonly Dictionary<FruitKind, int> _fruitStock = new();

	public GameState State => _state;
	public int Score => _score;
	public int Lives => _lives;

	public override void _Ready()
	{
		_rng.Randomize();

		_frog = GetNode<Frog>("Frog");
		_frog.Game = this;
		_frog.SyncUiScale(ScreenScale);
		_frog.CaughtItemReturned += OnCaughtItemReturned;
		_xp.LevelUp += OnLevelUp;
		_items = GetNode<Node2D>("Items");
		_hud = GetNode<HUD>("HUD");
		_suika = GetNode<SuikaGame>("Suika");
		_suika.Game = this;
		_suika.ExitRequested += CloseSuika;
		_hud.PlayPressed += StartGame;
		_hud.MiniGamePressed += OpenSuika;
		_hud.ExitPressed += QuitGame;
		_hud.RestartPressed += Restart;
		_hud.MenuPressed += GoToMenu;

		_highScore = LoadHighScore();
		LoadCollectedFruits();
		UpdateSuikaButton();

		_spawnTimer = new Timer();
		AddChild(_spawnTimer);
		_spawnTimer.WaitTime = InitialSpawnInterval;
		_spawnTimer.Timeout += OnSpawnTimerTimeout;
		// Не запускаем: стартовая кнопка управляет запуском партии.

		GetViewport().SizeChanged += OnViewportResized;

		PositionFrog();
		_hud.SetScore(0);
		_hud.SetLives(_lives);
		_hud.HideGameOver();
		_hud.ShowStart();
		_hud.SetGameplayVisible(false);
		_hud.SetXp(_xp.Level, _xp.LevelProgress);
		_suika.Visible = false;
	}

	private void OnViewportResized()
	{
		QueueRedraw();
		PositionFrog();
		_frog?.SyncUiScale(ScreenScale);
	}

	private void PositionFrog()
	{
		if (_frog == null)
		{
			return;
		}
		Vector2 size = GetViewportRect().Size;
		_frog.Position = new Vector2(Mathf.Min(180f * ScreenScale, size.X * 0.30f), size.Y * 0.56f);
	}

	/// <summary>
	/// База для UI/игрового мира: во сколько раз видимая область больше или меньше
	/// эталонного портретного разрешения 540×960. Кламп по соображениям разумных границ.
	/// </summary>
	public float ScreenScale
	{
		get
		{
			Vector2 size = GetViewportRect().Size;
			if (size.X <= 0f || size.Y <= 0f)
			{
				return 1f;
			}
			return Mathf.Clamp(Mathf.Min(size.X / 540f, size.Y / 960f), 0.85f, 2.2f);
		}
	}

	/// <summary>Размер видимой области (в координатах сцены).</summary>
	public Vector2 ViewSize => GetViewportRect().Size;

	public override void _Process(double delta)
	{
		if (_state != GameState.Playing || _items == null || _frog == null)
		{
			return;
		}

		Vector2 viewSize = GetViewportRect().Size;
		Vector2[]? hands = _frog.IsCatching ? _frog.GetHandPositions() : null;

		foreach (Node child in _items.GetChildren())
		{
			if (child is not FallingItem item)
			{
				continue;
			}

			// Уже пойман и едет за ладонью — не обрабатываем.
			if (item.IsCaught)
			{
				continue;
			}

			// Предмет уже помечен на удаление (например, остался после Game Over
			// в кадре рестарта) — не обрабатываем, чтобы не наносил урон новой попытке.
			if (item.IsQueuedForDeletion())
			{
				continue;
			}

			// Упал за нижнюю границу — штраф за фрукт/золотой, мусор просто пропадает.
			if (item.Position.Y > viewSize.Y + 30f)
			{
				if (item.ItemType == ItemType.Fruit || item.ItemType == ItemType.GoldenFruit)
				{
					_hud?.SpawnPopup(new Vector2(item.Position.X, viewSize.Y - 60f), "Мимо!");
					TakeDamage();
				}
				item.QueueFree();
				continue;
			}

			if (hands == null)
			{
				continue;
			}

			float itemScale = item.Scale.X;
			for (int i = 0; i < hands.Length; i++)
			{
				// Рука может держать не более одного предмета; занятую пропускаем.
				if (!_frog.CanHold(i))
				{
					continue;
				}
				Vector2 hand = hands[i];
				// Радиус зависит от масштаба предмета и лягушки.
				if (item.GlobalPosition.DistanceTo(hand) <= item.CatchRadius * itemScale + _frog.CatchRadiusWorld)
				{
					CatchItem(item, i);
					break;
				}
			}
		}

		// Падающие предметы отталкиваются друг от друга.
		ResolveItemCollisions();
	}

	/// <summary>Сброс при рестарте партии (опыт не сохраняется между партиями).</summary>
	private void ResetXp()
	{
		_xp.Reset();
		_hud?.SetXp(_xp.Level, _xp.LevelProgress);
	}

	private void OnLevelUp(int newLevel)
	{
		if (_state == GameState.Playing)
		{
			_hud?.ShowLevelUp(newLevel);
		}
	}

	/// <summary>
	/// Попарное отталкивание падающих предметов: раздвигает пересекающиеся объекты
	/// и разворачивает их скорости вдоль нормали (мягкий рикошет). Пойманные не участвуют.
	/// </summary>
	private void ResolveItemCollisions()
	{
		var moving = new List<FallingItem>();
		foreach (Node child in _items!.GetChildren())
		{
			if (child is FallingItem fi && !fi.IsCaught && !fi.IsQueuedForDeletion())
			{
				moving.Add(fi);
			}
		}

		const float restitution = 0.6f;
		for (int i = 0; i < moving.Count; i++)
		{
			for (int j = i + 1; j < moving.Count; j++)
			{
				FallingItem a = moving[i];
				FallingItem b = moving[j];
				Vector2 delta = a.GlobalPosition - b.GlobalPosition;
				float minDist = a.Radius + b.Radius;
				float distSq = delta.LengthSquared();
				if (distSq > minDist * minDist || distSq <= 0.0001f)
				{
					continue;
				}

				float dist = Mathf.Sqrt(distSq);
				Vector2 n = delta / dist;

				// Раздвигаем, чтобы предметы не проникали друг в друга.
				float overlap = minDist - dist;
				a.GlobalPosition += n * (overlap * 0.5f);
				b.GlobalPosition -= n * (overlap * 0.5f);

				// Импульс вдоль нормали (равные массы): отталкиваем, только если сближаются.
				float relN = (a.Velocity - b.Velocity).Dot(n);
				if (relN < 0f)
				{
					float impulse = -(1f + restitution) * relN * 0.5f;
					a.Velocity += n * impulse;
					b.Velocity -= n * impulse;
				}
			}
		}
	}

	private void CatchItem(FallingItem item, int handIndex)
	{
		// Мусор нельзя нести в руках: сразу урон и исчезновение.
		if (item.ItemType == ItemType.Trash)
		{
			TakeDamage();
			item.QueueFree();
			return;
		}

		// Хилка тоже не несётся в руках: мгновенно восстанавливает жизнь.
		if (item.ItemType == ItemType.Healing)
		{
			RestoreLife(item.GlobalPosition);
			item.QueueFree();
			return;
		}

		// Фрукт берётся в ладонь; очки начислятся позже — после полного возврата рук.
		if (_frog?.AttachCaughtItem(item, handIndex) == true)
		{
			_frog.Pulse();
		}
	}

	/// <summary>Фрукт полностью вернулся к лягушке — начисляем очки (если игра ещё идёт) и убираем предмет.</summary>
	private void OnCaughtItemReturned(FallingItem item)
	{
		if (item.ItemType == ItemType.Fruit)
		{
			RegisterCollectedFruit(item.Kind);
		}
		if (_state == GameState.Playing)
		{
			switch (item.ItemType)
			{
				case ItemType.GoldenFruit:
					_score += 30;
					_hud?.SpawnPopup(_frog?.GlobalPosition ?? item.GlobalPosition, "+30");
					break;
				case ItemType.Fruit:
					_score += 10;
					_hud?.SpawnPopup(_frog?.GlobalPosition ?? item.GlobalPosition, "+10");
					break;
			}
			_hud?.SetScore(_score);
		}

		// Пойман фрукт — начисляем опыт (в момент полного возврата к лягушке).
		_xp.AddXp(XpSystem.XpFor(item.ItemType));
		_hud?.SetXp(_xp.Level, _xp.LevelProgress);

		item.QueueFree();
	}

	private void TakeDamage()
	{
		_lives--;
		_hud?.SetLives(_lives);
		_hud?.FlashRed();
		_frog?.Flash();

		if (_lives <= 0)
		{
			GameOver();
		}
	}

	/// <summary>Восстанавливает одну жизнь (не превышая максимум).</summary>
	private void RestoreLife(Vector2 atGlobalPos)
	{
		if (_lives >= MaxLives)
		{
			return;
		}
		_lives++;
		_hud?.SetLives(_lives);
		_hud?.SpawnPopup(atGlobalPos, "+1");
	}

	// ---------- Спавн ----------

	private void OnSpawnTimerTimeout()
	{
		if (_state != GameState.Playing)
		{
			return;
		}
		SpawnItem();
		UpdateDifficulty();
	}

	private void SpawnItem()
	{
		var item = new FallingItem();
		Vector2 size = GetViewportRect().Size;

		float trashWeight = Mathf.Min(0.32f, 0.15f + _score * 0.00025f);
		float roll = _rng.Randf();

		ItemType type;
		// Хилка выпадает только при неполных жизнях и с очень маленьким шансом.
		if (_lives < MaxLives && _rng.Randf() < HealSpawnChance)
		{
			type = ItemType.Healing;
		}
		else if (roll < trashWeight)
		{
			type = ItemType.Trash;
		}
		else if (roll < trashWeight + 0.14f)
		{
			type = ItemType.GoldenFruit;
		}
		else
		{
			type = ItemType.Fruit;
			// В классическом режиме попадаются только «мелкие» фрукты (первые 6 из каталога).
			item.Kind = FruitCatalog.PickClassic(_rng);
		}

		float difficulty = Mathf.Clamp(_score / 500f, 0f, 1f);
		float speed = Mathf.Lerp(150f, 330f, difficulty) + _rng.RandfRange(-25f, 25f);

		item.ItemType = type;
		item.FallSpeed = speed;
		item.Position = new Vector2(_rng.RandfRange(46f, Mathf.Max(60f, size.X - 46f)), -70f);
		item.Scale = Vector2.One * (ScreenScale * _rng.RandfRange(0.85f, 1.08f));
		_items!.AddChild(item);
	}

	private void UpdateDifficulty()
	{
		if (_spawnTimer == null)
		{
			return;
		}
		float difficulty = Mathf.Clamp(_score / 500f, 0f, 1f);
		_spawnTimer.WaitTime = Mathf.Lerp(InitialSpawnInterval, MinSpawnInterval, difficulty) * _rng.RandfRange(0.8f, 1.2f);
	}

	// ---------- Игровой цикл ----------

	/// <summary>Запуск игры со стартового экрана.</summary>
	public void StartGame()
	{
		if (_state == GameState.Menu)
		{
			ResetMainRoundData();
		}
		_suika?.Close();
		if (_frog != null) _frog.Visible = true;
		_state = GameState.Playing;
		_hud?.HideStart();
		_hud?.HideGameOver();
		_hud?.SetGameplayVisible(true);
		_hud?.ShowHint();

		if (_spawnTimer != null)
		{
			_spawnTimer.WaitTime = InitialSpawnInterval;
			_spawnTimer.Start();
		}
	}

	/// <summary>Открывает отдельную мини-игру с фруктами из запаса игрока.</summary>
	public void OpenSuika()
	{
		if (TotalFruitStock() <= 0)
		{
			// Фруктов нет — показываем подсказку, а не открываем режим.
			_hud?.ShowSuikaNeedFruitsHint();
			return;
		}
		_spawnTimer?.Stop();
		ClearMainRound();
		_state = GameState.Suika;
		_frog!.Visible = false;
		_hud?.HideStart();
		_hud?.HideGameOver();
		_hud?.SetGameplayVisible(false);
		_suika?.Open();
	}

	private void CloseSuika()
	{
		_suika?.Close();
		UpdateSuikaButton();
		ResetMainRoundData();
		_state = GameState.Menu;
		_frog!.Visible = true;
		PositionFrog();
		_hud?.SetGameplayVisible(false);
		_hud?.ShowStart();
	}

	/// <summary>Возвращает игрока в главное меню, очищая текущую партию.</summary>
	public void GoToMenu()
	{
		_spawnTimer?.Stop();
		ClearMainRound();
		ResetMainRoundData();
		_state = GameState.Menu;
		_frog!.Visible = true;
		PositionFrog();
		_hud?.HideGameOver();
		_hud?.ShowStart();
	}

	private void QuitGame() => GetTree().Quit();

	private void ClearMainRound()
	{
		if (_items != null)
		{
			foreach (Node child in _items.GetChildren()) child.QueueFree();
		}
		_frog?.ClearCaughtItems();
	}

	private void ResetMainRoundData()
	{
		_score = 0;
		_lives = MaxLives;
		ResetXp();
		_hud?.SetScore(0);
		_hud?.SetLives(_lives);
	}

	private void GameOver()
	{
		_state = GameState.GameOver;
		if (_score > _highScore)
		{
			_highScore = _score;
			SaveHighScore(_highScore);
		}
		_spawnTimer?.Stop();
		_hud?.ShowGameOver(_score, _highScore);
	}

	private void Restart()
	{
		ClearMainRound();
		_frog!.Visible = true;

		ResetMainRoundData();
		_state = GameState.Playing;

		_hud?.SetScore(_score);
		_hud?.SetLives(_lives);
		_hud?.HideGameOver();
		_hud?.SetGameplayVisible(true);

		if (_spawnTimer != null)
		{
			_spawnTimer.WaitTime = InitialSpawnInterval;
			_spawnTimer.Start();
		}
	}

	/// <summary>
	/// Возвращает пул фруктов для Suika: только те виды, которые игрок собрал
	/// в классическом режиме и которые ещё есть в запасе (количество &gt; 0).
	/// </summary>
	public FruitKind[] GetSuikaFruitPool()
	{
		var result = new List<FruitKind>();
		foreach (FruitKind kind in _fruitStock.Keys)
		{
			if (StockOf(kind) > 0) result.Add(kind);
		}
		result.Sort();
		return result.ToArray();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true } key) return;
		if (key.Keycode == Key.F2 && _state == GameState.Menu)
		{
			OpenSuika();
			GetViewport().SetInputAsHandled();
		}
	}

	private void RegisterCollectedFruit(FruitKind kind)
	{
		_fruitStock[kind] = StockOf(kind) + 1;
		SaveCollectedFruits();
		UpdateSuikaButton();
	}

	/// <summary>Остаток фруктов данного вида в запасе.</summary>
	public int FruitStock(FruitKind kind) => StockOf(kind);

	/// <summary>Внутренний хелпер чтения счётчика без исключений по отсутствующему ключу.</summary>
	private int StockOf(FruitKind kind)
	{
		return _fruitStock.TryGetValue(kind, out int count) ? count : 0;
	}

	/// <summary>Суммарный остаток фруктов в запасе (сколько всего можно бросить в Suika).</summary>
	public int TotalFruitStock()
	{
		int total = 0;
		foreach (int count in _fruitStock.Values) total += count;
		return total;
	}

	/// <summary>
	/// Списывает одну единицу фрукта из запаса. Возвращает false, если такого фрукта нет.
	/// Вызывается из Suika в момент броска, поэтому расход сохраняется сразу.
	/// </summary>
	public bool ConsumeFruit(FruitKind kind)
	{
		int count = StockOf(kind);
		if (count <= 0)
		{
			return false;
		}
		if (count > 1)
		{
			_fruitStock[kind] = count - 1;
		}
		else
		{
			_fruitStock.Remove(kind);
		}
		SaveCollectedFruits();
		return true;
	}

	private void LoadCollectedFruits()
	{
		var config = new ConfigFile();
		if (config.Load("user://save.cfg") != Error.Ok) return;
		string raw = config.GetValue("collection", "fruits", "").ToString();
		foreach (string token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			// Новый формат «Вид:Количество». Старые сейвы содержали просто имена видов —
			// конвертируем их в одну единицу каждого вида.
			int colon = token.IndexOf(':');
			if (colon < 0)
			{
				if (Enum.TryParse(token, out FruitKind freshKind)) _fruitStock[freshKind] = StockOf(freshKind) + 1;
				continue;
			}
			string name = token.Substring(0, colon).StripEdges();
			int amount = token.Substring(colon + 1).StripEdges().ToInt();
			if (amount > 0 && Enum.TryParse(name, out FruitKind savedKind))
			{
				_fruitStock[savedKind] = StockOf(savedKind) + amount;
			}
		}
	}

	private void SaveCollectedFruits()
	{
		var config = new ConfigFile();
		config.Load("user://save.cfg");
		var entries = new List<string>();
		foreach (FruitKind kind in _fruitStock.Keys)
		{
			entries.Add($"{kind}:{_fruitStock[kind]}");
		}
		entries.Sort();
		config.SetValue("collection", "fruits", string.Join(",", entries));
		config.Save("user://save.cfg");
	}

	/// <summary>Блокирует/разблокирует кнопку «Мини-игра: Suika» по наличию запаса.</summary>
	private void UpdateSuikaButton()
	{
		_hud?.SetMiniGameEnabled(TotalFruitStock() > 0);
	}

	// ---------- Рекорд ----------

	private static int LoadHighScore()
	{
		var config = new ConfigFile();
		Error err = config.Load("user://save.cfg");
		if (err != Error.Ok)
		{
			return 0;
		}
		return (int)config.GetValue("score", "high", 0);
	}

	private static void SaveHighScore(int value)
	{
		var config = new ConfigFile();
		config.Load("user://save.cfg");
		config.SetValue("score", "high", value);
		config.Save("user://save.cfg");
	}

	// ---------- Фон (пруд) ----------

	public override void _Draw()
	{
		Vector2 size = GetViewportRect().Size;
		if (size.X <= 0f || size.Y <= 0f)
		{
			return;
		}

		// Вертикальный градиент «глубина пруда».
		const int steps = 64;
		Color top = new("1f7a6b");
		Color bottom = new("0a352f");
		float strip = size.Y / steps;
		for (int i = 0; i < steps; i++)
		{
			float t = i / (float)(steps - 1);
			DrawRect(new Rect2(0f, i * strip, size.X, strip + 1f), top.Lerp(bottom, t));
		}

		float s = ScreenScale;

		// Мягкие блики света на воде.
		float[][] spots =
		{
			new[] { 0.20f, 0.15f, 46f },
			new[] { 0.72f, 0.22f, 60f },
			new[] { 0.85f, 0.58f, 34f },
			new[] { 0.30f, 0.75f, 30f },
			new[] { 0.62f, 0.38f, 26f },
		};
		foreach (float[] sp in spots)
		{
			DrawCircle(new Vector2(size.X * sp[0], size.Y * sp[1]), sp[2] * s, new Color(1f, 1f, 1f, 0.05f));
		}

		// Кувшинки у дна (размер пропорционален экрану).
		DrawLilyPad(new Vector2(size.X * 0.25f, size.Y * 0.88f), 34f * s);
		DrawLilyPad(new Vector2(size.X * 0.80f, size.Y * 0.82f), 26f * s);
		DrawLilyPad(new Vector2(size.X * 0.58f, size.Y * 0.93f), 30f * s);
		DrawLilyPad(new Vector2(size.X * 0.10f, size.Y * 0.70f), 22f * s);
	}

	private void DrawLilyPad(Vector2 center, float radius)
	{
		DrawCircle(center, radius, new Color(0.22f, 0.55f, 0.33f));
		DrawCircle(center, radius, new Color(0.16f, 0.44f, 0.26f), false, 4f);

		float start = -0.6f;
		Vector2[] notch =
		{
			center + new Vector2(Mathf.Cos(start), Mathf.Sin(start)) * radius,
			center + new Vector2(Mathf.Cos(start + 1.8f), Mathf.Sin(start + 1.8f)) * radius,
			center,
		};
		DrawColoredPolygon(notch, new Color("0a352f"));
	}
}
