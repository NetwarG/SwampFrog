using Godot;

namespace SwampFrog;

public enum GameState
{
	Menu,
	Playing,
	GameOver,
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

	private Frog? _frog;
	private Node2D? _items;
	private HUD? _hud;
	private Timer? _spawnTimer;
	private readonly RandomNumberGenerator _rng = new();

	private int _score;
	private int _lives = MaxLives;
	private int _highScore;
	private GameState _state = GameState.Menu;

	public GameState State => _state;
	public int Score => _score;
	public int Lives => _lives;

	public override void _Ready()
	{
		_rng.Randomize();

		_frog = GetNode<Frog>("Frog");
		_frog.Game = this;
		_frog.SyncUiScale(ScreenScale);
		_items = GetNode<Node2D>("Items");
		_hud = GetNode<HUD>("HUD");

		_highScore = LoadHighScore();

		_spawnTimer = new Timer();
		AddChild(_spawnTimer);
		_spawnTimer.WaitTime = InitialSpawnInterval;
		_spawnTimer.Timeout += OnSpawnTimerTimeout;
		// Не запускаем: ждём первого касания на стартовом экране.

		GetViewport().SizeChanged += OnViewportResized;

		PositionFrog();
		_hud.SetScore(0);
		_hud.SetLives(_lives);
		_hud.HideGameOver();
		_hud.ShowStart();
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
			foreach (Vector2 hand in hands)
			{
				// Радиус зависит от масштаба предмета и лягушки.
				if (item.GlobalPosition.DistanceTo(hand) <= item.CatchRadius * itemScale + _frog.CatchRadiusWorld)
				{
					CatchItem(item);
					break;
				}
			}
		}
	}

	private void CatchItem(FallingItem item)
	{
		switch (item.ItemType)
		{
			case ItemType.GoldenFruit:
				_score += 30;
				_hud?.SpawnPopup(item.GlobalPosition, "+30");
				break;
			case ItemType.Fruit:
				_score += 10;
				_hud?.SpawnPopup(item.GlobalPosition, "+10");
				break;
			case ItemType.Trash:
				TakeDamage();
				break;
		}

		item.QueueFree();
		_frog?.Pulse();
		_hud?.SetScore(_score);
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
		if (roll < trashWeight)
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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventScreenTouch { Pressed: true })
		{
			return;
		}

		switch (_state)
		{
			case GameState.Menu:
				StartGame();
				break;
			case GameState.GameOver:
				Restart();
				break;
		}
	}

	/// <summary>Запуск игры со стартового экрана.</summary>
	private void StartGame()
	{
		_state = GameState.Playing;
		_hud?.HideStart();
		_hud?.ShowHint();

		if (_spawnTimer != null)
		{
			_spawnTimer.WaitTime = InitialSpawnInterval;
			_spawnTimer.Start();
		}
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
		if (_items != null)
		{
			foreach (Node child in _items.GetChildren())
			{
				child.QueueFree();
			}
		}

		_score = 0;
		_lives = MaxLives;
		_state = GameState.Playing;

		_hud?.SetScore(0);
		_hud?.SetLives(_lives);
		_hud?.HideGameOver();

		if (_spawnTimer != null)
		{
			_spawnTimer.WaitTime = InitialSpawnInterval;
			_spawnTimer.Start();
		}
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