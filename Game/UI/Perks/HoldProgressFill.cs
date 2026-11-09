using Godot;

namespace SwampFrog;

/// <summary>
/// Заливка карточки перка слева направо: показывает прогресс удержания.
/// Скругление совпадает со скруглением карточки, кромка прогресса подсвечена белым,
/// поэтому заполнение видно и под пальцем.
/// </summary>
public partial class HoldProgressFill : Control
{
	/// <summary>Прогресс заполнения от 0 до 1.</summary>
	public float Progress { get; set; }

	/// <summary>Цвет заливки.</summary>
	public Color FillColor { get; set; } = new Color(1f, 1f, 1f, 0.95f);

	/// <summary>Коэффициент масштаба UI, от которого зависят кромка и скругление.</summary>
	public float UiScale { get; set; } = 1f;

	/// <summary>Радиус скругления карточки в логических единицах.</summary>
	public float CornerRadius { get; set; } = UiStyle.CornerRadius;

	private StyleBoxFlat? _style;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;
		_style = new StyleBoxFlat();
	}

	public override void _Draw()
	{
		if (_style == null)
			return;

		float width = Size.X * Mathf.Clamp(Progress, 0f, 1f);
		if (width <= 0f)
			return;

		// Радиус не может быть больше половины заливки: иначе углы складываются и вылетают за карточку.
		int radius = (int)Mathf.Round(Mathf.Min(CornerRadius * UiScale, Mathf.Min(width, Size.Y) * 0.5f));
		_style.BgColor = FillColor;
		_style.BorderColor = new Color(1f, 1f, 1f, 0.95f);
		_style.SetCornerRadiusAll(radius);
		_style.BorderWidthRight = Progress < 1f ? (int)Mathf.Round(3f * UiScale) : 0;
		DrawStyleBox(_style, new Rect2(Vector2.Zero, new Vector2(width, Size.Y)));
	}
}
