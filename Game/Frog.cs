using System;
using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Лягушка с вытягивающимися руками. Пока палец зажат — руки растут в направлении
/// точки касания (направление можно менять, двигая палец). Отпустил — руки втягиваются.
/// Тело — спрайт из Game/assets/frogs (позы idle/catching/eating/angry), а руки и ладони
/// рисуются процедурно: они динамически следуют за пальцем и задают хитбоксы ловли.
/// Вся геометрия рук хранится в «мировых» единицах (как и границы экрана), поэтому руки
/// всегда дотягиваются до любого угла экрана на любом разрешении.
/// </summary>
public partial class Frog : Node2D
{
	/// <summary>Радиус хитбокса ладони в локальных единицах — чуть больше нарисованной ладони (15f), до пальцев.</summary>
	public const float HandHitRadiusLocal = 16f;

	/// <summary>На сколько дольше максимально возможной дистанции тянутся руки.</summary>
	private const float ReachMargin = 1.06f;
	private const float MinReach = 340f;

	/// <summary>Время полного вытягивания рук от нуля до максимума, секунды.</summary>
	private const float FullExtendTime = 0.8f;
	private const float RetractScale = 1.55f;

	/// <summary>Разворот ладоней от направления прицела: верхняя рука на +13°, нижняя на −13°.</summary>
	public const float HandSpreadDeg = 13f;

	private const float MinCatchLengthPx = 50f;

	/// <summary>Отступ ладони от края экрана (в базовых единицах, затем умножается на UiScale).</summary>
	private const float HandScreenMargin = 24f;

	/// <summary>Сколько секунд лягушка находится в позе «ест» после доставки предмета.</summary>
	private const float EatingStateTime = 0.7f;

	/// <summary>Высота персонажа в локальных единицах (как у старого процедурного тела).</summary>
	private const float BodyVisualHeight = 85f;

	/// <summary>Высота текстуры тела из Game/assets/frogs.</summary>
	private const float BodyTextureHeight = 179f;

	/// <summary>Позы тела лягушки — соответствуют файлам в Game/assets/frogs.</summary>
	private enum FrogPose
	{
		Idle,
		Catching,
		Eating,
		Angry,
	}

	/// <summary>Кэш текстур тела по позе.</summary>
	private static readonly Dictionary<FrogPose, Texture2D> BodyTextures = new();

	private static readonly Color Skin = new("55a82e");
	private static readonly Color SkinDark = new("2f7d2f");
	private static readonly Color Hand = new("b4e863");
	private static readonly Color HandDark = new("3a8a2a");

	/// <summary>Ссылка на корень игры (устанавливается из Main).</summary>
	public Main? Game { get; set; }

	/// <summary>Режим декоративной лягушки для экранов, где ловля не используется.</summary>
	public bool DecorativeOnly { get; set; }

	/// <summary>Короткая вытянутая поза броска для декоративного режима.</summary>
	public bool ThrowPose { get; set; }

	private Vector2 _direction = Vector2.Right;
	private Vector2 _aimTarget;
	private Vector2 _moveDirection;
	private float _moveTurnTimer;
	private bool _randomMovement;
	/// <summary>Длины рук в «мировых» единицах экрана.</summary>
	private readonly float[] _armLengthsWorld = new float[2];
	private float _armGrowSpeedWorld;
	private float _armRetractSpeedWorld;
	/// <summary>Профиль провиса рук: предел длины и масштаб поля (см. Game/Core/ArmSag.cs).</summary>
	private ArmSag.Profile _sag = new(MinReach, 1f);
	private bool _holding;
	private int _pointerIndex = -1;
	private float _flash;
	private bool _pulsing;
	private float _pulseT;
	private float _currentUiScale = 1f;
	private Sprite2D? _body;
	private float _eatingTimer;

	/// <summary>Пойманный предмет, зажатый в ладони (с индексом держащей руки).</summary>
	private struct CaughtItem
	{
		public FallingItem Item;
		public int HandIndex;
	}

	private readonly List<CaughtItem> _caughtItems = new();

	/// <summary>Предмет «доехал» до лягушки — после полного возврата рук.</summary>
	public event Action<FallingItem>? CaughtItemReturned;

	private bool CanAct => !DecorativeOnly && Game?.State == GameState.Playing;

