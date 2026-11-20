using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Экран постоянных улучшений: прокручиваемый список карточек, баланс слизи и кнопка «Назад».
/// Уровни и цены берутся из каталога, поэтому новый пункт добавляется без правки интерфейса.
/// </summary>
public partial class UpgradePanel : Control
{
	/// <summary>Сколько карточек видно в списке без прокрутки.</summary>
	private const int VisibleCards = 5;

	/// <summary>Зазор между карточками списка.</summary>
	private const int ListSeparation = 10;

	/// <summary>Запас ширины под вертикальную полосу прокрутки.</summary>
	private const float ScrollbarAllowance = 16f;

	private VBoxContainer? _box;
	private VBoxContainer? _list;
	private ScrollContainer? _scroll;
	private Label? _titleLabel;
	private Label? _coinsLabel;
	private Button? _backButton;
	private UpgradeEntry[] _entries = Array.Empty<UpgradeEntry>();
	private int _coins;

	/// <summary>Игрок запросил покупку уровня улучшения.</summary>
	public event Action<UpgradeId>? UpgradePurchased;

	/// <summary>Экран закрыт кнопкой «Назад».</summary>
	public event Action? Closed;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.92f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(backdrop);
		backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_box.AddThemeConstantOverride("separation", 10);
		// Отступ чуть меньше стандартного: освобождаем место под полосу прокрутки списка.
		ModalContent.Mount(this, _box, padding: 16);

		BuildHeader();
		BuildScroll();
		BuildBack();

		Visible = false;
		Loc.Changed += Rebuild;
	}

	public override void _ExitTree() => Loc.Changed -= Rebuild;

	/// <summary>Показывает экран: пункты улучшений и текущий баланс слизи.</summary>
	public void Show(UpgradeEntry[] entries, int coins)
	{
		_entries = entries ?? Array.Empty<UpgradeEntry>();
		_coins = coins;
		Rebuild();
		Visible = true;
	}

	/// <summary>Скрывает экран.</summary>
	public void Hide() => Visible = false;

	private void Close()
	{
		Hide();
		Closed?.Invoke();
	}

	/// <summary>Шапка экрана: название и баланс слизи на ширине карточек.</summary>
	private void BuildHeader()
	{
		var header = new HBoxContainer
		{
			Name = "UpgradeHeader",
			CustomMinimumSize = new Vector2(UpgradeCard.CardWidth, 0f),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter
		};
		header.AddThemeConstantOverride("separation", 20);
		_titleLabel = UiStyle.MakeLabel(Loc.T(L.UpgradesTitle), 38, new Color("ffd45e"));
		header.AddChild(_titleLabel);
		header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		_coinsLabel = UiStyle.MakeLabel(Loc.T(L.SlimeLabel, 0), 24, new Color("b4e863"));
		header.AddChild(_coinsLabel);
		_box?.AddChild(header);
	}

	/// <summary>
	/// Прокручиваемый список карточек. Каркас живёт постоянно: пересборка карточек
	/// после покупки или смены языка не сбрасывает позицию прокрутки.
	/// </summary>
	private void BuildScroll()
	{
		_scroll = new TouchScrollContainer
		{
			Name = "UpgradeScroll",
			CustomMinimumSize = new Vector2(UpgradeCard.CardWidth + ScrollbarAllowance, 0f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
			MouseFilter = MouseFilterEnum.Stop,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter
		};
		_box?.AddChild(_scroll);

		_list = new VBoxContainer { Name = "UpgradeList", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_list.AddThemeConstantOverride("separation", ListSeparation);
		_scroll.AddChild(_list);
	}

	/// <summary>Кнопка «Назад» под списком.</summary>
	private void BuildBack()
	{
		var backRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_backButton = UiStyle.MakeButton(Loc.T(L.UpgradesBack), 0.9f, new Color("b4e863"), new Vector2(220f, 60f));
		_backButton.Pressed += Close;
		backRow.AddChild(_backButton);
		_box?.AddChild(backRow);
	}

	/// <summary>Обновляет тексты и пересобирает карточки, сохраняя прокрутку списка.</summary>
	private void Rebuild()
	{
		if (_list == null || _scroll == null)
			return;
		if (_titleLabel != null) _titleLabel.Text = Loc.T(L.UpgradesTitle);
		if (_coinsLabel != null) _coinsLabel.Text = Loc.T(L.SlimeLabel, _coins);
		if (_backButton != null) _backButton.Text = Loc.T(L.UpgradesBack);

		foreach (Node child in _list.GetChildren())
		{
			_list.RemoveChild(child);
			child.QueueFree();
		}
		foreach (UpgradeEntry entry in _entries)
		{
			var card = new UpgradeCard { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
			card.Setup(entry);
			card.PurchaseRequested += id => UpgradePurchased?.Invoke(id);
			_list.AddChild(card);
		}

		int visible = Mathf.Clamp(_entries.Length, 1, VisibleCards);
		float height = visible * UpgradeCard.CardHeight + Mathf.Max(0, visible - 1) * ListSeparation;
		_scroll.CustomMinimumSize = new Vector2(UpgradeCard.CardWidth + ScrollbarAllowance, height);
	}
}
