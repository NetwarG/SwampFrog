using Godot;

namespace SwampFrog;

/// <summary>Эталонная ширина мира и адаптация его высоты к экрану.</summary>
public static class Playfield
{
	public static readonly Vector2 Size = new(540f, 960f);
	public static readonly Rect2 Bounds = new(Vector2.Zero, Size);

	/// <summary>Палитра воды: верх пруда (поверхность) и низ (глубина).</summary>
	public static readonly Color PondTop = new("1f7a6b");
	public static readonly Color PondBottom = new("0a352f");

	/// <summary>
	/// Колонка поля с наложенным HUD: высота всегда равна высоте экрана, поэтому
	/// вертикальных неигровых полос нет. На широком экране ширина остаётся портретной.
	/// </summary>
	public static Rect2 ColumnFor(Rect2 screen)
	{
		if (!screen.HasArea()) return new Rect2(screen.Position, Vector2.Zero);
		float width = Mathf.Min(screen.Size.X, screen.Size.Y * Size.X / Size.Y);
		return new Rect2(screen.GetCenter().X - width * 0.5f, screen.Position.Y, width, screen.Size.Y);
	}

	/// <summary>
	/// Возвращает размер мира для игровой колонки. Ширина остаётся эталонной,
	/// а высота равна высоте колонки: по вертикали поле ничем не ограничено.
	/// Пропорции колонки ограничивает ColumnFor, а не минимальная высота поля.
	/// </summary>
	public static Vector2 SizeFor(Rect2 available)
	{
		if (!available.HasArea() || available.Size.X <= 0f)
		{
			return Size;
		}

		// SubViewport принимает целочисленные размеры: физика и ввод используют те же координаты.
		float height = Mathf.Max(1f, Mathf.Ceil(Size.X * available.Size.Y / available.Size.X));
		return new Vector2(Size.X, height);
	}

	public static Rect2 Fit(Rect2 available) => Fit(available, Size);

	public static Rect2 Fit(Rect2 available, Vector2 worldSize)
	{
		if (!available.HasArea() || worldSize.X <= 0f || worldSize.Y <= 0f)
		{
			return new Rect2(available.Position, Vector2.Zero);
		}

		float scale = Mathf.Min(available.Size.X / worldSize.X, available.Size.Y / worldSize.Y);
		Vector2 size = worldSize * Mathf.Max(0f, scale);
		return new Rect2(available.GetCenter() - size * 0.5f, size);
	}
}
