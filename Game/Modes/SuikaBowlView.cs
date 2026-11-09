using Godot;

namespace SwampFrog;

/// <summary>
/// Кувшин мини-игры: стекло в стиле иконок Game/assets — толстый коричневый контур,
/// диагональный градиент и глянцевые отблески. Полость стекла совпадает со стенками и
/// дном физического кувшина, поэтому фрукты лежат ровно на дне и у стенок.
/// </summary>
internal static class SuikaBowlView
{
	/// <summary>Толщина стекла по бокам и у дна в логических единицах.</summary>
	private const float GlassSide = 12f;
	private const float GlassFloor = 14f;

	/// <summary>Скругление внешних углов стекла и высота видимой кромки.</summary>
	private const float TopCorner = 10f;
	private const float BottomCorner = 24f;
	private const float LipHeight = 12f;

	/// <summary>Толщина коричневого контура, как у кнопок и иконок игры.</summary>
	private const float OutlineWidth = 5f;

	private const int EllipseSegments = 28;

	/// <summary>Стекло: светлый верхний левый угол градиента и тёмный нижний правый.</summary>
	private static readonly Color GlassLight = new(0.66f, 0.92f, 0.84f, 0.44f);
	private static readonly Color GlassDark = new(0.20f, 0.50f, 0.43f, 0.52f);

	/// <summary>Вода внутри стекла: светлее у поверхности и темнее к дну, поэтому полость не плоская.</summary>
	private static readonly Color CavityLight = new(0.05f, 0.20f, 0.18f, 0.44f);
	private static readonly Color CavityDeep = new(0.01f, 0.09f, 0.09f, 0.56f);

	/// <summary>Светлые грани стекла, отблеск канта и тень под кувшином.</summary>
	private static readonly Color InnerEdge = new(0.78f, 0.98f, 0.92f, 0.18f);
	private static readonly Color Sheen = new(0.86f, 1f, 0.97f, 0.06f);
	private static readonly Color RimLight = new(0.88f, 1f, 0.97f, 0.34f);
	private static readonly Color Shadow = new(0.01f, 0.05f, 0.04f, 0.34f);

	/// <summary>Линия переполнения: тёплый сигнальный тон, приглушённый, чтобы не спорить с фруктами.</summary>
	private static readonly Color Danger = new(1f, 0.56f, 0.38f, 0.46f);

	/// <summary>Рисует кувшин: тень, стекло, полость, кромку и линию переполнения.</summary>
	public static void Draw(CanvasItem target, Rect2 bowl, float floorInset, float rimOffset)
	{
		if (bowl.Size.X <= 0f || bowl.Size.Y <= 0f) return;

		float left = bowl.Position.X;
		float right = bowl.End.X;
		float top = bowl.Position.Y;
		// Дно физики: пол кувшина лежит на floorInset выше низа прямоугольника.
		float floor = bowl.End.Y - floorInset;
		var shell = new Rect2(left - GlassSide, top, bowl.Size.X + GlassSide * 2f, floor - top + GlassFloor);
		var cavity = new Rect2(left, top + LipHeight, bowl.Size.X, floor - top - LipHeight);
		if (cavity.Size.Y <= 0f) return;

		DrawShadow(target, new Vector2((left + right) * 0.5f, shell.End.Y + 2f), shell.Size.X * 0.52f, 13f);
		// Внешние углы скруглены, а грани полости прямые: стекло толстое, тела физики не меняются.
		StickerStyle.DrawPanel(target, shell, TopCorner, BottomCorner, BottomCorner, TopCorner,
			GlassLight, GlassDark, OutlineWidth, false);
		DrawCavityWater(target, cavity);
		DrawGlassSheen(target, cavity);
		DrawGloss(target, left, top);
		DrawRim(target, left, right, top);
		DrawOverflowLine(target, left, right, top + rimOffset);
	}

	/// <summary>Вода внутри стекла: градиент от светлой поверхности к тёмному дну.</summary>
	private static void DrawCavityWater(CanvasItem target, Rect2 cavity) =>
		StickerDraw.FillVerticalGradient(target, cavity, CavityLight, CavityDeep);

