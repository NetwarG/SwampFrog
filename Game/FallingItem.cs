using Godot;

namespace SwampFrog;

/// <summary>
/// Падающий сверху объект: фрукт (ловить), золотой фрукт (ловить, +30) или мусор (не ловить).
/// Вся графика рисуется процедурно в _Draw, поэтому ассеты не нужны.
/// </summary>
public partial class FallingItem : Node2D
{
	public ItemType ItemType { get; set; } = ItemType.Fruit;
	public FruitKind Kind { get; set; } = FruitKind.Cherry;
	public float FallSpeed { get; set; } = 160f;
	public float CatchRadius { get; set; } = 40f;

	/// <summary>Пойманный предмет больше не падает, а следует за ладонью лягушки.</summary>
	public bool IsCaught { get; set; }

	/// <summary>Текущая скорость движения в мировых единицах в секунду.</summary>
	public Vector2 Velocity { get; set; }

	/// <summary>Радиус коллизии (учитывает масштаб): используется для отталкивания от стен и других предметов.</summary>
	public float Radius { get; set; }

	private const float WallRestitution = 0.85f;

	private float _rotationSpeed;

	private static readonly Color Leaf = new("57a74a");
	private static readonly Color Stem = new("5a4a2a");
	private static readonly Color TrashBag = new("89a29d");
	private static readonly Color TrashDark = new("4d635e");

	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();

		_rotationSpeed = _rng.RandfRange(-1.3f, 1.3f);

		float scale = _rng.RandfRange(0.85f, 1.08f);
		Scale = new Vector2(scale, scale);

		// Начальная скорость: падение вниз + случайный горизонтальный дрейф,
		// чтобы предметы сами «гуляли» по экрану и сталкивались друг с другом.
		Velocity = new Vector2(_rng.RandfRange(-60f, 60f), FallSpeed);

