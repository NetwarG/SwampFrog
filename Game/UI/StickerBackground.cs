using Godot;

namespace SwampFrog;

/// <summary>
/// Фон кнопки-«стикера»: рисуется отдельным слоем под родителем, поэтому надпись
/// и иконка кнопки остаются поверх градиентной заливки и блика.
/// </summary>
public partial class StickerBackground : Control
{
	/// <summary>Тон верхнего левого угла градиента.</summary>
	public Color LightColor { get; set; } = Colors.White;

	/// <summary>Тон нижнего правого угла градиента.</summary>
	public Color DarkColor { get; set; } = Colors.Gray;

	/// <summary>Радиус скругления кнопки.</summary>
	public float CornerRadius { get; set; } = UiStyle.CornerRadius;

	/// <summary>Толщина коричневого контура.</summary>
	public float OutlineWidth { get; set; } = StickerStyle.OutlineWidth;

	/// <summary>Рисовать ли глянцевый блик.</summary>
	public bool Gloss { get; set; } = true;

	/// <summary>Курсор наведён на кнопку.</summary>
	public bool Hovered { get; private set; }

	/// <summary>Кнопка удерживается.</summary>
	public bool Pressed { get; private set; }

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ShowBehindParent = true;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		Resized += QueueRedraw;
	}

	/// <summary>Наведение курсора: подсвечивает заливку.</summary>
	public void SetHovered(bool hovered)
	{
		if (Hovered == hovered)
			return;
		Hovered = hovered;
		QueueRedraw();
	}

	/// <summary>Нажатие: затемняет заливку, чтобы отклик был виден и без текста.</summary>
	public void SetPressed(bool pressed)
	{
		if (Pressed == pressed)
			return;
		Pressed = pressed;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Color light = LightColor;
		Color dark = DarkColor;
		if (Pressed)
		{
			light = light.Darkened(0.14f);
			dark = dark.Darkened(0.18f);
		}
		else if (Hovered)
		{
			light = light.Lightened(0.14f);
			dark = dark.Lightened(0.08f);
		}
		StickerStyle.DrawPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius, light, dark, OutlineWidth, Gloss);
	}
}