	/// <summary>Глянцевый блик, как у иконок: пятно и точка под ним на верхней части стекла.</summary>
	private static void DrawGloss(CanvasItem target, float left, float top)
	{
		var center = new Vector2(left + 26f, top + LipHeight + 42f);
		DrawEllipse(target, center, 9f, 16f, new Color(1f, 1f, 1f, 0.16f));
		DrawEllipse(target, center + new Vector2(2f, 42f), 3.4f, 3.8f, new Color(1f, 1f, 1f, 0.12f));
	}

	/// <summary>Отблески стекла: светлая полоса у левой стенки, слабее у правой, и грани полости.</summary>
	private static void DrawGlassSheen(CanvasItem target, Rect2 cavity)
	{
		target.DrawRect(new Rect2(cavity.Position.X, cavity.Position.Y, 22f, cavity.Size.Y), Sheen);
		target.DrawRect(new Rect2(cavity.End.X - 10f, cavity.Position.Y, 10f, cavity.Size.Y),
			new Color(Sheen, Sheen.A * 0.55f));
		target.DrawLine(new Vector2(cavity.Position.X + 1.5f, cavity.Position.Y),
			new Vector2(cavity.Position.X + 1.5f, cavity.End.Y), InnerEdge, 3f);
		target.DrawLine(new Vector2(cavity.End.X - 1.5f, cavity.Position.Y),
			new Vector2(cavity.End.X - 1.5f, cavity.End.Y), InnerEdge, 3f);
		target.DrawLine(new Vector2(cavity.Position.X, cavity.End.Y - 1.5f),
			new Vector2(cavity.End.X, cavity.End.Y - 1.5f), InnerEdge, 3f);
	}

	/// <summary>Кромка стекла: отблеск по верху канта, глянец и тёмная грань внутри «рта».</summary>
	private static void DrawRim(CanvasItem target, float left, float right, float top)
	{
		target.DrawLine(new Vector2(left, top + 3.5f), new Vector2(right, top + 3.5f), RimLight, 3f);
		target.DrawLine(new Vector2(left + 8f, top + 7.5f), new Vector2(left + 74f, top + 7.5f),
			new Color(1f, 1f, 1f, 0.22f), 3f);
		target.DrawLine(new Vector2(left, top + LipHeight), new Vector2(right, top + LipHeight),
			new Color(StickerStyle.Outline, 0.92f), 3f);
	}

	/// <summary>Линия переполнения: штрихи внутри кувшина и риски у его стенок.</summary>
	private static void DrawOverflowLine(CanvasItem target, float left, float right, float y)
	{
		const float inset = 14f;
		const int dashes = 3;
		float dash = (right - left - inset * 2f) / (dashes * 2f - 1f);
		for (int i = 0; i < dashes; i++)
		{
			float x = left + inset + i * dash * 2f;
			target.DrawLine(new Vector2(x, y), new Vector2(x + dash, y), Danger, 3f);
		}
		target.DrawLine(new Vector2(left + 6f, y - 6f), new Vector2(left + 6f, y + 6f), Danger, 3f);
		target.DrawLine(new Vector2(right - 6f, y - 6f), new Vector2(right - 6f, y + 6f), Danger, 3f);
	}

	/// <summary>Мягкая тень под стеклом: вложенные сплюснутые пятна.</summary>
	private static void DrawShadow(CanvasItem target, Vector2 center, float radiusX, float radiusY)
	{
		DrawEllipse(target, center, radiusX, radiusY, new Color(Shadow, Shadow.A * 0.4f));
		DrawEllipse(target, center, radiusX * 0.72f, radiusY * 0.78f, new Color(Shadow, Shadow.A * 0.5f));
		DrawEllipse(target, center, radiusX * 0.44f, radiusY * 0.5f, new Color(Shadow, Shadow.A * 0.62f));
	}

	private static void DrawEllipse(CanvasItem target, Vector2 center, float radiusX, float radiusY, Color color)
	{
		var points = new Vector2[EllipseSegments];
		for (int i = 0; i < EllipseSegments; i++)
		{
			float angle = Mathf.Pi * 2f * i / EllipseSegments;
			points[i] = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
		}
		target.DrawColoredPolygon(points, color);
	}
}
