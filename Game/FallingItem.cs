using Godot;

namespace SwampFrog;

/// <summary>
/// Падающий сверху объект: фрукт (ловить), золотой фрукт (ловить, +30) или мусор (не ловить).
/// Вся графика рисуется процедурно в _Draw, поэтому ассеты не нужны.
/// </summary>
public partial class FallingItem : Node2D
{
	public ItemType ItemType { get; set; } = ItemType.Fruit;
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
	private Color _fruitColor;

	private static readonly Color Leaf = new("57a74a");
	private static readonly Color Stem = new("5a4a2a");
	private static readonly Color TrashBag = new("89a29d");
	private static readonly Color TrashDark = new("4d635e");

	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();

		_fruitColor = _rng.Randf() < 0.5f ? new Color("e84a35") : new Color("f09a2a");

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
		_ => 22f,
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
			default:
				DrawFruit();
				break;
		}
	}

	private void DrawFruit()
	{
		DrawCircle(Vector2.Zero, 22f, _fruitColor);
		DrawCircle(new Vector2(-7f, -8f), 6f, new Color(1f, 1f, 1f, 0.35f));
		DrawLine(new Vector2(0f, -16f), new Vector2(3f, -28f), Stem, 3f);
		Vector2[] leaf =
		{
			new(3f, -27f),
			new(17f, -21f),
			new(4f, -18f),
		};
		DrawColoredPolygon(leaf, Leaf);
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
}