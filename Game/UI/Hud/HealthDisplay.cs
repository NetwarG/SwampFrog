using Godot;

namespace SwampFrog;

public partial class HealthDisplay : Control
{
	private const float IconSize = 38f;
	private const float IconGap = 8f;

	private TextureRect? _heart;
	private Label? _valueLabel;

	public string ValueText => _valueLabel?.Text ?? string.Empty;
	public bool HasIcon => _heart?.Texture != null;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_heart = new TextureRect
		{
			Name = "Heart",
			Texture = GD.Load<Texture2D>("res://Game/assets/ui/hp.png"),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 10
		};
		AddChild(_heart);

		_valueLabel = new Label
		{
			Name = "Value",
			Text = "3/3",
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
			ZIndex = 10
		};
		_valueLabel.AddThemeFontSizeOverride("font_size", 32);
		_valueLabel.AddThemeColorOverride("font_color", Colors.White);
		_valueLabel.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.12f, 0.09f, 0.85f));
		_valueLabel.AddThemeConstantOverride("outline_size", 4);
		_valueLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		_valueLabel.AddThemeConstantOverride("shadow_offset_y", 2);
		AddChild(_valueLabel);
		UpdateLayout();
	}

	public void SetLives(int lives, int maxLives)
	{
		if (_valueLabel == null)
			return;
		_valueLabel.Text = $"{lives}/{maxLives}";
		UpdateLayout();
	}

	private void UpdateLayout()
	{
		if (_heart == null || _valueLabel == null)
			return;

		_valueLabel.ResetSize();
		float height = Mathf.Max(IconSize, _valueLabel.Size.Y);
		_heart.Position = new Vector2(0f, (height - IconSize) * 0.5f);
		_heart.Size = Vector2.One * IconSize;
		_valueLabel.Position = new Vector2(IconSize + IconGap, (height - _valueLabel.Size.Y) * 0.5f);
		Size = new Vector2(IconSize + IconGap + _valueLabel.Size.X, height);
	}
}
