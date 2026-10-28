using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SwampFrog;

/// <summary>
/// Форма иконки предмета: силуэт непрозрачной области спрайта. Точки нормированы на
/// BaseRadius и заданы относительно центра холста спрайта, поэтому тело и хитбокс
/// повторяют видимые очертания фрукта, а не прямоугольник картинки с прозрачными полями.
/// Данные снимает Tools/ShapeDump.tscn: godot --headless --path . res://Tools/ShapeDump.tscn
/// </summary>
public sealed record FruitShape(Vector2 Center, Vector2[] Outline)
{
	private Vector2[]? _local;

	/// <summary>Силуэт в локальных координатах тела: центр — центр непрозрачной области.</summary>
	public Vector2[] Local => _local ??= Outline.Select(point => point - Center).ToArray();

	/// <summary>Горизонтальная полуось силуэта в долях от BaseRadius.</summary>
	public float HitX => Local.Max(point => Mathf.Abs(point.X));

	/// <summary>Вертикальная полуось силуэта в долях от BaseRadius.</summary>
	public float HitY => Local.Max(point => Mathf.Abs(point.Y));

	/// <summary>Выпуклая форма тела: силуэт в мировых единицах.</summary>
	public ConvexPolygonShape2D Shape(float radius)
	{
		return new ConvexPolygonShape2D { Points = Local.Select(point => point * radius).ToArray() };
	}

	/// <summary>Смещение спрайта в пикселях текстуры: центр иконки встаёт в центр тела.</summary>
	public Vector2 SpriteOffset(float textureHeight) => -Center * textureHeight * 0.5f;

	/// <summary>Проекция силуэта на ось: размещение тел и линия переполнения.</summary>
	public float ExtentAlong(Vector2 axis, float rotation, float radius)
	{
		Vector2 local = axis.Rotated(-rotation);
		float extent = 0f;
		foreach (Vector2 point in Local)
			extent = Mathf.Max(extent, Mathf.Abs(point.Dot(local)));
		return extent * radius;
	}