	/// <summary>Текущий коэффициент масштаба UI/мира, чтобы лягушка не «худела» на больших экранах.</summary>
	public float UiScale => _currentUiScale;

	/// <summary>Высота видимой части тела в мировых единицах: спрайт центрирован по Position.</summary>
	public float VisualHeight => BodyVisualHeight * _currentUiScale;

	/// <summary>Радиус хитбокса ладони в мировых единицах (учитывает масштаб ноды и множитель перка «Липкие ладони»).</summary>
	public float HandHitRadiusWorld => HandHitRadiusLocal * _currentUiScale * CatchRadiusMultiplier;

	/// <summary>Множитель радиуса хитбокса ладони (перк «Липкие ладони»). Выставляется из Main.</summary>
	public float CatchRadiusMultiplier { get; set; } = 1f;

	/// <summary>Множитель скорости вытягивания рук (перк «Скорострельность»).</summary>
	public float ExtendSpeedMultiplier { get; set; } = 1f;

	/// <summary>Множитель скорости втягивания рук (перк «Скорострельность»).</summary>
	public float RetractSpeedMultiplier { get; set; } = 1f;

	/// <summary>Разрешает держать несколько предметов в одной ладони (перк «Липкие ладони» ур. 5).</summary>
	public bool MultiCatch { get; set; }

	/// <summary>Сколько предметов максимально в одной ладони при MultiCatch.</summary>
	private const int MaxItemsPerHand = 2;

	/// <summary>Обновляет текущий масштаб ноды из корня игры.</summary>
	public void SyncUiScale(float uiScale)
	{
		_currentUiScale = uiScale;
		if (!_pulsing) Scale = Vector2.One * uiScale;
		_sag = new ArmSag.Profile(Game != null ? ComputeMaxArmLength(Game.ViewSize) : _sag.MaxArmLength, uiScale);
		ConstrainHands();
		UpdateBodyState();
	}

	/// <summary>Идёт ли ловля прямо сейчас: руки достаточно вытянуты (во время роста, удержания или втягивания).</summary>
	public bool IsCatching => IsHandCatching(0) || IsHandCatching(1);

	public bool IsHandCatching(int handIndex) => handIndex >= 0 && handIndex < _armLengthsWorld.Length
		&& _armLengthsWorld[handIndex] >= MinCatchLengthPx * _currentUiScale;

	/// <summary>Движение тела до проверки столкновений ладоней в Main.</summary>
	public void UpdateMovement(float dt)
	{
		if (!CanAct || Game == null || dt <= 0f)
		{
			return;
		}
		float speed = Difficulty.FrogSpeed(Game.Score);
		if (speed <= 0f)
		{
			return;
		}

		bool randomMovement = Game.Score >= Difficulty.FrogRandomMovementScore;
		_moveTurnTimer -= dt;
		if (_moveDirection == Vector2.Zero || _moveTurnTimer <= 0f || randomMovement != _randomMovement)
		{
			if (randomMovement)
			{
				_moveDirection = Vector2.FromAngle(Game.GameRng.RandfRange(0f, Mathf.Tau));
			}
			else
			{
				// Чередуем горизонтальные и вертикальные отрезки, без диагоналей.
				float sign = Game.GameRng.Randf() < 0.5f ? -1f : 1f;
				_moveDirection = _moveDirection.X == 0f ? Vector2.Right * sign : Vector2.Down * sign;
			}
			_randomMovement = randomMovement;
			_moveTurnTimer = randomMovement ? Difficulty.FrogRandomTurnInterval : Difficulty.FrogAxisTurnInterval;
		}

		Position += _moveDirection * speed * _currentUiScale * dt;
		ConstrainMovement();
	}

