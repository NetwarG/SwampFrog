using Godot;

namespace SwampFrog;

/// <summary>
/// Квадратная полупрозрачная кнопка с тремя точками сверху вниз в правом верхнем углу поля.
/// Четверть кнопки уходит за правый край: видимая часть прижата к кромке.
/// Собственный Control, а не Button: фон и точки рисуются кодом и не зависят от шрифта.
/// </summary>
public partial class PauseDotsButton : TouchArea
{
	/// <summary>Размер кнопки в логических единицах.</summary>
	private static readonly Vector2 ButtonSize = new(72f, 72f);

	/// <summary>Четверть кнопки уходит за правый край поля: кнопка прижата к кромке, как язычок.</summary>
	public static readonly float EdgeSink = ButtonSize.X * 0.25f;

	private const float CornerRadius = 18f;

	private static readonly Color Background = new(0.02f, 0.12f, 0.09f, 0.38f);
	private static readonly Color Outline = new("9ee4c3", 0.5f);
	private static readonly Color DotColor = new(0.93f, 1f, 0.97f, 0.8f);

	private const float DotRadius = 6f;
	/// <summary>Шаг между точками: три точки идут сверху вниз.</summary>
	private const float DotStep = 20f;

	public override void _Ready()
	{
		CustomMinimumSize = ButtonSize;
		Size = ButtonSize;
	}

	public override void _Draw()
	{
		// Скруглённый квадрат: кнопка стоит в углу поля и не прижата к самой кромке.
		DrawRect(new Rect2(CornerRadius, 0f, Size.X - CornerRadius * 2f, Size.Y), Background);
		DrawRect(new Rect2(0f, CornerRadius, Size.X, Size.Y - CornerRadius * 2f), Background);
		DrawCircle(new Vector2(CornerRadius, CornerRadius), CornerRadius, Background);
		DrawCircle(new Vector2(Size.X - CornerRadius, CornerRadius), CornerRadius, Background);
		DrawCircle(new Vector2(CornerRadius, Size.Y - CornerRadius), CornerRadius, Background);
		DrawCircle(new Vector2(Size.X - CornerRadius, Size.Y - CornerRadius), CornerRadius, Background);

		DrawLine(new Vector2(CornerRadius, 1f), new Vector2(Size.X - CornerRadius, 1f), Outline, 2f);
		DrawLine(new Vector2(CornerRadius, Size.Y - 1f), new Vector2(Size.X - CornerRadius, Size.Y - 1f), Outline, 2f);
		DrawLine(new Vector2(1f, CornerRadius), new Vector2(1f, Size.Y - CornerRadius), Outline, 2f);
		DrawLine(new Vector2(Size.X - 1f, CornerRadius), new Vector2(Size.X - 1f, Size.Y - CornerRadius), Outline, 2f);
		DrawArc(new Vector2(CornerRadius, CornerRadius), CornerRadius, Mathf.Pi, Mathf.Pi * 1.5f, 8, Outline, 2f);
		DrawArc(new Vector2(Size.X - CornerRadius, CornerRadius), CornerRadius, Mathf.Pi * 1.5f, Mathf.Pi * 2f, 8, Outline, 2f);
		DrawArc(new Vector2(Size.X - CornerRadius, Size.Y - CornerRadius), CornerRadius, 0f, Mathf.Pi * 0.5f, 8, Outline, 2f);
		DrawArc(new Vector2(CornerRadius, Size.Y - CornerRadius), CornerRadius, Mathf.Pi * 0.5f, Mathf.Pi, 8, Outline, 2f);

		// Три точки сверху вниз по центру видимой части: четверть кнопки скрыта за кромкой.
		Vector2 center = new((Size.X - EdgeSink) * 0.5f, Size.Y * 0.5f);
		for (int i = -1; i <= 1; i++)
			DrawCircle(new Vector2(center.X, center.Y + i * DotStep), DotRadius, DotColor);
	}
}
