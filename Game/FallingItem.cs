using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Падающий сверху объект: фрукт (ловить), мусор (не ловить) или хилка (+1 жизнь).
/// Любой фрукт может быть золотым (IsGolden): он крупнее и даёт больше очков и опыта.
/// Фрукты и мусор отображаются спрайтами из Game/assets; хилка пока рисуется
/// процедурно — у неё ещё нет ассета.
/// </summary>
public partial class FallingItem : Node2D
{
	private ItemType _itemType = ItemType.Fruit;

	public ItemType ItemType
	{
		get => _itemType;
		set
		{
			_itemType = value;
			RefreshSprite();
		}
	}

	private FruitKind _kind = FruitKind.Cherry;

	public FruitKind Kind
	{
		get => _kind;
		set
		{
			_kind = value;
			RefreshSprite();
		}
	}
	public float FallSpeed { get; set; } = 160f;

	/// <summary>Пойманный предмет больше не падает, а следует за ладонью лягушки.</summary>
	public bool IsCaught
	{
		get => _isCaught;
		set
		{
			if (_isCaught == value) return;
			_isCaught = value;
			UpdatePhysicsState();
		}
	}

	/// <summary>Золотой фрукт: модификатор любого фрукта — крупнее и даёт больше очков.</summary>
	public bool IsGolden { get; set; }

	/// <summary>
	/// Предмет обездвижен: заморозка «Взгляда василиска», остановка «Зоны замедления»
	/// или пауза выбора перка. Поймать такой предмет всё ещё можно.
	/// </summary>
	public bool Frozen
	{
		get => _frozen;
		set
		{
			if (_frozen == value) return;
			_frozen = value;
			UpdatePhysicsState();
		}
	}

	/// <summary>Текущая скорость движения в мировых единицах в секунду.</summary>
	public Vector2 Velocity
	{
		get => _physicsBody == null || _physicsBody.Freeze ? _velocity : _physicsBody.LinearVelocity;
		set
		{
			_velocity = value;
			if (_physicsBody != null && !_physicsBody.Freeze) _physicsBody.LinearVelocity = value;
		}
	}

	/// <summary>Только изображение, когда физическое тело создаёт Suika.</summary>
	public bool ManualPhysics { get; set; }

	/// <summary>Полуоси эллипса коллизии в мировых единицах (учитывают масштаб и форму спрайта).</summary>
	public float HitRadiusX { get; set; }
	public float HitRadiusY { get; set; }

	/// <summary>
	/// Хитбокс предмета: эллипс по форме тела фрукта в мировых координатах.
	/// Ловля срабатывает только при пересечении эллипса с хитбоксом ладони.
	/// </summary>
	public EllipseHitbox GetHitbox() => new(GlobalPosition, HitRadiusX, HitRadiusY, GlobalRotation);

	/// <summary>Насколько золотой фрукт крупнее обычного (умножение масштаба).</summary>
	private const float GoldenSizeFactor = 1.35f;

	private float _rotationSpeed;
	private Vector2 _velocity;
	private bool _isCaught;
	private bool _frozen;
	private bool _roundFrozen;
	private bool _despawning;
	private RigidBody2D? _physicsBody;

	/// <summary>Спрайт предмета (фрукт или мусор).</summary>
	private Sprite2D? _sprite;

	/// <summary>Золотое кольцо поверх спрайта золотого фрукта.</summary>
	private GoldenGlow? _glow;

	private static readonly Dictionary<string, Texture2D> TextureCache = new();

	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();

		_rotationSpeed = _rng.RandfRange(-1.3f, 1.3f);

		float baseScale = Mathf.Max(Scale.X, 0.01f);
		// Масштаб: базовый (ScreenScale от Main или 1 по умолчанию) умножается на случайный
		// разброс, чтобы одинаковые предметы отличались размером; золотой — крупнее.
		float scale = baseScale * _rng.RandfRange(0.85f, 1.08f);
		if (IsGolden)
		{
			scale *= GoldenSizeFactor;
		}
		Scale = new Vector2(scale, scale);

		// Начальная скорость: падение вниз + случайный горизонтальный дрейф,
		// чтобы предметы сами «гуляли» по экрану и сталкивались друг с другом.
		Velocity = new Vector2(_rng.RandfRange(-60f, 60f), FallSpeed);

		// Полуоси эллипса коллизии — форма тела из спрайта (доли от BaseRadius) в мировых единицах.
		FruitBody body = BaseBody();
		HitRadiusX = BaseRadius * body.HitX * scale;
		HitRadiusY = BaseRadius * body.HitY * scale;