	/// <summary>Удерживает тело на игровом поле и отражает движение от краёв.</summary>
	public void ConstrainMovement()
	{
		if (Game == null || DecorativeOnly)
		{
			return;
		}
		Vector2 size = Game.ViewSize;
		// Размер тела задаёт только родитель; запас учитывает также Pulse.
		float bodyMargin = BodyVisualHeight * 0.7f * _currentUiScale;
		Vector2 margin = new(bodyMargin, bodyMargin + 90f * _currentUiScale);
		margin.X = Mathf.Min(margin.X, size.X * 0.5f);
		margin.Y = Mathf.Min(margin.Y, size.Y * 0.5f);
		Vector2 end = size - margin;
		Vector2 position = Position;
		if ((position.X <= margin.X && _moveDirection.X < 0f) || (position.X >= end.X && _moveDirection.X > 0f))
		{
			_moveDirection.X = -_moveDirection.X;
		}
		if ((position.Y <= margin.Y && _moveDirection.Y < 0f) || (position.Y >= end.Y && _moveDirection.Y > 0f))
		{
			_moveDirection.Y = -_moveDirection.Y;
		}
		Position = new Vector2(Mathf.Clamp(position.X, margin.X, end.X), Mathf.Clamp(position.Y, margin.Y, end.Y));
		if (_holding)
		{
			// Палец может стоять на месте, пока лягушка движется под ним.
			UpdateDirection(_aimTarget);
		}
		ConstrainHands();
	}

	/// <summary>Новая партия начинается без движения и оставшегося удержания рук.</summary>
	public void ResetForRound()
	{
		_moveDirection = Vector2.Zero;
		_moveTurnTimer = 0f;
		_randomMovement = false;
		_direction = Vector2.Right;
		CancelInput();
		Array.Clear(_armLengthsWorld);
		_flash = 0f;
		_eatingTimer = 0f;
		_pulsing = false;
		Scale = Vector2.One * _currentUiScale;
		ClearCaughtItems();
		UpdateBodyState();
		QueueRedraw();
	}

	private static string TexturePath(FrogPose pose) => pose switch
	{
		FrogPose.Catching => "res://Game/assets/frogs/catching.png",
		FrogPose.Eating => "res://Game/assets/frogs/eating.png",
		FrogPose.Angry => "res://Game/assets/frogs/angry.png",
		_ => "res://Game/assets/frogs/idle.png",
	};

	private static Texture2D LoadBodyTexture(FrogPose pose)
	{
		if (BodyTextures.TryGetValue(pose, out Texture2D? cached))
		{
			return cached;
		}
		Texture2D loaded = GD.Load<Texture2D>(TexturePath(pose));
		BodyTextures[pose] = loaded;
		return loaded;
	}

	public override void _Ready()
	{
		_body = new Sprite2D { Centered = true };
		AddChild(_body);
		UpdateBodyState();
	}

	/// <summary>Текущая поза: урон важнее «еды», «еда» важнее ловли.</summary>
	private FrogPose CurrentPose()
	{
		if (_flash > 0f)
		{
			return FrogPose.Angry;
		}
		if (_eatingTimer > 0f)
		{
			return FrogPose.Eating;
		}
		if (DecorativeOnly ? ThrowPose : IsCatching)
		{
			return FrogPose.Catching;
		}
		return FrogPose.Idle;
	}

	/// <summary>Обновляет текстуру и позиционирование тела по текущему состоянию.</summary>
	private void UpdateBodyState()
	{
		if (_body == null)
		{
			return;
		}
		UpdateBodyTransform();

		FrogPose pose = CurrentPose();
		Texture2D texture = LoadBodyTexture(pose);
		if (_body.Texture != texture)
		{
			_body.Texture = texture;
		}
	}

	/// <summary>Масштабирует тело под UiScale, зеркалирует к направлению рук и покачивает.</summary>
	private void UpdateBodyTransform()
	{
		if (_body == null)
		{
			return;
		}
		float unit = BodyVisualHeight / BodyTextureHeight;
		float flip = _direction.X < -0.01f ? -1f : 1f;
		_body.Scale = new Vector2(unit * flip, unit);
		// Лёгкое покачивание как в старом процедурном рисунке.
		float bob = Mathf.Sin((float)Time.GetTicksMsec() / 380f) * 2.2f;
		_body.Position = new Vector2(0f, bob);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (DecorativeOnly)
		{
			_flash = Mathf.Max(0f, _flash - dt);
			_eatingTimer = Mathf.Max(0f, _eatingTimer - dt);
			UpdateBodyState();
			QueueRedraw();
			return;
		}
		if (Game == null)
		{
			return;
		}

		_flash = Mathf.Max(0f, _flash - dt);
		_eatingTimer = Mathf.Max(0f, _eatingTimer - dt);

		float uiScale = UiScale;
		if (_pulsing)
		{
			_pulseT = Mathf.Min(1f, _pulseT + dt * 5f);
			Scale = Vector2.One * uiScale * Mathf.Lerp(1.18f, 1f, Mathf.SmoothStep(0f, 1f, _pulseT));
			if (_pulseT >= 1f)
			{
				_pulsing = false;
			}
		}
		else
		{
			Scale = Vector2.One * uiScale;
		}

		UpdateBodyState();
		QueueRedraw();
	}

