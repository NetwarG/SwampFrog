using Godot;

namespace SwampFrog;

/// <summary>Центрирует короткое меню; длинное можно прокрутить целиком.</summary>
public static class ModalContent
{
	public static void Mount(Control parent, Control content, int padding = 24)
	{
		content.MouseFilter = Control.MouseFilterEnum.Pass;
		var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		parent.AddChild(margin);
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		foreach (string side in new[] { "left", "right", "top", "bottom" })
			margin.AddThemeConstantOverride("margin_" + side, padding);
		var scroll = new TouchScrollContainer
		{
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		margin.AddChild(scroll);
		var center = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		scroll.AddChild(center);
		center.AddChild(content);
	}
}
