using Godot;

namespace SwampFrog;

/// <summary>
/// Отрисовка скинов лягушки иконками из Game/assets/skins (см. <see cref="SkinCatalog.Icon"/>).
/// Иконка рисуется по центру переданной точки с размером по слоту; перчатки поворачиваются
/// по направлению руки. Арт легко заменить: положить свой PNG с тем же именем.
/// unit — масштаб: у лягушки ≈1 (тело 85 единиц), у превью больше.
/// </summary>
public static class CosmeticDraw
{
	/// <summary>
	/// Якорь шляпы: середина иконки по вертикали. Подобран так, чтобы низ шляпы
	/// ложился на верх головы лягушки (спрайт имеет прозрачное поле сверху).
	/// </summary>
	public const float HatAnchorY = -54f;

	/// <summary>Якорь подставки: середина иконки на уровне лап лягушки.</summary>
	public const float StandAnchorY = 35f;

	private const float HatIconSize = 62f;
	private const float GlovesIconSize = 44f;
	private const float StandIconSize = 120f;

	/// <summary>Шляпа: иконка по центру головы, без поворота.</summary>
	public static void DrawHat(CanvasItem target, SkinId hat, Vector2 center, float unit) =>
		DrawIcon(target, hat, center, HatIconSize * unit, 0f);

	/// <summary>Перчатки: иконка поворачивается по направлению руки (angle — радианы).</summary>
	public static void DrawGloves(CanvasItem target, SkinId gloves, Vector2 center, float angle, float unit) =>
		DrawIcon(target, gloves, center, GlovesIconSize * unit, angle);

	/// <summary>Подставка: иконка по центру под телом, без поворота.</summary>
	public static void DrawStand(CanvasItem target, SkinId stand, Vector2 center, float unit) =>
		DrawIcon(target, stand, center, StandIconSize * unit, 0f);

	private static void DrawIcon(CanvasItem target, SkinId id, Vector2 center, float size, float angle)
	{
		Texture2D? icon = SkinCatalog.Icon(id);
		if (icon == null)
			return;
		var half = new Vector2(size * 0.5f, size * 0.5f);
		target.DrawSetTransformMatrix(new Transform2D(angle, Vector2.One, 0f, center));
		target.DrawTextureRect(icon, new Rect2(-half, new Vector2(size, size)), false);
		target.DrawSetTransformMatrix(Transform2D.Identity);
	}
}
