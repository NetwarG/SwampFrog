using Godot;

namespace SwampFrog;

/// <summary>
/// Круглая кнопка паузы внутри выдвижной панели: круг и иконка паузы рисуются кодом.
/// </summary>
public partial class PauseCircleButton : TouchArea
{
	private static readonly Vector2 ButtonSize = new(56f, 56f);
	private static readonly Color Fill = new("ffd45e");
	private static readonly Color Outline = new(0.09f, 0.24f, 0.20f, 0.9f);
	private static readonly Color Bars = new("173c34");

	public override void _Ready()
	{
		CustomMinimumSize = ButtonSize;
		Size = ButtonSize;
	}

	public override void _Draw()
	{
		Vector2 center = Size * 0.5f;
		float radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 1f;
		DrawCircle(center, radius, new Color(Fill, 0.92f));
		DrawCircle(center, radius, Outline, false, 2f);

		// Иконка паузы: две вертикальные полосы.
		const float barWidth = 6f;
		const float barHeight = 22f;
		const float gap = 6f;
		float top = center.Y - barHeight * 0.5f;
		DrawRect(new Rect2(center.X - gap * 0.5f - barWidth, top, barWidth, barHeight), Bars);
		DrawRect(new Rect2(center.X + gap * 0.5f, top, barWidth, barHeight), Bars);
	}
}
