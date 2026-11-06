using Godot;

namespace SwampFrog;

/// <summary>
/// Квадратная полупрозрачная кнопка с иконкой динамика в правом верхнем углу меню.
/// Тап полностью включает или отключает звук (шина Master в <see cref="SoundSettings"/>).
/// </summary>
public partial class SoundMuteButton : TouchArea
{
	/// <summary>Размер кнопки в логических единицах.</summary>
	public static readonly Vector2 ButtonSize = new(64f, 64f);

	private const float CornerRadius = 16f;

	private static readonly Color Background = new(0.02f, 0.12f, 0.09f, 0.4f);
	private static readonly Color Outline = new("9ee4c3", 0.5f);
	private static readonly Color Glyph = new(0.93f, 1f, 0.97f, 0.9f);
	private static readonly Color Slash = new("ff8c7a");

	private bool _muted;

	public override void _Ready()
	{
		CustomMinimumSize = ButtonSize;
		Size = ButtonSize;
	}

	/// <summary>Отключён ли звук: рисует перечёркнутый динамик.</summary>
	public void SetMuted(bool muted)
	{
		if (_muted == muted)
			return;
		_muted = muted;
		QueueRedraw();
	}

	public override void _Draw()
	{
		UiStyle.DrawRoundedPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius, Background, Outline, 2f);
		DrawSpeaker();
	}

	private void DrawSpeaker()
	{
		Vector2 c = Size * 0.5f;
		Color color = _muted ? new Color(Glyph, 0.55f) : Glyph;

		// Корпус динамика: прямоугольник и раструб.
		DrawRect(new Rect2(c.X - 13f, c.Y - 6f, 8f, 12f), color);
		Vector2[] cone =
		{
			new(c.X - 5f, c.Y - 6f),
			new(c.X + 4f, c.Y - 13f),
			new(c.X + 4f, c.Y + 13f),
			new(c.X - 5f, c.Y + 6f),
		};
		DrawColoredPolygon(cone, color);

		if (_muted)
		{
			DrawLine(new Vector2(c.X - 12f, c.Y - 12f), new Vector2(c.X + 14f, c.Y + 12f), Slash, 3f);
			return;
		}

		// Волны звука.
		DrawArc(new Vector2(c.X + 5f, c.Y), 7f, -Mathf.Pi / 3f, Mathf.Pi / 3f, 10, color, 2f);
		DrawArc(new Vector2(c.X + 5f, c.Y), 12f, -Mathf.Pi / 3f, Mathf.Pi / 3f, 12, color, 2f);
	}
}
