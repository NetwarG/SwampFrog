using Godot;

namespace SwampFrog;

/// <summary>
/// Маленький полукруглый полупрозрачный язычок со стрелкой влево у правого края экрана.
/// Собственный Control, а не Button: язычок и стрелка рисуются кодом и не зависят от шрифта.
/// </summary>
public partial class MenuArrowButton : TouchArea
{
	/// <summary>Размер язычка: полукруг радиусом 22, прижатый к правому краю экрана.</summary>
	private static readonly Vector2 TabSize = new(22f, 44f);

	private static readonly Color Background = new(0.02f, 0.12f, 0.09f, 0.38f);
	private static readonly Color Outline = new("9ee4c3", 0.5f);
	private static readonly Color ArrowColor = new(0.93f, 1f, 0.97f, 0.8f);

	public override void _Ready()
	{
		CustomMinimumSize = TabSize;
		Size = TabSize;
	}

	public override void _Draw()
	{
		// Полукруг: центр круга лежит на кромке экрана, поэтому видна только левая половина.
		Vector2 center = new(Size.X, Size.Y * 0.5f);
		float radius = Size.Y * 0.5f;
		DrawCircle(center, radius, Background);
		DrawCircle(center, radius, Outline, false, 2f);

		// Стрелка влево: панель выдвигается влево от края экрана.
		Vector2 arrow = new(Size.X * 0.5f, Size.Y * 0.5f);
		Vector2[] chevron =
		{
			arrow + new Vector2(4f, -6f),
			arrow + new Vector2(-4f, 0f),
			arrow + new Vector2(4f, 6f),
		};
		DrawPolyline(chevron, ArrowColor, 4f, true);
	}
}
