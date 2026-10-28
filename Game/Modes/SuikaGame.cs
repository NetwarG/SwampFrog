using System;
using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Мини-игра Suika: лягушка двигается по верхней границе и бросает фрукты в кувшин.
/// Фрукты используют тот же FallingItem, что и основной режим, поэтому их внешний вид
/// и последовательность развития всегда остаются синхронизированы с FruitCatalog.
/// Движение и столкновения считает физический движок проекта через стандартные тела Godot.
/// Часть класса SuikaGame: см. SuikaPile.cs (фрукты в кувшине) и SuikaUi.cs (интерфейс).
/// </summary>
public partial class SuikaGame : Node2D
{
	private const float Restitution = 0.25f;
	private const float FruitFriction = 0.4f;
	/// <summary>Трение дна и стенок: горка должна сдвигаться от удара, а не упираться в дно.</summary>
	private const float BowlFriction = 0.1f;
	/// <summary>Затухание движения: удар брошенного фрукта не должен тонуть в вязкости.</summary>
	private const float FruitLinearDamp = 0f;
	private const float FruitAngularDamp = 0.3f;
	/// <summary>Показатель роста массы: крупный фрукт тяжелее, но не «давит» мелкие намертво.</summary>
	private const float MassExponent = 1f;
	private const uint SuikaCollisionLayer = ItemPhysics.SuikaLayer;
	private const float WallThickness = 12f;
	private const float FloorInset = 4f;
	/// <summary>Ширина кувшина в долях от ширины экрана.</summary>
	private const float BowlWidthRatio = 0.8f;
	/// <summary>Высота кувшина в долях от высоты экрана.</summary>
	private const float BowlHeightRatio = 0.70f;
	/// <summary>Отступ кувшина от нижнего края экрана.</summary>
	private const float BowlBottom = 88f;
	/// <summary>Минимальные размеры кувшина, чтобы корзина оставалась играбельной на узких окнах.</summary>
	private const float BowlMinWidth = 160f;
	private const float BowlMinHeight = 200f;
	/// <summary>Отступ линии переполнения от верхнего края кувшина.</summary>
	private const float OverflowRimOffset = 56f;
	private const float OverflowDelay = 1.15f;

	/// <summary>Пауза перед завершением раунда после того, как последний фрукт упал и устаканился.</summary>
	private const float SettleDelay = 0.9f;

	/// <summary>
	/// Жёсткий лимит завершения раунда: даже если куски долго не «устаканиваются»
	/// из-за столкновений, раунд завершится не позднее чем через 8 секунд
	/// после последнего брошенного фрукта.
	/// </summary>
	private const float CompletionTimeout = 8f;

	/// <summary>Скорость, ниже которой кусок считается «устаканившимся» (могут слиться).</summary>
	private const float SettleSpeed = 40f;

	/// <summary>Случайный боковой импульс брошенного фрукта, чтобы броски в одну точку не ложились столбцом.</summary>
	private const float DropScatter = 70f;

	/// <summary>Скорость броска вниз: у самой кромки кувшина свободного падения почти нет.</summary>
	private const float ThrowSpeed = 520f;

	/// <summary>Выпуск фрукта выше кромки кувшина — на уровне лап лягушки.</summary>
	private const float ReleaseAboveRim = 28f;

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<Piece> _pieces = new();
	private readonly Dictionary<ulong, Piece> _piecesByBody = new();
	private readonly Queue<(Piece A, Piece B)> _pendingMerges = new();
	private readonly PhysicsMaterial _fruitMaterial = new() { Friction = FruitFriction, Bounce = Restitution };
	private StaticBody2D? _bowlBody;
	private CollisionPolygon2D? _leftWall;
	private CollisionPolygon2D? _rightWall;
	private CollisionPolygon2D? _floor;
	private FruitKind _currentKind = FruitKind.Cherry;
	private FruitKind _nextKind = FruitKind.Cherry;
	private Rect2 _bowl;
	private float _aimX;
	private float _overflowTime;
	private float _dropCooldown;
	private float _throwTimer;
	private int _score;
	private int _bestScore;
	private bool _active;
	private bool _gameOver;

