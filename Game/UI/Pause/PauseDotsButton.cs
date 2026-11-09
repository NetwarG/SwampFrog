using Godot;

namespace SwampFrog;

/// <summary>
/// Квадратная кнопка-стикер с тремя точками сверху вниз в правом верхнем углу поля.
/// Четверть кнопки уходит за правый край: видимая часть прижата к кромке.
/// Собственный Control, а не Button: фон и точки рисуются кодом в стиле иконок игры.
/// </summary>
public partial class PauseDotsButton : TouchArea
{
	/// <summary>Размер кнопки в логических единицах.</summary>
	private static readonly Vector2 ButtonSize = new(72f, 72f);

	/// <summary>Четверть кнопки уходит за правый край поля: кнопка прижата к кромке, как язычок.</summary>
	public static readonly float EdgeSink = ButtonSize.X * 0.25f;

	private const float CornerRadius = 18f;

	/// <summary>Цвет кнопки: жёлтый, как у кнопок «В меню».</summary>
	private static readonly Color Accent = new("ffd45e");

	/// <summary>Цвет точек: тёмный, как контур иконок.</summary>
	private static readonly Color DotColor = new("2b170f");

	private const float DotRadius = 6f;
	/// <summary>Шаг между точками: три точки идут сверху вниз.</summary>
	private const float DotStep = 20f;

	public override void _Ready()
	{
		CustomMinimumSize = ButtonSize;
		Size = ButtonSize;
	}

	/// <summary>Фон рисуется общим стилем кнопок: градиент, коричневый контур и блик.</summary>
	public override void _Draw()
	{
		StickerStyle.DrawPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius,
			StickerStyle.Light(Accent), StickerStyle.Dark(Accent), StickerStyle.OutlineWidth, true);

		// Три точки сверху вниз по центру видимой части: четверть кнопки скрыта за кромкой.
		Vector2 center = new((Size.X - EdgeSink) * 0.5f, Size.Y * 0.5f);
		for (int i = -1; i <= 1; i++)
			DrawCircle(new Vector2(center.X, center.Y + i * DotStep), DotRadius, DotColor);
	}
}
