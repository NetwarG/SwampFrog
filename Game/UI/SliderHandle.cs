using Godot;

namespace SwampFrog;

/// <summary>
/// Маленький рычажок слева от кнопки mute: тап раскрывает и сворачивает панель
/// с ползунками громкости. Шеврон показывает направление: раскрытие — вниз,
/// сворачивание — вверх.
/// </summary>
public partial class SliderHandle : TouchArea
{
	/// <summary>Размер рычажка в логических единицах.</summary>
	public static readonly Vector2 HandleSize = new(40f, 64f);

	private const float CornerRadius = 14f;

	/// <summary>Цвет рычажка: жёлтый, как у кнопок «В меню».</summary>
	private static readonly Color Accent = new("ffd45e");

	/// <summary>Цвет шеврона: тёмный, как контур иконок.</summary>
	private static readonly Color Glyph = new("2b170f");

	private bool _expanded;

	public override void _Ready()
	{
		CustomMinimumSize = HandleSize;
		Size = HandleSize;
	}

	/// <summary>Раскрыта ли панель: меняет направление шеврона.</summary>
	public void SetExpanded(bool expanded)
	{
		if (_expanded == expanded)
			return;
		_expanded = expanded;
		QueueRedraw();
	}

	public override void _Draw()
	{
		StickerStyle.DrawPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius,
			StickerStyle.Light(Accent), StickerStyle.Dark(Accent), StickerStyle.OutlineWidth, true);

		Vector2 c = Size * 0.5f;
		// Пока панель скрыта — шеврон вниз (раскрытие), пока показана — вверх.
		float sign = _expanded ? -1f : 1f;
		Vector2 apex = new(c.X, c.Y + 5f * sign);
		DrawLine(new Vector2(c.X - 8f, c.Y - 5f * sign), apex, Glyph, 3f);
		DrawLine(new Vector2(c.X + 8f, c.Y - 5f * sign), apex, Glyph, 3f);
	}
}