		// Радиус коллизии совпадает с прорисовкой предмета.
		Radius = BaseRadius * scale;
	}

	/// <summary>Базовый радиус предмета без учёта масштаба.</summary>
	private float BaseRadius => ItemType switch
	{
		ItemType.GoldenFruit => 25f,
		ItemType.Trash => 23f,
		ItemType.Healing => 20f,
		_ => FruitCatalog.Get(Kind).BaseRadius,
	};

	public override void _Process(double delta)
	{
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
		QueueRedraw();
	}

	public override void _Draw()
	{
		switch (ItemType)
		{
			case ItemType.GoldenFruit:
				DrawGoldenFruit();
				break;
			case ItemType.Trash:
				DrawTrash();
				break;
			case ItemType.Healing:
				DrawHealing();
				break;
			default:
				DrawFruitByKind(Kind);
				break;
		}
	}

	/// <summary>Рисует конкретный вид фрукта. Новый фрукт = новый case здесь.</summary>
	private void DrawFruitByKind(FruitKind kind)
	{
		switch (kind)
		{
			case FruitKind.Cherry:
				DrawCherry();
				break;
			case FruitKind.Strawberry:
				DrawStrawberry();
				break;
			case FruitKind.Grape:
				DrawGrape();
				break;
			case FruitKind.Mandarin:
				DrawMandarin();
				break;
			case FruitKind.Apple:
				DrawApple();
				break;
			case FruitKind.Pear:
				DrawPear();
				break;
			case FruitKind.Peach:
				DrawPeach();
				break;
			case FruitKind.Pineapple:
				DrawPineapple();
				break;
			case FruitKind.Melon:
				DrawMelon();
				break;
			case FruitKind.Watermelon:
				DrawWatermelon();
				break;
		}
	}

	// --- Вишня: две красные ягоды ---
	private void DrawCherry()
	{
		DrawCircle(new Vector2(-5f, 0f), 8f, new Color("c1222f"));
		DrawCircle(new Vector2(5f, -2f), 8f, new Color("c1222f"));
		DrawCircle(new Vector2(-6f, -2f), 3f, new Color(1f, 1f, 1f, 0.4f));
		DrawCircle(new Vector2(4f, -4f), 3f, new Color(1f, 1f, 1f, 0.4f));
		DrawLine(new Vector2(-2f, -4f), new Vector2(-1f, -16f), Stem, 3f);
		DrawLine(new Vector2(6f, -6f), new Vector2(2f, -18f), Stem, 3f);
	}

	// --- Клубника: красная ягода с зелёной шапочкой и семечками ---
	private void DrawStrawberry()
	{
		// Ягода — сердцевидная форма.
		DrawCircle(new Vector2(0f, -2f), 13f, new Color("e03a3a"));
		DrawCircle(new Vector2(0f, 4f), 8f, new Color("e03a3a"));

		// Семечки.
		Vector2[] seeds =
		{
			new Vector2(-4f, -6f), new Vector2(5f, -4f), new Vector2(-2f, 1f),
			new Vector2(3f, 4f), new Vector2(-6f, 5f),
		};
		foreach (Vector2 p in seeds)
		{
			DrawCircle(p, 1.6f, new Color("f8e24a"));
		}

		// Шапочка из листиков.
		Vector2[] cap =
		{
			new(-10f, -10f),
			new(-4f, -15f),
			new(4f, -15f),
			new(10f, -10f),
			new(0f, -8f),
		};
		DrawColoredPolygon(cap, Leaf);
		DrawCircle(new Vector2(0f, -13f), 2.5f, Stem);
	}

	// --- Виноград: гроздь фиолетовых ягод ---
	private void DrawGrape()
	{
		Color grape = new("6d4ac9");
		Vector2[] berries =
		{
			new(0f, 4f), new(-6f, 3f), new(6f, 3f),
			new(-3f, -3f), new(3f, -3f), new(0f, -8f),
		};
		foreach (Vector2 p in berries)
		{
			DrawCircle(p, 5.5f, grape);
		}
		DrawCircle(new Vector2(-2f, -4f), 2f, new Color(1f, 1f, 1f, 0.35f));
		DrawLine(new Vector2(0f, -10f), new Vector2(1f, -20f), Stem, 3f);
		DrawCircle(new Vector2(1f, -20f), 3f, Leaf);
	}

	// --- Мандарин: оранжевый с листиком ---
	private void DrawMandarin()
	{
		DrawCircle(Vector2.Zero, 15f, new Color("f08a2a"));
		DrawCircle(new Vector2(-5f, -6f), 4f, new Color(1f, 1f, 1f, 0.35f));
		Vector2[] leaf =
		{
			new(2f, -14f),
			new(14f, -8f),
			new(4f, -10f),
		};
		DrawColoredPolygon(leaf, Leaf);
		DrawLine(new Vector2(0f, -14f), new Vector2(2f, -20f), Stem, 3f);
	}

	// --- Яблоко: красное с черенком и листом ---
	private void DrawApple()
	{
		DrawCircle(Vector2.Zero, 16f, new Color("d63a2a"));
		DrawCircle(new Vector2(-5f, -7f), 5f, new Color(1f, 1f, 1f, 0.4f));
		Vector2[] leaf =
		{
			new(2f, -15f),
			new(16f, -9f),
			new(4f, -11f),
		};
		DrawColoredPolygon(leaf, Leaf);
		DrawLine(new Vector2(0f, -16f), new Vector2(0f, -23f), Stem, 3f);
	}

	// --- Груша: жёлто-зелёная с узким верхом ---
	private void DrawPear()
	{
		Color pear = new("9ac44a");
		DrawCircle(new Vector2(0f, 6f), 9f, pear);
		DrawCircle(new Vector2(0f, -4f), 14f, pear);
		DrawCircle(new Vector2(-5f, -8f), 4f, new Color(1f, 1f, 1f, 0.35f));
		DrawLine(new Vector2(0f, -2f), new Vector2(0f, -13f), Stem, 3f);
		Vector2[] leaf =
		{
			new(0f, -12f),
			new(10f, -5f),
			new(2f, -8f),
		};
		DrawColoredPolygon(leaf, Leaf);
	}

	// --- Персик: оранжево-розовый с бороздкой (для будущего режима) ---
	private void DrawPeach()
	{
		DrawCircle(Vector2.Zero, 16f, new Color("f2a05a"));
		DrawCircle(new Vector2(0f, -2f), 2f, new Color(1f, 1f, 1f, 0.25f));
		DrawLine(new Vector2(-4f, 10f), new Vector2(4f, -10f), new Color("d97a3a"), 1.5f);
		Vector2[] leaf =
		{
			new(2f, -15f),
			new(14f, -9f),
			new(4f, -11f),
		};
		DrawColoredPolygon(leaf, Leaf);
		DrawLine(new Vector2(0f, -16f), new Vector2(0f, -22f), Stem, 3f);
	}

	// --- Ананас: жёлтый бочонок с кроной (для будущего режима) ---
	private void DrawPineapple()
	{
		// Тело.
		DrawRect(new Rect2(-11f, -8f, 22f, 26f), new Color("e9c940"));
		DrawRect(new Rect2(-11f, -8f, 22f, 26f), new Color("b58f2a"), false, 2f);
		// Ромбики-чешуйки.
		for (float y = -6f; y <= 12f; y += 6f)
		{
			for (float x = -7f; x <= 7f; x += 6f)
			{
				DrawLine(new Vector2(x - 3f, y + 2f), new Vector2(x + 3f, y - 2f), new Color("b58f2a"), 1.5f);
				DrawLine(new Vector2(x - 3f, y - 2f), new Vector2(x + 3f, y + 2f), new Color("b58f2a"), 1.5f);
			}
		}
		// Крона.
		DrawCircle(new Vector2(0f, -14f), 4f, Stem);
		Vector2[] crown =
		{
			new Vector2(-5f, -22f), new Vector2(0f, -25f), new Vector2(5f, -22f),
		};
		foreach (Vector2 p in crown)
		{
			DrawCircle(p, 4f, Leaf);
		}
	}

	// --- Дыня: светлая с тёмными полосами (для будущего режима) ---
	private void DrawMelon()
	{
		DrawCircle(Vector2.Zero, 21f, new Color("f2e3a0"));
		DrawCircle(new Vector2(-5f, -7f), 4f, new Color(1f, 1f, 1f, 0.3f));
		// Сеточка полос.
		for (float a = -0.6f; a <= 0.6f; a += 0.3f)
		{
			Vector2 d = Vector2.FromAngle(a);
			DrawLine(-d * 20f, d * 22f, new Color("a8924a"), 2f);
		}
		DrawLine(new Vector2(0f, -19f), new Vector2(0f, -26f), Stem, 3f);
	}

	// --- Арбуз: тёмно-зелёный с полосами (для будущего режима) ---
	private void DrawWatermelon()
	{
		DrawCircle(Vector2.Zero, 26f, new Color("2e6b2e"));
		// Изогнутые светлые полосы.
		foreach (float x in new float[] { -16f, 0f, 16f })
		{
			DrawArc(new Vector2(x, 0f), 5f, 0f, Mathf.Tau, 12, new Color("8ab85a"), 5f);
		}
		DrawLine(new Vector2(0f, -24f), new Vector2(0f, -30f), Stem, 3f);
	}

	private void DrawGoldenFruit()
	{
		DrawCircle(Vector2.Zero, 24f, new Color("ffd32e"));
		DrawCircle(new Vector2(-8f, -9f), 7f, new Color(1f, 1f, 1f, 0.45f));
		DrawLine(new Vector2(0f, -18f), new Vector2(2f, -30f), Stem, 3f);
		Vector2[] leaf =
		{
			new(3f, -29f),
			new(18f, -23f),
			new(4f, -20f),
		};
		DrawColoredPolygon(leaf, Leaf);
	}

	private void DrawTrash()
	{
		// Полиэтиленовый пакет: прямоугольник с ручками.
		DrawRect(new Rect2(-22f, -12f, 44f, 34f), TrashBag, true);
		DrawRect(new Rect2(-22f, -12f, 44f, 34f), TrashDark, false, 3f);

		DrawArc(new Vector2(-8f, -12f), 6f, Mathf.Pi, Mathf.Pi * 2f, 16, TrashDark, 3f);
		DrawArc(new Vector2(8f, -12f), 6f, Mathf.Pi, Mathf.Pi * 2f, 16, TrashDark, 3f);

		DrawLine(new Vector2(-13f, 2f), new Vector2(1f, -2f), TrashDark, 2f);
		DrawLine(new Vector2(1f, -2f), new Vector2(13f, 4f), TrashDark, 2f);
		DrawLine(new Vector2(-8f, 10f), new Vector2(9f, 12f), TrashDark, 2f);
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