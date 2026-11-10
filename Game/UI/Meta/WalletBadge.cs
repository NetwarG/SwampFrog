using Godot;

namespace SwampFrog;

/// <summary>
/// Бейдж валюты «Слизь» в главном меню: стикер с подписью баланса.
/// Фон рисует <see cref="StickerBackground"/>, значение задаёт <see cref="SetAmount"/>.
/// </summary>
public partial class WalletBadge : Control
{
	/// <summary>Размер бейджа в логических единицах.</summary>
	public const float Width = 208f;
	public const float Height = 52f;

	/// <summary>Цвет бейджа: голубой, как у кнопки «Профиль».</summary>
	private static readonly Color Accent = new("7fd6f2");

	private Label? _label;

	public float UiScale => 1f;

	/// <summary>Показанный баланс слизи.</summary>
	public int Amount { get; private set; }

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(Width * UiScale, Height * UiScale);

		AddChild(new StickerBackground
		{
			LightColor = StickerStyle.Light(Accent),
			DarkColor = StickerStyle.Dark(Accent),
			CornerRadius = Height * 0.5f * UiScale,
			OutlineWidth = StickerStyle.OutlineWidth * UiScale
		});

		_label = UiStyle.MakeLabel(Loc.T(L.SlimeLabel, 0), (int)Mathf.Round(24f * UiScale), new Color("173c34"));
		_label.VerticalAlignment = VerticalAlignment.Center;
		AddChild(_label);
		_label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		Loc.Changed += RefreshText;
	}

	public override void _ExitTree() => Loc.Changed -= RefreshText;

	/// <summary>Обновляет показанный баланс.</summary>
	public void SetAmount(int amount)
	{
		Amount = Mathf.Max(0, amount);
		RefreshText();
	}

	private void RefreshText()
	{
		if (_label != null) _label.Text = Loc.T(L.SlimeLabel, Amount);
	}
}