	/// <summary>Запас закончился — ждём, пока последний фрукт упадёт и устаканится.</summary>
	private bool _stockExhausted;

	/// <summary>Время, в течение которого все куски почти неподвижны (для завершения раунда).</summary>
	private float _settleTime;

	/// <summary>Время с момента последнего брошенного фрукта (принудительное завершение раунда).</summary>
	private float _completionTimer;

	private Frog? _frog;
	private FallingItem? _heldFruit;
	private FallingItem? _nextFruit;
	private int _pointerIndex = -1;
	private Control? _uiRoot;
	private Label? _scoreLabel;
	private Label? _bestLabel;
	private Label? _nextLabel;
	private Label? _hintLabel;
	private Label? _stockLabel;
	private Control? _gameOverRoot;
	private Label? _gameOverTitle;
	private Button? _retryButton;
	private Label? _gameOverScore;
	/// <summary>Окно со списком собранных фруктов перед началом раунда Suika.</summary>
	private Control? _stockRoot;
	private VBoxContainer? _stockListBox;
	private Label? _stockTotal;
	/// <summary>Заранее созданные строки «вид — количество» на каждый вид из FruitCatalog.All.</summary>
	private readonly List<Label> _stockRows = new();
	private bool _stockEmpty;

	public Main? Game { get; set; }
	public event Action? ExitRequested;

	private Vector2 ViewSize => Game?.ViewSize ?? Playfield.Size;
	private Rect2 ViewBounds => new(Vector2.Zero, ViewSize);

	public void RefreshViewSize()
	{
		UpdateBowl();
		UpdateFrog(0f);
		UpdateVisuals();
	}

	public override void _Ready()
	{
		_rng.Randomize();
		_bestScore = LoadBestScore();
		BuildUi();
		BuildBowlPhysics();

		_frog = new Frog { DecorativeOnly = true };
		AddChild(_frog);
		_frog.ZIndex = 3;

		_heldFruit = CreateVisualFruit(FruitKind.Cherry, 14f);
		_heldFruit.ZIndex = 4;
		_nextFruit = CreateVisualFruit(FruitKind.Cherry, 15f);
		_nextFruit.Reparent(_uiRoot!, false);
		_nextFruit.ZIndex = 4;
		UpdateBowl();
		if (_uiRoot != null) _uiRoot.Visible = false;
		Visible = false;
	}

	/// <summary>
	/// Открывает мини-игру. Пул фруктов берётся из запаса классического режима
	/// (доступные виды и их количество). Если запас пуст, режим даже не открывается.
	/// </summary>
	public void Open()
	{
		Visible = true;
		_uiRoot!.Visible = true;
		_active = true;
		_gameOver = false;
		_stockEmpty = false;
		_stockExhausted = false;
		_settleTime = 0f;
		_completionTimer = 0f;
		_score = 0;
		_overflowTime = 0f;
		_dropCooldown = 0.25f;
		_throwTimer = 0f;
		_aimX = ViewSize.X * 0.5f;
		ClearPieces();
		UpdateBowl();
		_currentKind = PickStartFruit();
		_nextKind = PickStartFruit();
		UpdateVisuals();
		if (_heldFruit != null) _heldFruit.Visible = true;
		if (_nextFruit != null) _nextFruit.Visible = true;
		_gameOverRoot!.Visible = false;
		_scoreLabel!.Text = "Счёт  0";
		_bestLabel!.Text = $"Рекорд  {_bestScore}";
		_hintLabel!.Visible = true;
		UpdateStockLabel();

		// Окно со списком собранных фруктов: раунд начнётся после нажатия «Начать игру».
		_active = false;
		ShowStockDialog();
	}

