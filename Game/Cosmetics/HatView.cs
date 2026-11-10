using Godot;

namespace SwampFrog;

/// <summary>
/// Слой шляпы лягушки: отдельный узел, добавленный после спрайта тела,
/// поэтому шляпа рисуется поверх. Позицию и масштаб наследует от лягушки.
/// </summary>
public partial class HatView : Node2D
{
	/// <summary>Текущая шляпа; вариант «Нет» ничего не рисует.</summary>
	public SkinId Hat { get; private set; } = SkinId.HatNone;

	/// <summary>Меняет шляпу: пустой вызов не перерисовывает слой.</summary>
	public void SetHat(SkinId hat)
	{
		if (Hat == hat)
			return;
		Hat = hat;
		QueueRedraw();
	}

	public override void _Draw() => CosmeticDraw.DrawHat(this, Hat, Vector2.Zero, 1f);
}
