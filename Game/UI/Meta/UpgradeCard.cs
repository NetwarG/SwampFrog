using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Карточка постоянного улучшения: иконка, имя с уровнем, описание эффекта и цена.
/// Фон — стикер в цвете состояния (прокачано / можно купить / не хватает слизи).
/// </summary>
public partial class UpgradeCard : Control
{
	public const float CardWidth = 480f;
	public const float CardHeight = 96f;

	private const float IconSize = 74f;

	private UpgradeEntry? _entry;
	private StickerBackground? _background;
	private Label? _nameLabel;
	private Label? _effectLabel;
	private Label? _maxLabel;
	private StickerButton? _buyButton;

	/// <summary>Игрок нажал «Купить».</summary>
	public event Action<UpgradeId>? PurchaseRequested;

	/// <summary>Задаёт улучшение карточки; вызывается до добавления в дерево.</summary>
	public void Setup(UpgradeEntry entry)
	{
		_entry = entry;
		if (_background != null)
			Refresh();
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(CardWidth, CardHeight);
		Size = new Vector2(CardWidth, CardHeight);

		_background = new StickerBackground { CornerRadius = 18f };
		AddChild(_background);

		_nameLabel = UiStyle.MakeLabel(string.Empty, 22, new Color("173c34"));
		_nameLabel.HorizontalAlignment = HorizontalAlignment.Left;
		_nameLabel.Position = new Vector2(104f, 10f);
		_nameLabel.Size = new Vector2(236f, 30f);
		AddChild(_nameLabel);

		_effectLabel = UiStyle.MakeLabel(string.Empty, 15, new Color("3a2a12"));
		_effectLabel.HorizontalAlignment = HorizontalAlignment.Left;
		_effectLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_effectLabel.Position = new Vector2(104f, 42f);
		_effectLabel.Size = new Vector2(236f, 46f);
		AddChild(_effectLabel);

		_maxLabel = UiStyle.MakeLabel(string.Empty, 18, new Color("3a2a12"));
		_maxLabel.Position = new Vector2(348f, 32f);
		_maxLabel.Size = new Vector2(120f, 30f);
		AddChild(_maxLabel);

		_buyButton = (StickerButton)UiStyle.MakeButton(string.Empty, 0.72f, new Color("7fd6f2"),
			new Vector2(120f, 52f));
		_buyButton.Position = new Vector2(348f, 22f);
		_buyButton.Pressed += OnAction;
		AddChild(_buyButton);

		Refresh();
	}

	public override void _ExitTree()
	{
		if (_buyButton != null) _buyButton.Pressed -= OnAction;
	}

	private void OnAction()
	{
		if (_entry == null || _entry.Maxed || !_entry.Affordable)
			return;
		PurchaseRequested?.Invoke(_entry.Id);
	}

	/// <summary>Перечитывает подписи, цвет и доступность покупки.</summary>
	private void Refresh()
	{
		if (_entry == null || _nameLabel == null || _effectLabel == null || _maxLabel == null
			|| _buyButton == null || _background == null)
			return;

		_nameLabel.Text = $"{_entry.Name}   {_entry.Level}/{_entry.MaxLevel}";
		_effectLabel.Text = _entry.Effect;
		Color accent = AccentOf(_entry);
		_background.LightColor = StickerStyle.Light(accent);
		_background.DarkColor = StickerStyle.Dark(accent);
		_background.QueueRedraw();
		_buyButton.SetAccent(accent);

		_buyButton.Visible = !_entry.Maxed;
		_maxLabel.Visible = _entry.Maxed;
		if (_entry.Maxed)
		{
			_maxLabel.Text = Loc.T(L.UpgradeMax);
			return;
		}
		_buyButton.Text = Loc.T(L.UpgradeBuy, _entry.Price);
		_buyButton.Disabled = !_entry.Affordable;
		_buyButton.Modulate = _entry.Affordable ? Colors.White : new Color(1f, 1f, 1f, 0.5f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_entry == null)
			return;
		Texture2D? icon = GD.Load<Texture2D>(_entry.IconPath);
		if (icon == null)
			return;
		var half = new Vector2(IconSize * 0.5f, IconSize * 0.5f);
		Vector2 center = new(52f, CardHeight * 0.5f);
		DrawTextureRect(icon, new Rect2(center - half, new Vector2(IconSize, IconSize)), false);
	}

	private static Color AccentOf(UpgradeEntry entry) => entry.Maxed
		? new Color("ffd45e")
		: entry.Affordable ? new Color("7fd6f2") : new Color("9aa0a8");
}