	/// <summary>Формы всех фруктов; единица измерения — BaseRadius, центр холста — в нуле.</summary>
	private static readonly Dictionary<FruitKind, FruitShape> Shapes = new()
	{
		[FruitKind.Cherry] = new FruitShape(new Vector2(0.020f, 0.140f), new[]
		{
			new Vector2(-0.650f, 0.353f),
			new Vector2(-0.621f, 0.272f),
			new Vector2(-0.348f, -0.246f),
			new Vector2(-0.247f, -0.369f),
			new Vector2(0.059f, -0.561f),
			new Vector2(0.094f, -0.561f),
			new Vector2(0.143f, -0.528f),
			new Vector2(0.667f, 0.301f),
			new Vector2(0.693f, 0.392f),
			new Vector2(0.696f, 0.493f),
			new Vector2(0.670f, 0.603f),
			new Vector2(0.618f, 0.694f),
			new Vector2(0.501f, 0.795f),
			new Vector2(0.354f, 0.837f),
			new Vector2(-0.306f, 0.837f),
			new Vector2(-0.433f, 0.808f),
			new Vector2(-0.524f, 0.753f),
			new Vector2(-0.611f, 0.649f),
			new Vector2(-0.660f, 0.483f),
		}),
		[FruitKind.Strawberry] = new FruitShape(new Vector2(0.005f, 0.133f), new[]
		{
			new Vector2(-0.584f, 0.119f),
			new Vector2(-0.554f, -0.008f),
			new Vector2(-0.395f, -0.330f),
			new Vector2(0.028f, -0.613f),
			new Vector2(0.109f, -0.610f),
			new Vector2(0.395f, -0.379f),
			new Vector2(0.558f, -0.050f),
			new Vector2(0.590f, 0.076f),
			new Vector2(0.584f, 0.233f),
			new Vector2(0.528f, 0.402f),
			new Vector2(0.428f, 0.577f),
			new Vector2(0.337f, 0.694f),
			new Vector2(0.220f, 0.808f),
			new Vector2(0.102f, 0.873f),
			new Vector2(-0.002f, 0.883f),
			new Vector2(-0.135f, 0.831f),
			new Vector2(-0.236f, 0.753f),
			new Vector2(-0.444f, 0.538f),
			new Vector2(-0.551f, 0.356f),
			new Vector2(-0.580f, 0.239f),
		}),
		[FruitKind.Grape] = new FruitShape(new Vector2(-0.005f, 0.176f), new[]
		{
			new Vector2(-0.631f, 0.005f),
			new Vector2(-0.589f, -0.119f),
			new Vector2(-0.153f, -0.610f),
			new Vector2(-0.075f, -0.646f),
			new Vector2(0.524f, -0.633f),
			new Vector2(0.550f, -0.593f),
			new Vector2(0.618f, 0.073f),
			new Vector2(0.481f, 0.577f),
			new Vector2(0.442f, 0.662f),
			new Vector2(0.124f, 0.951f),
			new Vector2(0.023f, 0.997f),
			new Vector2(-0.065f, 0.997f),
			new Vector2(-0.176f, 0.945f),
			new Vector2(-0.436f, 0.597f),
			new Vector2(-0.624f, 0.119f),
		}),
		[FruitKind.Mandarin] = new FruitShape(new Vector2(0.031f, 0.233f), new[]
		{
			new Vector2(-0.610f, 0.346f),
			new Vector2(-0.593f, 0.239f),
			new Vector2(-0.522f, 0.063f),
			new Vector2(-0.418f, -0.057f),
			new Vector2(-0.047f, -0.343f),
			new Vector2(0.304f, -0.454f),
			new Vector2(0.512f, -0.470f),
			new Vector2(0.649f, -0.434f),
			new Vector2(0.662f, -0.395f),
			new Vector2(0.659f, 0.483f),
			new Vector2(0.600f, 0.646f),
			new Vector2(0.551f, 0.717f),
			new Vector2(0.444f, 0.818f),
			new Vector2(0.242f, 0.909f),
			new Vector2(0.096f, 0.932f),
			new Vector2(-0.047f, 0.932f),
			new Vector2(-0.259f, 0.889f),
			new Vector2(-0.405f, 0.811f),
			new Vector2(-0.476f, 0.746f),
			new Vector2(-0.545f, 0.652f),
			new Vector2(-0.600f, 0.489f),
		}),
		[FruitKind.Apple] = new FruitShape(new Vector2(-0.005f, 0.083f), new[]
		{
			new Vector2(-0.681f, 0.127f),
			new Vector2(-0.638f, -0.068f),
			new Vector2(-0.564f, -0.192f),
			new Vector2(-0.137f, -0.550f),
			new Vector2(0.322f, -0.674f),
			new Vector2(0.463f, -0.664f),
			new Vector2(0.482f, -0.638f),
			new Vector2(0.658f, 0.026f),
			new Vector2(0.664f, 0.221f),
			new Vector2(0.619f, 0.410f),
			new Vector2(0.511f, 0.609f),
			new Vector2(0.410f, 0.723f),
			new Vector2(0.329f, 0.785f),
			new Vector2(0.189f, 0.834f),
			new Vector2(-0.218f, 0.837f),
			new Vector2(-0.313f, 0.808f),
			new Vector2(-0.440f, 0.720f),
			new Vector2(-0.560f, 0.573f),
			new Vector2(-0.616f, 0.469f),
			new Vector2(-0.674f, 0.270f),
		}),
		[FruitKind.Pear] = new FruitShape(new Vector2(-0.005f, 0.091f), new[]
		{
			new Vector2(-0.546f, 0.277f),
			new Vector2(-0.207f, -0.648f),
			new Vector2(-0.178f, -0.681f),
			new Vector2(-0.122f, -0.707f),
			new Vector2(0.324f, -0.700f),
			new Vector2(0.484f, -0.635f),
			new Vector2(0.536f, -0.596f),
			new Vector2(0.549f, 0.443f),
			new Vector2(0.529f, 0.560f),
			new Vector2(0.480f, 0.664f),
			new Vector2(0.435f, 0.723f),
			new Vector2(0.308f, 0.824f),
			new Vector2(0.217f, 0.863f),
			new Vector2(0.093f, 0.886f),
			new Vector2(-0.116f, 0.886f),
			new Vector2(-0.233f, 0.863f),
			new Vector2(-0.331f, 0.821f),
			new Vector2(-0.454f, 0.720f),
			new Vector2(-0.516f, 0.629f),
			new Vector2(-0.562f, 0.479f),
		}),
		[FruitKind.Peach] = new FruitShape(new Vector2(0.002f, 0.148f), new[]
		{
			new Vector2(-0.642f, 0.065f),
			new Vector2(-0.593f, -0.062f),
			new Vector2(-0.537f, -0.147f),
			new Vector2(-0.150f, -0.502f),
			new Vector2(-0.107f, -0.524f),
			new Vector2(0.261f, -0.590f),
			new Vector2(0.345f, -0.590f),
			new Vector2(0.453f, -0.560f),
			new Vector2(0.544f, -0.508f),
			new Vector2(0.609f, -0.440f),
			new Vector2(0.661f, 0.277f),
			new Vector2(0.642f, 0.391f),
			new Vector2(0.590f, 0.524f),
			new Vector2(0.528f, 0.619f),
			new Vector2(0.440f, 0.710f),
			new Vector2(0.283f, 0.805f),
			new Vector2(-0.016f, 0.883f),
			new Vector2(-0.290f, 0.818f),
			new Vector2(-0.427f, 0.736f),
			new Vector2(-0.570f, 0.573f),
			new Vector2(-0.619f, 0.472f),
			new Vector2(-0.661f, 0.251f),
		}),
		[FruitKind.Pineapple] = new FruitShape(new Vector2(0.116f, 0.005f), new[]
		{
			new Vector2(-0.399f, -0.564f),
			new Vector2(-0.223f, -0.801f),
			new Vector2(0.122f, -0.945f),
			new Vector2(0.497f, -0.814f),
			new Vector2(0.656f, -0.577f),
			new Vector2(0.650f, 0.440f),
			new Vector2(0.637f, 0.554f),
			new Vector2(0.594f, 0.687f),
			new Vector2(0.546f, 0.769f),
			new Vector2(0.484f, 0.834f),
			new Vector2(0.334f, 0.919f),
			new Vector2(0.171f, 0.951f),
			new Vector2(-0.077f, 0.935f),
			new Vector2(-0.194f, 0.889f),
			new Vector2(-0.321f, 0.785f),
			new Vector2(-0.396f, 0.651f),
			new Vector2(-0.425f, 0.521f),
		}),
		[FruitKind.Melon] = new FruitShape(new Vector2(-0.107f, -0.042f), new[]
		{
			new Vector2(-0.800f, -0.125f),
			new Vector2(-0.741f, -0.304f),
			new Vector2(-0.673f, -0.418f),
			new Vector2(-0.572f, -0.528f),
			new Vector2(-0.098f, -0.821f),
			new Vector2(-0.036f, -0.811f),
			new Vector2(0.335f, -0.535f),
			new Vector2(0.446f, -0.415f),
			new Vector2(0.530f, -0.255f),
			new Vector2(0.582f, 0.002f),
			new Vector2(0.576f, 0.128f),
			new Vector2(0.520f, 0.330f),
			new Vector2(0.390f, 0.525f),
			new Vector2(0.234f, 0.646f),
			new Vector2(0.072f, 0.711f),
			new Vector2(-0.068f, 0.733f),
			new Vector2(-0.306f, 0.714f),
			new Vector2(-0.449f, 0.662f),
			new Vector2(-0.537f, 0.610f),
			new Vector2(-0.660f, 0.496f),
			new Vector2(-0.732f, 0.392f),
			new Vector2(-0.800f, 0.203f),
		}),
		[FruitKind.Watermelon] = new FruitShape(new Vector2(-0.011f, 0.008f), new[]
		{
			new Vector2(-0.717f, -0.018f),
			new Vector2(-0.675f, -0.184f),
			new Vector2(-0.623f, -0.294f),
			new Vector2(-0.532f, -0.418f),
			new Vector2(-0.444f, -0.499f),
			new Vector2(0.050f, -0.792f),
			new Vector2(0.119f, -0.798f),
			new Vector2(0.151f, -0.785f),
			new Vector2(0.551f, -0.366f),
			new Vector2(0.649f, -0.184f),
			new Vector2(0.691f, -0.021f),
			new Vector2(0.701f, 0.151f),
			new Vector2(0.655f, 0.366f),
			new Vector2(0.600f, 0.480f),
			new Vector2(0.496f, 0.610f),
			new Vector2(0.350f, 0.717f),
			new Vector2(0.128f, 0.798f),
			new Vector2(-0.060f, 0.811f),
			new Vector2(-0.223f, 0.785f),
			new Vector2(-0.363f, 0.730f),
			new Vector2(-0.493f, 0.639f),
			new Vector2(-0.629f, 0.476f),
			new Vector2(-0.711f, 0.265f),
			new Vector2(-0.727f, 0.141f),
		}),
	};

	/// <summary>Форма мусора: он не входит в FruitKind, но участвует в физике.</summary>
	public static readonly FruitShape Trash = new(new Vector2(-0.003f, 0.013f), new[]
	{
		new Vector2(-0.676f, -0.441f),
		new Vector2(-0.550f, -0.603f),
		new Vector2(-0.488f, -0.659f),
		new Vector2(-0.094f, -0.782f),
		new Vector2(-0.046f, -0.782f),
		new Vector2(0.507f, -0.499f),
		new Vector2(0.647f, -0.327f),
		new Vector2(0.670f, -0.262f),
		new Vector2(0.660f, -0.141f),
		new Vector2(0.501f, 0.587f),
		new Vector2(0.472f, 0.652f),
		new Vector2(0.358f, 0.740f),
		new Vector2(0.202f, 0.789f),
		new Vector2(-0.078f, 0.808f),
		new Vector2(-0.254f, 0.785f),
		new Vector2(-0.403f, 0.737f),
		new Vector2(-0.501f, 0.662f),
		new Vector2(-0.530f, 0.610f),
		new Vector2(-0.680f, -0.203f),
	});

	/// <summary>Форма иконки фрукта.</summary>
	public static FruitShape Of(FruitKind kind) => Shapes[kind];
}
