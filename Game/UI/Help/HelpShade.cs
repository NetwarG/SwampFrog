using Godot;

namespace SwampFrog;

/// <summary>
/// Полупрозрачный фон модального окна поверх поля: затемняет игру и закрывает окно по тапу.
/// </summary>
public partial class HelpShade : TouchArea
{
	private static readonly Color Shade = new(0.02f, 0.08f, 0.1f, 0.82f);

	public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), Shade);
}
