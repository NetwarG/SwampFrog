using Godot;

namespace SwampFrog;

/// <summary>
/// Цепочка слияния фруктов для подсказки мини-игры: иконки видов по порядку FruitCatalog.All
/// со стрелками между ними. Рисуется кодом, поэтому окно подсказки остаётся компактным.
/// </summary>
public partial class FruitChainView : Control
{
	/// <summary>Высота видимой части самого крупного фрукта в логических единицах.</summary>
	private const float MaxIconHeight = 54f;

	/// <summary>Наименьшая высота иконки: мелкие фрукты не должны теряться.</summary>
	private const float MinIconHeight = 26f;

	private const float ArrowWidth = 12f;
	private const float Gap = 2f;

	private static readonly Color Arrow = new(0.62f, 0.93f, 0.8f, 0.75f);

	public FruitChainView()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		// Ширина ряда считается по размеру, который выдаёт компоновка окна.
		Resized += QueueRedraw;
	}

	/// <summary>Сколько видов показано: полнота цепочки.</summary>
	public int KindCount => FruitCatalog.All.Length;

	/// <summary>
	/// Рамки иконок в координатах вида. Размеры берутся от силуэта картинки, а не от холста
	/// спрайта с прозрачными полями, поэтому фрукты заполняют свои рамки и стоят по центру ряда.
	/// </summary>
	public Rect2[] IconRects()
	{
		int count = FruitCatalog.All.Length;
		var rects = new Rect2[count];
		var sizes = new Vector2[count];
		float maxRadius = 1f;
		foreach (FruitSpec spec in FruitCatalog.All) maxRadius = Mathf.Max(maxRadius, spec.BaseRadius);

		float total = 0f;
		for (int i = 0; i < count; i++)
		{
			FruitSpec spec = FruitCatalog.All[i];
			float height = Mathf.Max(MinIconHeight, MaxIconHeight * spec.BaseRadius / maxRadius);
			Texture2D? texture = FruitCatalog.Icon(spec.Kind);
			Vector2 content = texture == null ? Vector2.One : ContentRegion(spec.Kind, texture).Size;
			// Пропорции считаются во float: у холста спрайта ширина и высота — целые пиксели.
			float aspect = content.X / Mathf.Max(1f, content.Y);
			sizes[i] = new Vector2(height * aspect, height);
			total += sizes[i].X;
			if (i < count - 1) total += ArrowWidth + Gap * 2f;
		}

		// Ряд целиком вписывается в отведённую ширину окна подсказки.
		float fit = Size.X > 0f ? Mathf.Min(1f, Size.X / Mathf.Max(1f, total)) : 1f;
		float centerY = Size.Y * 0.5f;
		float x = (Size.X - total * fit) * 0.5f;
		for (int i = 0; i < count; i++)
		{
			Vector2 icon = sizes[i] * fit;
			rects[i] = new Rect2(x, centerY - icon.Y * 0.5f, icon.X, icon.Y);
			x += icon.X;
			if (i < count - 1) x += (ArrowWidth + Gap * 2f) * fit;
		}
		return rects;
	}

	public override void _Draw()
	{
		Rect2[] rects = IconRects();
		float centerY = Size.Y * 0.5f;
		for (int i = 0; i < rects.Length; i++)
		{
			Texture2D? texture = FruitCatalog.Icon(FruitCatalog.All[i].Kind);
			if (texture != null && rects[i].Size.X > 0f && rects[i].Size.Y > 0f)
				DrawTextureRectRegion(texture, rects[i], ContentRegion(FruitCatalog.All[i].Kind, texture));
			if (i >= rects.Length - 1) break;
			// Стрелка стоит по центру промежутка между соседними иконками.
			float gap = rects[i + 1].Position.X - rects[i].End.X;
			float width = Mathf.Min(ArrowWidth, gap * 0.7f);
			DrawArrow(rects[i].End.X + (gap - width) * 0.5f, width, centerY);
		}
	}

	/// <summary>Регион содержимого картинки: силуэт вида в пикселях текстуры.</summary>
	private static Rect2 ContentRegion(FruitKind kind, Texture2D texture)
	{
		FruitShape shape = FruitShape.Of(kind);
		float half = texture.GetHeight() * 0.5f;
		Vector2 center = new Vector2(texture.GetWidth() * 0.5f, half) + shape.Center * half;
		var size = new Vector2(2f * shape.HitX * half, 2f * shape.HitY * half);
		return new Rect2(center - size * 0.5f, size);
	}

	private void DrawArrow(float x, float width, float centerY)
	{
		float head = Mathf.Min(width * 0.45f, 7f);
		DrawLine(new Vector2(x, centerY), new Vector2(x + width - head * 0.5f, centerY), Arrow, 2f);
		DrawLine(new Vector2(x + width - head, centerY - head), new Vector2(x + width, centerY), Arrow, 2f);
		DrawLine(new Vector2(x + width - head, centerY + head), new Vector2(x + width, centerY), Arrow, 2f);
	}
}
