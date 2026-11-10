using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Экран постоянных улучшений: список карточек с балансом слизи и кнопкой «Назад».
/// Уровни и цены берутся из каталога, поэтому новый пункт добавляется без правки интерфейса.
/// </summary>
public partial class UpgradePanel : Control
{
	private VBoxContainer? _box;
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
		ModalContent.Mount(this, _box);
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

	private void Rebuild()
	{
		if (_box == null)
			return;
		foreach (Node child in _box.GetChildren())
		{
			_box.RemoveChild(child);
			child.QueueFree();
		}

		var header = new HBoxContainer { CustomMinimumSize = new Vector2(UpgradeCard.CardWidth, 0f) };
		header.AddThemeConstantOverride("separation", 20);
		header.AddChild(UiStyle.MakeLabel(Loc.T(L.UpgradesTitle), 38, new Color("ffd45e")));
		header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		header.AddChild(UiStyle.MakeLabel(Loc.T(L.SlimeLabel, _coins), 24, new Color("b4e863")));
		_box.AddChild(header);

		foreach (UpgradeEntry entry in _entries)
		{
			var card = new UpgradeCard();
			card.Setup(entry);
			card.PurchaseRequested += id => UpgradePurchased?.Invoke(id);
			_box.AddChild(card);
		}

		var backRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		Button back = UiStyle.MakeButton(Loc.T(L.UpgradesBack), 0.9f, new Color("b4e863"), new Vector2(220f, 60f));
		back.Pressed += Close;
		backRow.AddChild(back);
		_box.AddChild(backRow);
	}
}
