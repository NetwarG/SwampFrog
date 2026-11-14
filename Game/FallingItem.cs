using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Падающий сверху объект: фрукт (ловить), мусор (не ловить) или хилка (+1 жизнь).
/// Любой фрукт может быть золотым (IsGolden): он крупнее и даёт больше очков и опыта.
/// Все предметы отображаются спрайтами из Game/assets.
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

	/// <summary>
	/// Вид обездвиживания для отрисовки: лёд «Взгляда василиска» или камень мусора.
	/// На физику не влияет — её задаёт <see cref="Frozen"/>.
	/// </summary>
	public ItemFrost Frost
	{
		get => _frost;
		set
		{
			if (_frost == value) return;
			_frost = value;
			RefreshFrost();
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

	/// <summary>
	/// Фрукт протух от мухи: считается мусором (см. <see cref="CountsAsTrash"/>).
	/// Вид остаётся спрайтом исходного фрукта, поверх него ложится налёт порчи.
	/// </summary>
	public bool Rotten
	{
		get => _rotten;
		set
		{
			if (_rotten == value) return;
			_rotten = value;
			RefreshRotten();
		}
	}

	/// <summary>
	/// Предмет ведёт себя как мусор: касание рукой наносит урон, промах безопасен.
	/// Один ответ на вопрос «это мусор?» — и для самого мусора, и для протухшего фрукта.
	/// </summary>
	public bool CountsAsTrash => ItemType == ItemType.Trash || (ItemType == ItemType.Fruit && Rotten);

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

	/// <summary>Полуоси габарита формы в мировых единицах (учитывают масштаб и силуэт спрайта).</summary>
	public float HitRadiusX { get; set; }
	public float HitRadiusY { get; set; }

	/// <summary>
	/// Хитбокс предмета: силуэт иконки в мировых координатах. Ловля срабатывает
	/// только при пересечении силуэта с хитбоксом ладони.
	/// </summary>
	public ItemHitbox GetHitbox()
	{
		FruitShape? shape = Shape;
		if (shape == null)
		{
			return new EllipseHitbox(GlobalPosition, HitRadiusX, HitRadiusY, GlobalRotation);
		}
		return new PolygonHitbox(GlobalPosition, GlobalRotation, BaseRadius * Mathf.Max(Scale.X, 0.01f), shape.Local);
	}

	/// <summary>Форма иконки: у фруктов и мусора силуэт спрайта, у хилки эллипс.</summary>
	private FruitShape? Shape => ItemType switch
	{
		ItemType.Trash => FruitShape.Trash,
		ItemType.Healing => null,
		_ => FruitShape.Of(Kind),
	};

	/// <summary>Форма тела: силуэт иконки, а у хилки — эллипс.</summary>
	private Shape2D ItemShape()
	{
		FruitShape? shape = Shape;
		if (shape == null)
		{
			return ItemPhysics.Ellipse(HitRadiusX, HitRadiusY);
		}
		return shape.Shape(BaseRadius * Mathf.Max(Scale.X, 0.01f));
	}

	/// <summary>Насколько золотой фрукт крупнее обычного (умножение масштаба).</summary>
	private const float GoldenSizeFactor = 1.35f;

	private float _rotationSpeed;
	private Vector2 _velocity;
	private bool _isCaught;
	private bool _frozen;
	private bool _roundFrozen;
	private bool _rotten;
	private bool _despawning;
	private RigidBody2D? _physicsBody;

	/// <summary>Спрайт предмета.</summary>
	private Sprite2D? _sprite;

	/// <summary>Золотое кольцо поверх спрайта золотого фрукта.</summary>
	private GoldenGlow? _glow;

	/// <summary>Ледяная или каменная корочка обездвиженного предмета.</summary>
	private FrostCrust? _crust;
	private ItemFrost _frost = ItemFrost.None;

	/// <summary>Налёт порчи поверх спрайта протухшего фрукта.</summary>
	private RottenLook? _rot;

	private static readonly Dictionary<string, Texture2D> TextureCache = new();

	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();

		_rotationSpeed = _rng.RandfRange(-1.3f, 1.3f);

		float baseScale = Mathf.Max(Scale.X, 0.01f);
		// В классическом режиме случайный разброс делает одинаковые предметы разными;
		// в Suika картинка должна ровно совпадать со своим физическим телом.
		float scale = ManualPhysics ? baseScale : baseScale * _rng.RandfRange(0.85f, 1.08f);
		if (IsGolden)
		{
			scale *= GoldenSizeFactor;
		}
		Scale = new Vector2(scale, scale);

		// Начальная скорость: падение вниз + случайный горизонтальный дрейф,
		// чтобы предметы сами «гуляли» по экрану и сталкивались друг с другом.
		Velocity = new Vector2(_rng.RandfRange(-60f, 60f), FallSpeed);

		// Полуоси силуэта иконки (доли от BaseRadius) в мировых единицах.
		FruitShape? shape = Shape;
		Vector2 fractions = shape == null ? Vector2.One : new Vector2(shape.HitX, shape.HitY);
		HitRadiusX = BaseRadius * fractions.X * scale;
		HitRadiusY = BaseRadius * fractions.Y * scale;

		_sprite = new Sprite2D { Name = "Icon", Centered = true };
		AddChild(_sprite);
		RefreshSprite();
		// Протухший фрукт мог быть помечен до добавления в дерево: налёт включается здесь.
		if (_rotten) RefreshRotten();

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
		_physicsBody.AddChild(new CollisionShape2D { Shape = ItemShape() });
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

	/// <summary>Радиус видимой иконки в мировых единицах: спрайт нормирован по BaseRadius текущего вида.</summary>
	public float VisualRadius => BaseRadius * Mathf.Max(Scale.X, 0.01f);

	/// <summary>Базовый радиус предмета без учёта масштаба.</summary>
	private float BaseRadius => ItemType switch
	{
		ItemType.Trash => 46f,
		ItemType.Healing => 20f,
		_ => FruitCatalog.Get(Kind).BaseRadius,
	};

	/// <summary>
	/// Обновляет текстуру, масштаб и смещение спрайта под текущие ItemType/Kind.
	/// Вызывается из сеттеров и после создания спрайта, поэтому в Suika вид меняется сразу.
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
		// Центр непрозрачной области ставится в центр тела: иначе иконка съезжает с него.
		_sprite.Offset = Shape?.SpriteOffset(texture.GetHeight()) ?? Vector2.Zero;
	}

	/// <summary>Показывает или прячет корочку льда/камня поверх спрайта.</summary>
	private void RefreshFrost()
	{
		if (_frost == ItemFrost.None)
		{
			if (_crust != null) _crust.Visible = false;
			return;
		}
		if (_crust == null || !IsInstanceValid(_crust))
		{
			_crust = new FrostCrust { Name = "Frost" };
			AddChild(_crust);
		}
		_crust.Look = _frost;
		_crust.Radius = BaseRadius;
		_crust.Visible = true;
		_crust.QueueRedraw();
	}

	/// <summary>Показывает или прячет налёт порчи и приглушает краски протухшего фрукта.</summary>
	private void RefreshRotten()
	{
		if (_sprite != null)
			_sprite.Modulate = _rotten ? new Color(0.74f, 0.84f, 0.58f) : Colors.White;
		if (!_rotten)
		{
			if (_rot != null) _rot.Visible = false;
			return;
		}
		if (_rot == null || !IsInstanceValid(_rot))
		{
			_rot = new RottenLook { Name = "Rotten" };
			AddChild(_rot);
		}
		_rot.Radius = BaseRadius;
		_rot.Visible = true;
		_rot.QueueRedraw();
	}

	/// <summary>Текстура предмета из Game/assets.</summary>
	private Texture2D? LoadItemTexture()
	{
		if (ItemType == ItemType.Healing) return LoadTextureAt("res://Game/assets/fruits/healing.png");
		if (ItemType == ItemType.Trash) return LoadTextureAt("res://Game/assets/fruits/trash.png");
		return FruitCatalog.Icon(Kind);
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

	/// <summary>Налёт порчи протухшего фрукта: зелёная дымка, пятна плесени и «испарения».</summary>
	private sealed partial class RottenLook : Node2D
	{
		/// <summary>Радиус предмета в локальных единицах.</summary>
		public float Radius { get; set; }

		public override void _Draw()
		{
			DrawCircle(Vector2.Zero, Radius, new Color(FlyStyle.Mold, 0.30f));
			DrawCircle(Vector2.Zero, Radius + 1f, new Color(EffectStyle.Outline, 0.45f), false, 3f);
			// Пятна плесени: тёмные круги со смещениями от центра.
			DrawCircle(new Vector2(-Radius * 0.34f, -Radius * 0.28f), Radius * 0.26f, new Color(FlyStyle.Stain, 0.55f));
			DrawCircle(new Vector2(Radius * 0.30f, Radius * 0.10f), Radius * 0.20f, new Color(FlyStyle.Stain, 0.5f));
			DrawCircle(new Vector2(Radius * 0.04f, Radius * 0.40f), Radius * 0.16f, new Color(FlyStyle.Stain, 0.5f));
			// Два коротких «испарения» вверх: видно, что фрукт испорчен.
			DrawLine(new Vector2(-Radius * 0.22f, -Radius * 0.9f), new Vector2(-Radius * 0.30f, -Radius * 1.35f),
				new Color(FlyStyle.Mold, 0.75f), 3f);
			DrawLine(new Vector2(Radius * 0.24f, -Radius * 0.9f), new Vector2(Radius * 0.34f, -Radius * 1.35f),
				new Color(FlyStyle.Mold, 0.75f), 3f);
		}
	}

	/// <summary>Корочка обездвиженного предмета: изморозь заморозки или трещины камня.</summary>
	private sealed partial class FrostCrust : Node2D
	{
		/// <summary>Радиус предмета в локальных единицах.</summary>
		public float Radius { get; set; }

		/// <summary>Вид корочки.</summary>
		public ItemFrost Look { get; set; } = ItemFrost.Ice;

		public override void _Draw()
		{
			bool stone = Look == ItemFrost.Stone;
			Color color = stone ? EffectStyle.Stone : EffectStyle.Ice;
			DrawCircle(Vector2.Zero, Radius, new Color(color, stone ? 0.34f : 0.22f));
			DrawCircle(Vector2.Zero, Radius + 1f, new Color(EffectStyle.Outline, 0.5f), false, 3f);
			// Четыре коротких луча: лучики изморози или трещины камня.
			for (int i = 0; i < 4; i++)
			{
				Vector2 dir = Vector2.FromAngle(Mathf.Pi * 0.5f * i + Mathf.Pi * 0.25f);
				DrawLine(dir * (Radius * 0.45f), dir * (Radius * 1.05f), new Color(color, 0.85f), 3f);
			}
		}
	}
}
