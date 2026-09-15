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
	private const float HandSpreadDeg = 13f;
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
	/// <summary>Длины рук в «мировых» единицах экрана.</summary>
	private float _armLengthWorld;
	private float _armGrowSpeedWorld;
	private float _armRetractSpeedWorld;
	private bool _holding;
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
		UpdateBodyState();
	}

	/// <summary>Идёт ли ловля прямо сейчас: руки достаточно вытянуты (во время роста, удержания или втягивания).</summary>
	public bool IsCatching => _armLengthWorld >= MinCatchLengthPx * _currentUiScale;

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
		float unit = BodyVisualHeight * _currentUiScale / BodyTextureHeight;
		float flip = _direction.X < -0.01f ? -1f : 1f;
		_body.Scale = new Vector2(unit * flip, unit);
		// Лёгкое покачивание как в старом процедурном рисунке.
		float bob = Mathf.Sin((float)Time.GetTicksMsec() / 380f) * 2.2f * _currentUiScale;
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

		// Пересчитываем длины рук под текущее разрешение экрана (скорости — от глобального максимума).
		UpdateArmLengths(ComputeMaxArmLength(Game.ViewSize));

		// Максимальная длина в текущем направлении, чтобы ладони не выходили за пределы экрана.
		float maxForDir = ComputeMaxArmLengthInDirection(Game.ViewSize);

		if (_holding && CanAct)
		{
			_armLengthWorld = Mathf.Min(maxForDir, _armLengthWorld + _armGrowSpeedWorld * dt);
		}
		else if (_armLengthWorld > 0f)
		{
			_armLengthWorld = Mathf.Max(0f, _armLengthWorld - _armRetractSpeedWorld * dt);
		}

		// Ограничение по границе экрана: если длина оказалась больше допустимой для текущего
		// направления (например, направление резко сменилось), плавно втягиваем её до границы.
		if (_armLengthWorld > maxForDir)
		{
			_armLengthWorld = Mathf.Max(maxForDir, _armLengthWorld - _armRetractSpeedWorld * dt);
		}

		// Пока руки не вернулись до конца — пойманный фрукт едет за ладонью.
		UpdateCaughtItems();

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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				if (!CanAct)
				{
					return;
				}
				_holding = true;
				UpdateDirection(touch.Position);
			}
			else
			{
				_holding = false;
			}
		}
		else if (@event is InputEventScreenDrag drag)
		{
			if (_holding && CanAct)
			{
				UpdateDirection(drag.Position);
			}
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
	/// Максимальная длина рук в текущем направлении при условии, что обе ладони
	/// (включая разброс пальцев по HandSpreadDeg) остаются внутри видимой области —
	/// с отступом HandScreenMargin от каждого края. Возвращает >= 0.
	/// </summary>
	private float ComputeMaxArmLengthInDirection(Vector2 viewSize)
	{
		float margin = HandScreenMargin * _currentUiScale;
		Vector2 o = GlobalPosition;
		float spread = Mathf.DegToRad(HandSpreadDeg);
		float maxLen = float.PositiveInfinity;

		// Берём обе руки (верхнюю и нижнюю), чтобы ни одна не вышла за экран.
		foreach (Vector2 dir in new[] { _direction.Rotated(spread), _direction.Rotated(-spread) })
		{
			if (dir.X > 0.0001f)
			{
				maxLen = Mathf.Min(maxLen, (viewSize.X - margin - o.X) / dir.X);
			}
			else if (dir.X < -0.0001f)
			{
				maxLen = Mathf.Min(maxLen, (margin - o.X) / dir.X);
			}

			if (dir.Y > 0.0001f)
			{
				maxLen = Mathf.Min(maxLen, (viewSize.Y - margin - o.Y) / dir.Y);
			}
			else if (dir.Y < -0.0001f)
			{
				maxLen = Mathf.Min(maxLen, (margin - o.Y) / dir.Y);
			}
		}

		return Mathf.Max(0f, maxLen);
	}

	/// <summary>Мировые позиции обеих ладоней (в координатах сцены).</summary>
	public Vector2[] GetHandPositions()
	{
		if (_armLengthWorld <= 2f)
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
		if (_armLengthWorld <= 2f)
		{
			return Array.Empty<CircleHitbox>();
		}

		return new[]
		{
			new CircleHitbox(HandPositionWorld(0), HandHitRadiusWorld),
			new CircleHitbox(HandPositionWorld(1), HandHitRadiusWorld),
		};
	}

	/// <summary>Мировая позиция ладони (0 — верхняя, 1 — нижняя). Общая для ловли и для «прилипших» фруктов.</summary>
	private Vector2 HandPositionWorld(int handIndex)
	{
		float spread = Mathf.DegToRad(HandSpreadDeg);
		Vector2 dir = handIndex == 0 ? _direction.Rotated(spread) : _direction.Rotated(-spread);
		return GlobalPosition + dir * _armLengthWorld;
	}

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
		if (_armLengthWorld <= 0f)
		{
			if (_caughtItems.Count == 0)
			{
				return;
			}
			CaughtItem[] held = _caughtItems.ToArray();
			_caughtItems.Clear();
			foreach (CaughtItem entry in held)
			{
				CaughtItemReturned?.Invoke(entry.Item);
			}
			// Предмет «съеден» — включаем короткую позу eating.
			_eatingTimer = EatingStateTime;
			return;
		}

		foreach (CaughtItem entry in _caughtItems)
		{
			if (IsInstanceValid(entry.Item))
			{
				entry.Item.GlobalPosition = HandPositionWorld(entry.HandIndex);
			}
		}
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
			UpdateBodyState();
			QueueRedraw();
		}
	}

	/// <summary>Поворачивает взгляд лягушки без запуска режима ловли.</summary>
	public void SetLookTarget(Vector2 target) => UpdateDirection(target);

	public override void _Draw()
	{
		float ui = _currentUiScale;

		// --- Руки и ладони (за телом): динамически следуют за пальцем ---
		if (_armLengthWorld > 2f || (DecorativeOnly && ThrowPose))
		{
			float spread = Mathf.DegToRad(HandSpreadDeg);
			Vector2 upperDir = _direction.Rotated(spread);
			Vector2 lowerDir = _direction.Rotated(-spread);
			// Локальная длина руки в координатах ноды (одна и та же для обеих рук).
			float armLocal = DecorativeOnly ? 54f : _armLengthWorld / ui;

			foreach (Vector2 dir in new[] { upperDir, lowerDir })
			{
				Vector2 handLocal = dir * armLocal;
				DrawLine(Vector2.Zero, handLocal, SkinDark, 10f * ui);
				DrawLine(Vector2.Zero, handLocal, Skin, 5f * ui);

				DrawCircle(handLocal, 15f * ui, HandDark);
				DrawCircle(handLocal, 12f * ui, Hand);

				// Пальчики вокруг ладони.
				float angle = dir.Angle();
				for (int i = -1; i <= 1; i++)
				{
					Vector2 finger = Vector2.FromAngle(angle + i * 0.75f);
					DrawCircle(handLocal + finger * (14f * ui), 4.5f * ui, Hand);
				}
			}
		}
	}
}