	public void Close()
	{
		CancelInput();
		_active = false;
		_gameOver = false;
		_stockExhausted = false;
		_settleTime = 0f;
		_completionTimer = 0f;
		ClearPieces();
		if (_gameOverRoot != null) _gameOverRoot.Visible = false;
		if (_stockRoot != null) _stockRoot.Visible = false;
		if (_uiRoot != null) _uiRoot.Visible = false;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!_active) return;
		float dt = (float)delta;
		_throwTimer = Mathf.Max(0f, _throwTimer - dt);
		UpdateFrog(dt);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_active || _gameOver) return;
		float dt = (float)delta;
		_dropCooldown = Mathf.Max(0f, _dropCooldown - dt);
		foreach (Piece piece in _pieces) piece.Age += dt;
		// Сигналы контакта только ставят слияния в очередь. Меняем тела вне callback физики.
		MergeContactingPieces();
		CheckOverflow(dt);
		CheckStockEnd(dt);
	}

	public void CancelInput() => _pointerIndex = -1;

	public override void _UnhandledInput(InputEvent input) => HandleGameInput(input);

	public void HandleGameInput(InputEvent input)
	{
		if (!_active || _gameOver) return;
		if (input is InputEventScreenTouch touch)
		{
			if (touch.Canceled)
			{
				if (_pointerIndex == touch.Index) CancelInput();
			}
			else if (touch.Pressed && _pointerIndex == -1 && ViewBounds.HasPoint(touch.Position))
			{
				_pointerIndex = touch.Index;
				_aimX = touch.Position.X;
			}
			else if (!touch.Pressed && _pointerIndex == touch.Index)
			{
				_aimX = touch.Position.X;
				CancelInput();
				DropFruit();
			}
		}
		else if (input is InputEventScreenDrag drag && drag.Index == _pointerIndex)
			_aimX = drag.Position.X;
		else if (input is InputEventMouseMotion motion && _pointerIndex < 0)
			_aimX = motion.Position.X;
		else if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse)
		{
			if (mouse.Canceled) CancelInput();
			else if (mouse.Pressed && _pointerIndex == -1 && ViewBounds.HasPoint(mouse.Position))
			{
				_pointerIndex = -2;
				_aimX = mouse.Position.X;
			}
			else if (!mouse.Pressed && _pointerIndex == -2)
			{
				_aimX = mouse.Position.X;
				CancelInput();
				DropFruit();
			}
		}
		else if (input is InputEventKey { Pressed: true, Echo: false } key)
		{
			// Escape централизован в HUD: он работает и на паузе, когда Suika уже не получает ввод.
			if (key.Keycode is Key.Space or Key.Enter) DropFruit();
		}
	}

	public override void _ExitTree()
	{
		if (Game?.UiHost != null && IsInstanceValid(_uiRoot)) _uiRoot!.QueueFree();
	}

	public override void _Draw()
	{
		Vector2 size = ViewSize;
		if (size.X <= 0f || size.Y <= 0f) return;

		Color top = new("234f54");
		Color bottom = new("102d36");
		float strip = size.Y / 48f;
		for (int i = 0; i < 48; i++)
		{
			float t = i / 47f;
			DrawRect(new Rect2(0f, i * strip, size.X, strip + 1f), top.Lerp(bottom, t));
		}

		DrawCircle(new Vector2(size.X * 0.84f, size.Y * 0.19f), 94f, new Color(1f, 1f, 1f, 0.035f));
		DrawCircle(new Vector2(size.X * 0.15f, size.Y * 0.76f), 120f, new Color(0.2f, 0.9f, 0.67f, 0.035f));

		// Стеклянное тело кувшина и его толстый контур: стенки вертикальны, дно горизонтально.
		DrawRect(_bowl, new Color(0.03f, 0.14f, 0.16f, 0.72f));
		DrawLine(new Vector2(_bowl.Position.X, _bowl.Position.Y), new Vector2(_bowl.End.X, _bowl.Position.Y), new Color("9ee4c3"), 3f);
		DrawLine(new Vector2(_bowl.Position.X, _bowl.Position.Y), new Vector2(_bowl.Position.X, _bowl.End.Y), new Color("7acbb2"), 6f);
		DrawLine(new Vector2(_bowl.End.X, _bowl.Position.Y), new Vector2(_bowl.End.X, _bowl.End.Y), new Color("7acbb2"), 6f);
		DrawLine(new Vector2(_bowl.Position.X, _bowl.End.Y), new Vector2(_bowl.End.X, _bowl.End.Y), new Color("9ee4c3"), 8f);
		DrawLine(new Vector2(_bowl.Position.X, _bowl.Position.Y + 30f), new Vector2(_bowl.End.X, _bowl.Position.Y + 30f), new Color(0.62f, 0.93f, 0.8f, 0.18f), 2f);

		// Линия переполнения помогает понять, когда кувшин уже заполнен.
		float dangerY = _bowl.Position.Y + OverflowRimOffset;
		DrawLine(new Vector2(_bowl.Position.X + 5f, dangerY), new Vector2(_bowl.End.X - 5f, dangerY), new Color(1f, 0.55f, 0.38f, 0.33f), 2f);
	}


	private void BuildBowlPhysics()
	{
		_bowlBody = new StaticBody2D
		{
			Name = "BowlBody",
			CollisionLayer = SuikaCollisionLayer,
			CollisionMask = SuikaCollisionLayer,
			PhysicsMaterialOverride = new PhysicsMaterial { Friction = BowlFriction, Bounce = 0f }
		};
		AddChild(_bowlBody);
		_leftWall = new CollisionPolygon2D { Name = "LeftWall" };
		_rightWall = new CollisionPolygon2D { Name = "RightWall" };
		_floor = new CollisionPolygon2D { Name = "Floor" };
		_bowlBody.AddChild(_leftWall);
		_bowlBody.AddChild(_rightWall);
		_bowlBody.AddChild(_floor);
	}

	private void UpdateBowl()
	{
		Vector2 view = ViewSize;
		float width = Mathf.Max(BowlMinWidth, view.X * BowlWidthRatio);
		float height = Mathf.Max(BowlMinHeight, view.Y * BowlHeightRatio);
		Rect2 previous = _bowl;
		_bowl = new Rect2((view.X - width) * 0.5f, view.Y - BowlBottom - height, width, height);
		if (_bowl == previous) return;

		// Внутренние грани прямоугольные: боковые стенки вертикальны, дно горизонтально. Верх кувшина открыт.
		float left = _bowl.Position.X;
		float right = _bowl.End.X;
		float top = _bowl.Position.Y;
		float bottom = _bowl.End.Y;
		_leftWall!.Polygon = new[]
		{
			new Vector2(left - WallThickness, top), new Vector2(left, top),
			new Vector2(left, bottom), new Vector2(left - WallThickness, bottom)
		};
		_rightWall!.Polygon = new[]
		{
			new Vector2(right, top), new Vector2(right + WallThickness, top),
			new Vector2(right + WallThickness, bottom), new Vector2(right, bottom)
		};
		_floor!.Polygon = new[]
		{
			new Vector2(left - WallThickness, bottom - FloorInset),
			new Vector2(right + WallThickness, bottom - FloorInset),
			new Vector2(right + WallThickness, bottom + WallThickness),
			new Vector2(left - WallThickness, bottom + WallThickness)
		};

		if (previous.HasArea())
		{
			foreach (Piece piece in _pieces)
			{
				if (piece.Removed || piece.Body.IsQueuedForDeletion()) continue;
				// Переносим стопку вместе с дном, сохраняя размеры и скорости тел.
				Vector2 position = piece.Body.Position + new Vector2(0f, _bowl.End.Y - previous.End.Y);
				piece.Body.Position = FitPieceInsideBowl(piece, position);
				piece.Body.ResetPhysicsInterpolation();
				piece.Body.Sleeping = false;
			}
		}

		QueueRedraw();
	}

	private void UpdateFrog(float dt)
	{
		if (_frog == null) return;
		float x = Mathf.Clamp(_aimX, _bowl.Position.X + 45f, _bowl.End.X - 45f);
		_aimX = x;
		_frog.Position = new Vector2(x, 103f);
		_frog.SyncUiScale(0.56f);
		_frog.ThrowPose = _throwTimer > 0f;
		_frog.SetLookTarget(new Vector2(x, _bowl.Position.Y));
		if (_heldFruit != null)
		{
			_heldFruit.Position = new Vector2(x, _bowl.Position.Y - 28f);
		}
	}

	private void UpdateVisuals()
	{
		if (_heldFruit == null || _nextFruit == null) return;
		_heldFruit.Kind = _currentKind;
		_heldFruit.QueueRedraw();
		_nextFruit.Kind = _nextKind;
		_nextFruit.QueueRedraw();
		_nextFruit.Position = new Vector2(_uiRoot!.Size.X - 90f, 67f);
	}

	/// <summary>Случайный фрукт из текущего запаса (только виды с остатком &gt; 0).</summary>
	private FruitKind PickStartFruit()
	{
		FruitKind[] pool = Game?.GetSuikaFruitPool() ?? Array.Empty<FruitKind>();
		if (pool.Length == 0) return FruitKind.Cherry;
		return pool[_rng.RandiRange(0, pool.Length - 1)];
	}

	/// <summary>
	/// Перекидывает текущий фрукт на случайный доступный из запаса, если нынешний
	/// уже исчерпан (или запас пуст). Возвращает false, если бросить больше нечего.
	/// </summary>
	private bool RePickCurrent()
	{
		FruitKind[] pool = Game?.GetSuikaFruitPool() ?? Array.Empty<FruitKind>();
		if (pool.Length == 0) return false;
		_currentKind = pool[_rng.RandiRange(0, pool.Length - 1)];
		UpdateVisuals();
		return true;
	}

	private void DropFruit()
	{
		if (!_active || _gameOver || _dropCooldown > 0f || _stockExhausted) return;

		// Если текущий вид уже израсходован (например, совпадал со «следующим»),
		// перекидываем его на доступный. Если бросать нечего — завершаем раунд.
		if ((Game?.FruitStock(_currentKind) ?? 0) <= 0 && !RePickCurrent())
		{
			SetStockExhausted();
			return;
		}

		// Бросок тратит одну единицу фрукта из запаса классического режима.
		if (Game == null || !Game.ConsumeFruit(_currentKind))
		{
			return;
		}
		UpdateStockLabel();

		FruitShape shape = FruitShape.Of(_currentKind);
		float radius = FruitRadius(_currentKind);
		float x = ClampDropX(_aimX, radius * shape.HitX);
		// Фрукт вылетает из лап лягушки с заметной скоростью: у самой кромки кувшина
		// свободного падения почти нет, и слабый бросок не двигал горку.
		CreatePiece(_currentKind,
			new Vector2(x, _bowl.Position.Y - ReleaseAboveRim),
			new Vector2(_rng.RandfRange(-DropScatter, DropScatter), ThrowSpeed));
		_throwTimer = 0.24f;
		_currentKind = _nextKind;
		_nextKind = PickStartFruit();
		_dropCooldown = 0.3f;
		_hintLabel!.Visible = false;
		UpdateVisuals();

		// Запас закончился — больше бросать нечего, но даём последнему фрукту
		// упасть, столкнуться и слиться (возможны доп. очки), затем завершаем.
		if (Game.TotalFruitStock() <= 0)
		{
			SetStockExhausted();
			return;
		}

		// «Следующий» мог быть того же вида, что только что съеденный — если он
		// исчерпан, перекидываем текущий на оставшийся доступный вид.
		if (Game.FruitStock(_currentKind) <= 0)
		{
			RePickCurrent();
		}
	}

	/// <summary>
	/// Помечает, что запас фруктов исчерпан: прячет фрукты «в лапе»/«следующий»
	/// и запускает ожидание, пока последний кусок устаканится (см. CheckStockEnd).
	/// </summary>
	private void SetStockExhausted()
	{
		_stockExhausted = true;
		_settleTime = 0f;
		_completionTimer = 0f;
		if (_heldFruit != null) _heldFruit.Visible = false;
		if (_nextFruit != null) _nextFruit.Visible = false;
		if (_hintLabel != null) _hintLabel.Visible = false;
	}

	/// <summary>
	/// Пока запас исчерпан, ждём, пока все куски почти остановятся (могут слиться),
	/// и по истечении паузы завершаем раунд.
	/// Столкновения могут долго не давать кускам устаканиться — таймер
	/// гарантирует завершение раунда не позднее чем через CompletionTimeout секунд.
	/// </summary>
	private void CheckStockEnd(float dt)
	{
		if (!_stockExhausted || _gameOver)
		{
			return;
		}

		// Жёсткий дедлайн: даже если куски продолжают сталкиваться, раунд завершаем.
		_completionTimer += dt;
		if (_completionTimer >= CompletionTimeout)
		{
			EndRound();
			return;
		}

		if (_pieces.Count == 0)
		{
			return;
		}

		float maxSpeed = 0f;
		foreach (Piece piece in _pieces)
		{
			if (piece.Body.Sleeping) continue;
			float speed = piece.Body.LinearVelocity.Length()
				+ Mathf.Abs(piece.Body.AngularVelocity) * piece.Radius;
			maxSpeed = Mathf.Max(maxSpeed, speed);
		}
		if (maxSpeed < SettleSpeed)
		{
			_settleTime += dt;
			if (_settleTime >= SettleDelay)
			{
				EndRound();
			}
		}
		else
		{
			_settleTime = 0f;
		}
	}


	private void CheckOverflow(float dt)
	{
		bool overflowing = false;
		float danger = _bowl.Position.Y + OverflowRimOffset;
		foreach (Piece piece in _pieces)
		{
			// Новый фрукт пересекает линию по пути вниз, поэтому учитываем только
			// объект, который уже пробыл в кувшине заметное время.
			Vector2 position = piece.Body.Position;
			bool aboveLine = position.Y - ShapeExtent(piece, Vector2.Down) < danger;
			bool outsideBowl = position.X < _bowl.Position.X || position.X > _bowl.End.X || position.Y > _bowl.End.Y;
			if (piece.Age > 0.45f && (aboveLine || outsideBowl))
			{
				overflowing = true;
				break;
			}
		}
		_overflowTime = overflowing ? _overflowTime + dt : Mathf.Max(0f, _overflowTime - dt * 2f);
		if (_overflowTime >= OverflowDelay) EndRound();
	}

	private void EndRound()
	{
		_gameOver = true;
		_active = true;
		_pendingMerges.Clear();
		foreach (Piece piece in _pieces)
		{
			piece.Body.Freeze = true;
		}
		if (_score > _bestScore)
		{
			_bestScore = _score;
			SaveBestScore(_bestScore);
		}
		_bestLabel!.Text = $"Рекорд  {_bestScore}";
		_gameOverScore!.Text = $"Счёт: {_score}";
		_gameOverRoot!.Visible = true;
		UpdateStockLabel();

		// Если запас фруктов закончился — «Играть ещё» недоступен.
		_stockEmpty = Game?.TotalFruitStock() <= 0;
		if (_gameOverTitle != null)
		{
			_gameOverTitle.Text = _stockEmpty ? "Фрукты закончились" : "Кувшин полон";
		}
		if (_retryButton != null)
		{
			_retryButton.Visible = !_stockEmpty;
		}

		// ModalContent обновит положение и масштаб после изменения содержимого.
	}

	private void Restart()
	{
		Open();
	}



	private static int LoadBestScore()
	{
		var config = new ConfigFile();
		if (config.Load("user://save.cfg") != Error.Ok) return 0;
		return (int)config.GetValue("suika", "high", 0);
	}

	private static void SaveBestScore(int score)
	{
		var config = new ConfigFile();
		config.Load("user://save.cfg");
		config.SetValue("suika", "high", score);
		config.Save("user://save.cfg");
	}
}