	/// <summary>Обе руки обновляются на физическом такте до проверки ловли в Main.</summary>
	public void UpdateArms(float dt)
	{
		if (!CanAct || Game == null) return;
		_sag = new ArmSag.Profile(ComputeMaxArmLength(Game.ViewSize), _currentUiScale);
		UpdateArmLengths(_sag.MaxArmLength);
		for (int i = 0; i < _armLengthsWorld.Length; i++)
		{
			_armLengthsWorld[i] = _holding
				? _armLengthsWorld[i] + _armGrowSpeedWorld * dt
				: Mathf.Max(0f, _armLengthsWorld[i] - _armRetractSpeedWorld * dt);
		}
		ConstrainHands();
		UpdateCaughtItems();
	}

	/// <summary>Каждая рука имеет свой предел; поворот, движение и ресайз не выпускают ладони за экран.</summary>
	public void ConstrainHands()
	{
		if (Game == null || DecorativeOnly) return;
		for (int i = 0; i < _armLengthsWorld.Length; i++)
		{
			_armLengthsWorld[i] = Mathf.Min(_armLengthsWorld[i], ComputeMaxArmLengthInDirection(Game.ViewSize, i));
		}
		foreach (CaughtItem entry in _caughtItems)
		{
			if (IsInstanceValid(entry.Item) && !entry.Item.IsQueuedForDeletion())
			{
				entry.Item.GlobalPosition = HandPositionWorld(entry.HandIndex);
			}
		}
		QueueRedraw();
	}

	public void CancelInput()
	{
		_holding = false;
		_pointerIndex = -1;
	}

	public override void _UnhandledInput(InputEvent input) => HandleGameInput(input);

	public void HandleGameInput(InputEvent input)
	{
		if (input is InputEventScreenTouch touch)
		{
			if (touch.Canceled || !touch.Pressed)
			{
				if (touch.Index == _pointerIndex) CancelInput();
			}
			else if (CanAct && !_holding && Game != null && Game.ViewBounds.HasPoint(touch.Position))
			{
				_pointerIndex = touch.Index;
				_holding = true;
				_aimTarget = touch.Position;
				UpdateDirection(touch.Position);
			}
		}
		else if (input is InputEventScreenDrag drag && _holding && CanAct && drag.Index == _pointerIndex)
		{
			_aimTarget = drag.Position;
			UpdateDirection(drag.Position);
		}
	}

	/// <summary>
	/// Максимальная длина рук (мировые единицы), которая гарантирует доставание
	/// до самого дальнего угла видимой области.
	/// </summary>
	public float ComputeMaxArmLength(Vector2 viewSize)
	{
		float maxViewDist = GlobalPosition.DistanceTo(Vector2.Zero);
		maxViewDist = Mathf.Max(maxViewDist, GlobalPosition.DistanceTo(new Vector2(viewSize.X, 0f)));
		maxViewDist = Mathf.Max(maxViewDist, GlobalPosition.DistanceTo(new Vector2(0f, viewSize.Y)));
		maxViewDist = Mathf.Max(maxViewDist, GlobalPosition.DistanceTo(new Vector2(viewSize.X, viewSize.Y)));
		return Mathf.Max(MinReach, maxViewDist * ReachMargin);
	}

	/// <summary>
	/// Максимальная длина одной руки до края с запасом под пальцы и увеличенный хитбокс перка.
	/// Провис опускает ладонь тем сильнее, чем длиннее рука, поэтому предел по высоте
	/// считается вместе с ним.
	/// </summary>
	private float ComputeMaxArmLengthInDirection(Vector2 viewSize, int handIndex)
	{
		float margin = Mathf.Max(HandScreenMargin * _currentUiScale, HandHitRadiusWorld);
		Vector2 dir = HandDirection(handIndex);
		// Провис вертикальный: на предел по горизонтали он не влияет.
		float cap = Mathf.Min(MaxArmLengthAlongX(viewSize, dir, margin), _sag.MaxArmLength);
		return _sag.FitLengthInsideHeight(GlobalPosition, dir, DownWorld, margin, viewSize.Y - margin, cap);
	}

