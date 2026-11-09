using Godot;

namespace SwampFrog;

/// <summary>Что показывает иконка над ползунком.</summary>
public enum VolumeIconKind
{
	/// <summary>Нота: громкость музыки.</summary>
	Music,

	/// <summary>Динамик с волнами: громкость эффектов.</summary>
	Sfx,
}

/// <summary>
/// Иконка над ползунком громкости: нота для музыки, динамик для эффектов.
/// Рисуется кодом в общем стикер-стиле и лежит на тёмной панели панели громкости.
/// </summary>
public partial class VolumeIcon : Control
{
	/// <summary>Размер иконки в логических единицах (совпадает с шириной ползунка).</summary>
	public static readonly Vector2 IconSize = new(40f, 30f);

	/// <summary>Цвет глифа: светлый, читаемый на тёмной панели.</summary>
	private static readonly Color Glyph = new("f4fff0");

	/// <summary>Тип иконки задаётся до добавления в дерево.</summary>
	public VolumeIconKind Kind { get; set; } = VolumeIconKind.Music;

	public override void _Ready()
	{
		CustomMinimumSize = IconSize;
		Size = IconSize;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		if (Kind == VolumeIconKind.Music)
			DrawMusic();
		else
			DrawSpeaker();
	}

	/// <summary>Нота: головка, штиль и флажок.</summary>
	private void DrawMusic()
	{
		Vector2 c = Size * 0.5f;
		float stemX = c.X + 1f;
		DrawCircle(new Vector2(c.X - 6f, c.Y + 8f), 6f, Glyph);
		DrawLine(new Vector2(stemX, c.Y + 8f), new Vector2(stemX, c.Y - 10f), Glyph, 3f);
		DrawColoredPolygon(new[]
		{
			new Vector2(stemX, c.Y - 10f),
			new Vector2(stemX + 9f, c.Y - 5f),
			new Vector2(stemX, c.Y - 1f),
		}, Glyph);
	}

	/// <summary>Динамик: корпус, раструб и волны звука.</summary>
	private void DrawSpeaker()
	{
		Vector2 c = Size * 0.5f;
		DrawRect(new Rect2(c.X - 11f, c.Y - 5f, 7f, 10f), Glyph);
		DrawColoredPolygon(new[]
		{
			new Vector2(c.X - 4f, c.Y - 5f),
			new Vector2(c.X + 4f, c.Y - 11f),
			new Vector2(c.X + 4f, c.Y + 11f),
			new Vector2(c.X - 4f, c.Y + 5f),
		}, Glyph);
		DrawArc(new Vector2(c.X + 5f, c.Y), 6f, -Mathf.Pi / 3f, Mathf.Pi / 3f, 10, Glyph, 2f);
		DrawArc(new Vector2(c.X + 5f, c.Y), 10f, -Mathf.Pi / 3f, Mathf.Pi / 3f, 12, Glyph, 2f);
	}
}
