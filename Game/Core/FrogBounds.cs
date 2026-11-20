using Godot;

namespace SwampFrog;

/// <summary>
/// Вертикальная полоса движения лягушки: верхняя часть поля остаётся под HUD и падение
/// фруктов, нижняя — под палец и подсказку. Доли заданы от высоты поля, половина тела
/// не даёт спрайту заходить за границы полосы.
/// </summary>
public static class FrogBounds
{
	/// <summary>Доля высоты поля, выше которой лягушка не поднимается.</summary>
	public const float VerticalTopRatio = 0.30f;

	/// <summary>Доля высоты поля, ниже которой лягушка не опускается.</summary>
	public const float VerticalBottomRatio = 0.74f;

	/// <summary>Верхняя граница центра тела.</summary>
	public static float MinY(float viewHeight, float halfBody) =>
		viewHeight * VerticalTopRatio + halfBody;

	/// <summary>Нижняя граница центра тела; не поднимается выше верхней на низком поле.</summary>
	public static float MaxY(float viewHeight, float halfBody) =>
		Mathf.Max(MinY(viewHeight, halfBody), viewHeight * VerticalBottomRatio - halfBody);
}
