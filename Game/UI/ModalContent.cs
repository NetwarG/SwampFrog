using Godot;

namespace SwampFrog;

/// <summary>Размещает меню в безопасной области без прокрутки и обрезающих контейнеров.</summary>
public static class ModalContent
{
	public static void Mount(Control parent, Control content, int padding = 24)
	{
		content.MouseFilter = Control.MouseFilterEnum.Ignore;
		parent.AddChild(content);
		content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
		bool fitQueued = false;

		void QueueFit()
		{
			if (fitQueued) return;
			fitQueued = true;
			Callable.From(FitContent).CallDeferred();
		}

		void FitContent()
		{
			fitQueued = false;
			if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(content)
				|| !content.IsVisibleInTree() || parent.Size.X <= 0f || parent.Size.Y <= 0f) return;
			// Минимальная высота меняется после обновления текста и списка фруктов.
			content.ResetSize();
			float inset = Mathf.Min(padding, Mathf.Min(parent.Size.X, parent.Size.Y) * 0.04f);
			Vector2 available = (parent.Size - Vector2.One * (inset * 2f)).Max(Vector2.One);
			Vector2 size = content.Size;
			float scale = Mathf.Min(1f, Mathf.Min(available.X / Mathf.Max(1f, size.X),
				available.Y / Mathf.Max(1f, size.Y)));
			content.Scale = Vector2.One * scale;
			content.Position = (parent.Size - size * scale) * 0.5f;
		}

		parent.Resized += QueueFit;
		content.Resized += QueueFit;
		content.MinimumSizeChanged += QueueFit;
		content.VisibilityChanged += QueueFit;
		content.TreeExiting += () => parent.Resized -= QueueFit;
		QueueFit();
	}
}
