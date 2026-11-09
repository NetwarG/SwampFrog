using Godot;

namespace SwampFrog;

/// <summary>
/// Круглая кнопка паузы внутри выдвижной панели: круг и иконка паузы рисуются кодом.
/// </summary>
public partial class PauseCircleButton : TouchArea
{
	private static readonly Vector2 ButtonSize = new(64f, 64f);

	/// <summary>Цвет круга: жёлтый, как у кнопок «В меню».</summary>
	private static readonly Color Accent = new("ffd45e");

	/// <summary>Цвет полос паузы: тёмный, как контур иконок.</summary>
	private static readonly Color Bars = new("2b170f");

	public override void _Ready()
	{
		CustomMinimumSize = ButtonSize;
		Size = ButtonSize;
	}

	public override void _Draw()
	{
		Vector2 center = Size * 0.5f;
		float radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 1f;
		StickerStyle.DrawCirclePanel(this, center, radius,
			StickerStyle.Light(Accent), StickerStyle.Dark(Accent), StickerStyle.OutlineWidth, true);

		// Иконка паузы: две вертикальные полосы.
		const float barWidth = 7f;
		const float barHeight = 26f;
		const float gap = 7f;
		float top = center.Y - barHeight * 0.5f;
		DrawRect(new Rect2(center.X - gap * 0.5f - barWidth, top, barWidth, barHeight), Bars);
		DrawRect(new Rect2(center.X + gap * 0.5f, top, barWidth, barHeight), Bars);
	}
}
