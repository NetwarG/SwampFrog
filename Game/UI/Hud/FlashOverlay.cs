using Godot;

namespace SwampFrog;

/// <summary>Красная вспышка на весь экран при попадании мусором.</summary>
public partial class FlashOverlay : Control
{
	private float _alpha;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		_alpha = 0f;
	}

	public void Start()
	{
		_alpha = 0.6f;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_alpha <= 0f)
		{
			return;
		}
		_alpha = Mathf.Max(0f, _alpha - (float)delta * 1.5f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_alpha <= 0f)
		{
			return;
		}
		DrawRect(new Rect2(Vector2.Zero, Size), new Color(1f, 0.16f, 0.12f, _alpha));
	}
}
