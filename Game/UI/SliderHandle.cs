using Godot;

namespace SwampFrog;

/// <summary>
/// Маленький рычажок слева от кнопки mute: тап раскрывает и сворачивает ползунок громкости.
/// Шеврон показывает направление: раскрытие — влево, сворачивание — вправо.
/// </summary>
public partial class SliderHandle : TouchArea
{
	/// <summary>Размер рычажка в логических единицах.</summary>
	public static readonly Vector2 HandleSize = new(40f, 64f);

	private const float CornerRadius = 14f;

	private static readonly Color Background = new(0.02f, 0.12f, 0.09f, 0.4f);
	private static readonly Color Outline = new("9ee4c3", 0.5f);
	private static readonly Color Glyph = new(0.93f, 1f, 0.97f, 0.9f);

	private bool _expanded;

	public override void _Ready()
	{
		CustomMinimumSize = HandleSize;
		Size = HandleSize;
	}

	/// <summary>Раскрыт ли ползунок: меняет направление шеврона.</summary>
	public void SetExpanded(bool expanded)
	{
		if (_expanded == expanded)
			return;
		_expanded = expanded;
		QueueRedraw();
	}

	public override void _Draw()
	{
		UiStyle.DrawRoundedPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius, Background, Outline, 2f);

		Vector2 c = Size * 0.5f;
		float sign = _expanded ? 1f : -1f;
		Vector2 apex = new(c.X + 5f * sign, c.Y);
		DrawLine(new Vector2(c.X - 5f * sign, c.Y - 8f), apex, Glyph, 3f);
		DrawLine(apex, new Vector2(c.X - 5f * sign, c.Y + 8f), Glyph, 3f);
	}
}