	/// <summary>Предел длины руки по горизонтали: дальше ладонь вышла бы за боковую кромку.</summary>
	private float MaxArmLengthAlongX(Vector2 viewSize, Vector2 dir, float margin)
	{
		if (dir.X > 0.0001f)
		{
			return (viewSize.X - margin - GlobalPosition.X) / dir.X;
		}
		if (dir.X < -0.0001f)
		{
			return (margin - GlobalPosition.X) / dir.X;
		}
		return float.PositiveInfinity;
	}

	/// <summary>Мировые позиции обеих ладоней (в координатах сцены).</summary>
	public Vector2[] GetHandPositions()
	{
		if (_armLengthsWorld[0] <= 2f && _armLengthsWorld[1] <= 2f)
		{
			return Array.Empty<Vector2>();
		}

		return new[]
		{
			HandPositionWorld(0),
			HandPositionWorld(1),
		};
	}

	/// <summary>
	/// Хитбоксы обеих ладоней в мировых координатах: центры совпадают с позициями ладоней,
	/// радиус — с размером видимой ладони. Используются для проверки реального касания предмета.
	/// </summary>
	public CircleHitbox[] GetHandHitboxes()
	{
		if (_armLengthsWorld[0] <= 2f && _armLengthsWorld[1] <= 2f)
		{
			return Array.Empty<CircleHitbox>();
		}

		return new[]
		{
			new CircleHitbox(HandPositionWorld(0), HandHitRadiusWorld),
			new CircleHitbox(HandPositionWorld(1), HandHitRadiusWorld),
		};
	}

	/// <summary>
	/// Мировая позиция ладони (0 — верхняя, 1 — нижняя): направление руки плюс её провис.
	/// Общая для ловли, «прилипших» фруктов и магнита перка.
	/// </summary>
	private Vector2 HandPositionWorld(int handIndex) => HandPositionWorld(handIndex, _armLengthsWorld[handIndex]);

	/// <summary>Мировая позиция ладони при заданной длине руки — нужна для поиска предела у кромок.</summary>
	private Vector2 HandPositionWorld(int handIndex, float armLength) =>
		_sag.HandPosition(GlobalPosition, HandDirection(handIndex), DownWorld, armLength);

	/// <summary>Направление, в которое провисают ладони (вниз по экрану).</summary>
	private Vector2 DownWorld => Vector2.Down.Rotated(GlobalRotation);

	private Vector2 HandDirection(int handIndex) => _direction.Rotated(Mathf.DegToRad(handIndex == 0 ? HandSpreadDeg : -HandSpreadDeg));

	/// <summary>Реакция на урон мусором: на короткое время включает позу «злая».</summary>
	public void Flash() => _flash = 0.45f;

	/// <summary>Маленький подпрыг, когда что-то поймали (без конфликта с масштабом).</summary>
	public void Pulse()
	{
		_pulsing = true;
		_pulseT = 0f;
	}

	/// <summary>Взять фрукт в ладонь: предмет перестаёт падать и следует за рукой.</summary>
	/// <returns>true, если предмет взят; false, если ладонь уже занята.</returns>
	public bool AttachCaughtItem(FallingItem item, int handIndex)
	{
		if (item == null || !CanHold(handIndex))
		{
			return false;
		}
		item.IsCaught = true;
		_caughtItems.Add(new CaughtItem { Item = item, HandIndex = handIndex });
		item.GlobalPosition = HandPositionWorld(handIndex);
		return true;
	}

	/// <summary>Свободна ли ладонь (обычно не более одного предмета; при MultiCatch — два).</summary>
	public bool CanHold(int handIndex)
	{
		if (handIndex < 0 || handIndex >= 2)
		{
			return false;
		}
		int limit = MultiCatch ? MaxItemsPerHand : 1;
		int count = 0;
		foreach (CaughtItem held in _caughtItems)
		{
			if (held.HandIndex == handIndex)
			{
				count++;
			}
		}
		return count < limit;
	}

	/// <summary>Сбрасывает список пойманных предметов (например, при рестарте).</summary>
	public void ClearCaughtItems() => _caughtItems.Clear();