		_sprite = new Sprite2D { Centered = true };
		AddChild(_sprite);
		RefreshSprite();

		// Золотой отблеск добавляется узлом поверх спрайта (ассета для золота нет).
		if (IsGolden)
		{
			_glow = new GoldenGlow { Radius = BaseRadius };
			AddChild(_glow);
		}
		if (!ManualPhysics) CreatePhysicsBody();
	}

	private void CreatePhysicsBody()
	{
		_physicsBody = new RigidBody2D
		{
			Name = "PhysicsBody",
			// Родитель хранит изображение и API предмета; его масштаб не меняет форму тела.
			TopLevel = true,
			CollisionLayer = ItemPhysics.ClassicLayer,
			CollisionMask = ItemPhysics.ClassicLayer,
			Mass = Mathf.Max(0.25f, HitRadiusX * HitRadiusY / 400f),
			PhysicsMaterialOverride = new PhysicsMaterial { Friction = 0.2f, Bounce = 0.3f },
			// Баланс задаёт скорость падения, а Rapier — движение и реакцию на столкновения.
			GravityScale = 0f,
			LinearDampMode = RigidBody2D.DampMode.Replace,
			LinearDamp = 0f,
			AngularDampMode = RigidBody2D.DampMode.Replace,
			AngularDamp = 0f,
			ContinuousCd = RigidBody2D.CcdMode.CastShape,
			CanSleep = false,
			LinearVelocity = _velocity,
			AngularVelocity = _rotationSpeed
		};
		_physicsBody.AddChild(new CollisionShape2D { Shape = ItemPhysics.Ellipse(HitRadiusX, HitRadiusY) });
		AddChild(_physicsBody);
		_physicsBody.GlobalPosition = GlobalPosition;
		_physicsBody.GlobalRotation = GlobalRotation;
		UpdatePhysicsState();
	}

	private void UpdatePhysicsState()
	{
		if (_physicsBody == null) return;
		bool detached = _isCaught || _despawning;
		_physicsBody.CollisionLayer = detached ? 0u : ItemPhysics.ClassicLayer;
		_physicsBody.CollisionMask = detached ? 0u : ItemPhysics.ClassicLayer;
		bool freeze = detached || _frozen || _roundFrozen;
		if (_physicsBody.Freeze == freeze) return;
		if (freeze)
		{
			_velocity = _physicsBody.LinearVelocity;
			_rotationSpeed = _physicsBody.AngularVelocity;
			_physicsBody.Freeze = true;
		}
		else
		{
			_physicsBody.Freeze = false;
			_physicsBody.LinearVelocity = _velocity;
			_physicsBody.AngularVelocity = _rotationSpeed;
			_physicsBody.Sleeping = false;
		}
	}

	/// <summary>Синхронизация изображения и хитбокса перед ловлей; координаты тела задаёт Rapier.</summary>
	public void SyncFromPhysics()
	{
		if (_physicsBody == null || IsCaught || _despawning) return;
		GlobalPosition = _physicsBody.GlobalPosition;
		GlobalRotation = _physicsBody.GlobalRotation;
	}

	public void FreezeForRoundEnd()
	{
		_roundFrozen = true;
		UpdatePhysicsState();
	}

	/// <summary>Сразу исключает предмет из физики, удаление ноды произойдёт в конце кадра.</summary>
	public void Despawn()
	{
		_despawning = true;
		UpdatePhysicsState();
		QueueFree();
	}

	/// <summary>При сужении окна один раз переносит тело внутрь новых боковых стен.</summary>
	public void ConstrainToViewport(Vector2 size)
	{
		if (_physicsBody == null || IsCaught || _despawning) return;
		float angle = _physicsBody.GlobalRotation;
		float halfWidth = new Vector2(HitRadiusX * Mathf.Cos(angle), HitRadiusY * Mathf.Sin(angle)).Length();
		halfWidth = Mathf.Min(halfWidth + 1f, size.X * 0.5f);
		Vector2 position = _physicsBody.GlobalPosition;
		position.X = Mathf.Clamp(position.X, halfWidth, size.X - halfWidth);
		_physicsBody.GlobalPosition = position;
		_physicsBody.ResetPhysicsInterpolation();
		SyncFromPhysics();
	}

	/// <summary>Базовый радиус предмета без учёта масштаба.</summary>
	private float BaseRadius => ItemType switch
	{
		ItemType.Trash => 46f,
		ItemType.Healing => 20f,
		_ => FruitCatalog.Get(Kind).BaseRadius,
	};

	/// <summary>Форма тела для хитбокса: у фруктов из FruitCatalog, у остальных — свои доли.</summary>
	private FruitBody BaseBody() => ItemType switch
	{
		ItemType.Trash => new FruitBody(0.677f, 0.799f),
		ItemType.Healing => new FruitBody(1f, 1f),
		_ => FruitCatalog.Get(Kind).Body,
	};

	/// <summary>
	/// Обновляет текстуру и масштаб спрайта под текущие ItemType/Kind. Вызывается
	/// из сеттеров и после создания спрайта, поэтому в Suika вид меняется сразу.
	/// </summary>
	private void RefreshSprite()
	{
		if (_sprite == null)
		{
			return;
		}
		Texture2D? texture = LoadItemTexture();
		if (texture == null)
		{
			_sprite.Visible = false;
			_sprite.Texture = null;
			return;
		}
		_sprite.Visible = true;
		_sprite.Texture = texture;
		// Высота спрайта в локальных единицах — диаметр хитбокса (2 × BaseRadius):
		// видимый размер и радиус коллизии всегда совпадают.
		_sprite.Scale = Vector2.One * (2f * BaseRadius / texture.GetHeight());
	}

	/// <summary>Текстура предмета из Game/assets или null для хилки (нет ассета).</summary>
	private Texture2D? LoadItemTexture()
	{
		string path = ItemType switch
		{
			ItemType.Trash => "res://Game/assets/fruits/trash.png",
			ItemType.Healing => string.Empty,
			_ => "res://Game/assets/fruits/" + Kind.ToString().ToLower() + ".png",
		};
		return path.Length == 0 ? null : LoadTextureAt(path);
	}

	private static Texture2D LoadTextureAt(string path)
	{
		if (TextureCache.TryGetValue(path, out Texture2D? cached) && cached != null)
		{
			return cached;
		}
		Texture2D loaded = GD.Load<Texture2D>(path);
		TextureCache[path] = loaded;
		return loaded;
	}

	public override void _Process(double delta)
	{
		SyncFromPhysics();
	}

	public override void _Draw()
	{
		// Хилка пока процедурная — для неё нет ассета. Остальное рисуют спрайты.
		if (ItemType == ItemType.Healing)
		{
			DrawHealing();
		}
	}

	/// <summary>Золотое кольцо поверх спрайта золотого фрукта.</summary>
	private sealed partial class GoldenGlow : Node2D
	{
		/// <summary>Радиус кольца в локальных единицах предмета.</summary>
		public float Radius { get; set; }

		public override void _Draw()
		{
			DrawCircle(Vector2.Zero, Radius + 2f, new Color("ffd700"), false, 4f);
			DrawCircle(Vector2.Zero, Radius, new Color(1f, 0.84f, 0.2f, 0.22f));
			DrawCircle(new Vector2(-Radius * 0.30f, -Radius * 0.32f), Radius * 0.22f, new Color(1f, 1f, 1f, 0.5f));
		}
	}

	// --- Хилка: сердечко с медицинским крестом (+1 жизнь) ---
	private void DrawHealing()
	{
		Color heart = new("ff6b81");

		// Сердечко: два верхних круга + треугольник снизу.
		DrawCircle(new Vector2(-6f, -4f), 7f, heart);
		DrawCircle(new Vector2(6f, -4f), 7f, heart);
		Vector2[] body =
		{
			new(-12f, -2f),
			new(12f, -2f),
			new(0f, 12f),
		};
		DrawColoredPolygon(body, heart);

		// Белая кайма.
		DrawCircle(new Vector2(-6f, -4f), 8f, new Color(1f, 1f, 1f, 0.85f), false, 2f);
		DrawCircle(new Vector2(6f, -4f), 8f, new Color(1f, 1f, 1f, 0.85f), false, 2f);
		DrawLine(new Vector2(-12f, -2f), new Vector2(0f, 14f), new Color(1f, 1f, 1f, 0.85f), 2f);
		DrawLine(new Vector2(12f, -2f), new Vector2(0f, 14f), new Color(1f, 1f, 1f, 0.85f), 2f);

		// Медицинский крест.
		DrawRect(new Rect2(-2.5f, -3f, 5f, 9f), new Color("ffffff"));
		DrawRect(new Rect2(-4.5f, -1f, 9f, 5f), new Color("ffffff"));

		// Блик.
		DrawCircle(new Vector2(-8f, -8f), 2.5f, new Color(1f, 1f, 1f, 0.55f));
	}
}
