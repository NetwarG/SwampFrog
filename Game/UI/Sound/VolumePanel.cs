using Godot;

namespace SwampFrog;

/// <summary>
/// Тёмная полупрозрачная панель под вертикальными ползунками громкости:
/// на светлом фоне меню треки, бегунки и иконки остаются читаемыми.
/// </summary>
public partial class VolumePanel : Control
{
	/// <summary>Размер панели: два ползунка с иконками и отступы.</summary>
	public static readonly Vector2 PanelSize = new(112f, 232f);

	/// <summary>Отступ содержимого от края панели.</summary>
	public const float Pad = 12f;

	/// <summary>Зазор между иконкой и ползунком.</summary>
	public const float IconGap = 8f;

	/// <summary>Зазор между колонками музыки и эффектов.</summary>
	public const float ColumnGap = 8f;

	private const float CornerRadius = 16f;

	private static readonly Color Light = new(0.08f, 0.24f, 0.19f, 0.78f);
	private static readonly Color Dark = new(0.02f, 0.1f, 0.08f, 0.78f);

	public override void _Ready()
	{
		CustomMinimumSize = PanelSize;
		Size = PanelSize;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw() =>
		StickerStyle.DrawPanel(this, new Rect2(Vector2.Zero, Size), CornerRadius,
			Light, Dark, StickerStyle.OutlineWidth, true);
}
