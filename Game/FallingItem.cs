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
	public bool IsCaught { get; set; }

	/// <summary>Золотой фрукт: модификатор любого фрукта — крупнее и даёт больше очков.</summary>
	public bool IsGolden { get; set; }

	/// <summary>
	/// Предмет обездвижен: заморозка «Взгляда василиска», остановка «Зоны замедления»
	/// или пауза выбора перка. Поймать такой предмет всё ещё можно.
	/// </summary>
	public bool Frozen { get; set; }

	/// <summary>Текущая скорость движения в мировых единицах в секунду.</summary>
	public Vector2 Velocity { get; set; }

	/// <summary>Отключает встроенное падение, когда объект управляется физикой Suika.</summary>
	public bool ManualPhysics { get; set; }

	/// <summary>Радиус коллизии (учитывает масштаб): используется для отталкивания от стен и других предметов.</summary>
	public float Radius { get; set; }

	/// <summary>
	/// Хитбокс предмета: круг в мировых координатах с радиусом фактического рисуемого размера
	/// (Radius = BaseRadius × масштаб). Ловля срабатывает только при пересечении с хитбоксом ладони.
	/// </summary>
	public CircleHitbox GetHitbox() => new(GlobalPosition, Radius);

	private const float WallRestitution = 0.85f;

	/// <summary>Насколько золотой фрукт крупнее обычного (умножение масштаба).</summary>
	private const float GoldenSizeFactor = 1.35f;

	private float _rotationSpeed;

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

		float scale = _rng.RandfRange(0.85f, 1.08f);
		// Золотой фрукт крупнее обычного: масштаб и хитбокс растут вместе.
		if (IsGolden)
		{
			scale *= GoldenSizeFactor;
		}
		Scale = new Vector2(scale, scale);

		// Начальная скорость: падение вниз + случайный горизонтальный дрейф,
		// чтобы предметы сами «гуляли» по экрану и сталкивались друг с другом.
		Velocity = new Vector2(_rng.RandfRange(-60f, 60f), FallSpeed);

		// Радиус коллизии совпадает с видимым размером спрайта.
		Radius = BaseRadius * scale;

		_sprite = new Sprite2D { Centered = true };
		AddChild(_sprite);
		RefreshSprite();

		// Золотой отблеск добавляется узлом поверх спрайта (ассета для золота нет).
		if (IsGolden)
		{
			_glow = new GoldenGlow { Radius = BaseRadius };
			AddChild(_glow);
		}
	}

	/// <summary>Базовый радиус предмета без учёта масштаба.</summary>
	private float BaseRadius => ItemType switch
	{
		ItemType.Trash => 23f,
		ItemType.Healing => 20f,
		_ => FruitCatalog.Get(Kind).BaseRadius,
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
		// Предмет заморожен (эффект перка или пауза) — не двигается.
		if (Frozen)
		{
			return;
		}
		if (ManualPhysics)
		{
			return;
		}
		// Пойманный предмет управляется лягушкой: не падает и не вращается.
		if (IsCaught)
		{
			return;
		}

		float dt = (float)delta;

		Position += Velocity * dt;

		// Отталкивание от боковых стен экрана: ладонь полностью в границах, скорость разворачиваем.
		float viewWidth = GetViewportRect().Size.X;
		if (Position.X < Radius)
		{
			Position = new Vector2(Radius, Position.Y);
			Velocity = new Vector2(Mathf.Abs(Velocity.X) * WallRestitution, Velocity.Y);
		}
		else if (Position.X > viewWidth - Radius)
		{
			Position = new Vector2(viewWidth - Radius, Position.Y);
			Velocity = new Vector2(-Mathf.Abs(Velocity.X) * WallRestitution, Velocity.Y);
		}

		Rotation += _rotationSpeed * dt;
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
