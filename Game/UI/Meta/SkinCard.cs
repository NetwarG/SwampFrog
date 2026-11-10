using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Карточка скина в галерее: превью варианта, имя и действие.
/// Действие — «Выбрать», «Купить: N» или подпись условия, если скин закрыт.
/// </summary>
public partial class SkinCard : Control
{
	public const float CardWidth = 96f;
	public const float CardHeight = 132f;
	private const float PreviewHeight = 62f;

	private SkinGalleryEntry? _entry;
	private StickerBackground? _background;
	private Label? _nameLabel;
	private Label? _stateLabel;
	private StickerButton? _actionButton;

	/// <summary>Игрок выбрал доступный скин.</summary>
	public event Action<SkinId>? Selected;

	/// <summary>Игрок нажал «Купить».</summary>
	public event Action<SkinId>? PurchaseRequested;

	/// <summary>Задаёт скин карточки; вызывается до добавления в дерево.</summary>
	public void Setup(SkinGalleryEntry entry)
	{
		_entry = entry;
		if (_background != null)
			Refresh();
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Pass;
		CustomMinimumSize = new Vector2(CardWidth, CardHeight);
		Size = new Vector2(CardWidth, CardHeight);

		_background = new StickerBackground { CornerRadius = 12f };
		AddChild(_background);

		_nameLabel = UiStyle.MakeLabel(string.Empty, 13, new Color("173c34"));
		_nameLabel.Position = new Vector2(0f, PreviewHeight + 2f);
		_nameLabel.Size = new Vector2(CardWidth, 16f);
		AddChild(_nameLabel);

		_stateLabel = UiStyle.MakeLabel(string.Empty, 11, new Color("3a2a12"));
		_stateLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_stateLabel.Position = new Vector2(4f, PreviewHeight + 22f);
		_stateLabel.Size = new Vector2(CardWidth - 8f, 42f);
		AddChild(_stateLabel);

		_actionButton = (StickerButton)UiStyle.MakeButton(string.Empty, 0.62f, new Color("b4e863"), new Vector2(CardWidth - 14f, 34f));
		_actionButton.Position = new Vector2(7f, PreviewHeight + 22f);
		_actionButton.Pressed += OnAction;
		AddChild(_actionButton);

		Resized += QueueRedraw;
		Refresh();
	}

	public override void _ExitTree()
	{
		if (_actionButton != null) _actionButton.Pressed -= OnAction;
	}

	private void OnAction()
	{
		if (_entry == null)
			return;
		switch (_entry.State)
		{
			case SkinCardState.Owned:
				Selected?.Invoke(_entry.Id);
				break;
			case SkinCardState.Buyable:
				PurchaseRequested?.Invoke(_entry.Id);
				break;
		}
	}

	/// <summary>Перечитывает подписи, цвет и доступность действия по состоянию скина.</summary>
	private void Refresh()
	{
		if (_entry == null || _nameLabel == null || _stateLabel == null || _actionButton == null || _background == null)
			return;
		_nameLabel.Text = _entry.Name;
		Color accent = AccentOf(_entry.State);
		_background.LightColor = StickerStyle.Light(accent);
		_background.DarkColor = StickerStyle.Dark(accent);
		_background.QueueRedraw();
		_actionButton.SetAccent(accent);

		bool showButton = _entry.State == SkinCardState.Owned
			|| (_entry.State == SkinCardState.Buyable && _entry.Affordable);
		_actionButton.Visible = showButton;
		_stateLabel.Visible = !showButton;

		switch (_entry.State)
		{
			case SkinCardState.Selected:
				_stateLabel.Text = Loc.T(L.SkinSelected);
				break;
			case SkinCardState.Buyable:
				_stateLabel.Text = _entry.Affordable ? string.Empty : Loc.T(L.SkinBuy, _entry.Price);
				break;
			case SkinCardState.Locked:
				_stateLabel.Text = _entry.RequiredSuika
					? Loc.T(L.SkinLockedSuika, _entry.RequiredScore)
					: Loc.T(L.SkinLockedScore, _entry.RequiredScore);
				break;
		}
		if (_entry.State == SkinCardState.Buyable && _entry.Affordable)
			_actionButton.Text = Loc.T(L.SkinBuy, _entry.Price);
		else if (_entry.State == SkinCardState.Owned)
			_actionButton.Text = Loc.T(L.SkinSelect);

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_entry == null)
			return;
		Vector2 center = new(CardWidth * 0.5f, PreviewHeight * 0.62f);
		Texture2D? icon = SkinCatalog.Icon(_entry.Id);
		if (icon == null)
		{
			DrawNone(center);
			return;
		}
		const float size = 54f;
		var half = new Vector2(size * 0.5f, size * 0.5f);
		DrawTextureRect(icon, new Rect2(center - half, new Vector2(size, size)), false);
	}

	private void DrawNone(Vector2 center) =>
		DrawArc(center, 18f, 0f, Mathf.Tau, 24, new Color(1f, 1f, 1f, 0.3f), 2f);

	private static Color AccentOf(SkinCardState state) => state switch
	{
		SkinCardState.Selected => new Color("ffd45e"),
		SkinCardState.Owned => new Color("b4e863"),
		SkinCardState.Buyable => new Color("7fd6f2"),
		_ => new Color("9aa0a8"),
	};
}