	/// <summary>
	/// Пока руки не вернулись полностью — пойманный предмет следует за своей ладонью.
	/// Как только длина рук стала нулевой — фрукт «сдан» лягушке (событие начисляет очки и удаляет предмет).
	/// </summary>
	private void UpdateCaughtItems()
	{
		List<FallingItem>? returned = null;
		for (int i = _caughtItems.Count - 1; i >= 0; i--)
		{
			CaughtItem entry = _caughtItems[i];
			if (!IsInstanceValid(entry.Item) || entry.Item.IsQueuedForDeletion())
			{
				_caughtItems.RemoveAt(i);
				continue;
			}
			if (_armLengthsWorld[entry.HandIndex] <= 0f)
			{
				_caughtItems.RemoveAt(i);
				(returned ??= new List<FallingItem>()).Add(entry.Item);
			}
		}
		if (returned == null) return;
		_eatingTimer = EatingStateTime;
		// Сначала убираем доставленные фрукты из рук: награда может открыть меню перков.
		foreach (FallingItem item in returned) CaughtItemReturned?.Invoke(item);
	}

	private void UpdateArmLengths(float maxWorld)
	{
		// Базовые скорости умножаются на множители перка «Скорострельность».
		float baseSpeed = maxWorld / FullExtendTime;
		_armGrowSpeedWorld = baseSpeed * ExtendSpeedMultiplier;
		_armRetractSpeedWorld = baseSpeed * RetractScale * RetractSpeedMultiplier;
	}

	private void UpdateDirection(Vector2 target)
	{
		Vector2 to = target - GlobalPosition;
		if (to.LengthSquared() > 4f)
		{
			_direction = to.Normalized();
			ConstrainHands();
			UpdateBodyState();
			QueueRedraw();
		}
	}

	/// <summary>Поворачивает взгляд лягушки без запуска режима ловли.</summary>
	public void SetLookTarget(Vector2 target) => UpdateDirection(target);

	public override void _Draw()
	{
		// Геометрия ладоней — в мировых единицах. Pulse увеличивает тело, не сдвигая руки за край.
		float unit = _currentUiScale / Mathf.Max(0.001f, Mathf.Abs(GlobalScale.X));
		for (int i = 0; i < _armLengthsWorld.Length; i++)
		{
			if (!(DecorativeOnly && ThrowPose) && _armLengthsWorld[i] <= 2f) continue;
			DrawArm(i, unit);
		}
	}

	/// <summary>
	/// Рука рисуется изогнутой ломаной: чем дальше вытянута, тем сильнее провисает.
	/// Концом кривой остаётся ладонь, поэтому рисунок и хитбокс ловли совпадают.
	/// </summary>
	private void DrawArm(int handIndex, float unit)
	{
		Vector2 dirLocal = HandDirection(handIndex).Rotated(-GlobalRotation);
		Vector2 handLocal = DecorativeOnly ? dirLocal * 54f : ToLocal(HandPositionWorld(handIndex));
		float dropLocal = _sag.Drop(_armLengthsWorld[handIndex]) * unit;
		Vector2 control = ArmSag.ControlPoint(Vector2.Zero, handLocal, Vector2.Down, dropLocal);

		Vector2 previous = Vector2.Zero;
		for (int segment = 1; segment <= ArmSag.Segments; segment++)
		{
			Vector2 point = ArmSag.Point(Vector2.Zero, control, handLocal, segment / (float)ArmSag.Segments);
			DrawLine(previous, point, SkinDark, 10f * unit);
			DrawLine(previous, point, Skin, 5f * unit);
			previous = point;
		}
		DrawCircle(handLocal, 15f * unit, HandDark);
		DrawCircle(handLocal, 12f * unit, Hand);

		// Кисть свисает: чем сильнее провис, тем больше ладонь смотрит вниз.
		float tilt = DecorativeOnly ? 0f : ArmSag.PalmTilt(_sag.Amount(_armLengthsWorld[handIndex]));
		float angle = dirLocal.Angle() + tilt;
		for (int fingerIndex = -1; fingerIndex <= 1; fingerIndex++)
		{
			Vector2 finger = Vector2.FromAngle(angle + fingerIndex * 0.75f);
			DrawCircle(handLocal + finger * (14f * unit), 4.5f * unit, Hand);
		}
	}
}
