using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Экран «Скины»: три группы лягушки (шляпа, перчатки, подставка) с карточками.
/// Открывается из профиля; покупка и выбор уходят в Main через события.
/// </summary>
public partial class SkinGalleryPanel : Control
{
	private const float Gap = 6f;

	private VBoxContainer? _box;
	private SkinGalleryEntry[] _entries = Array.Empty<SkinGalleryEntry>();

	/// <summary>Игрок выбрал доступный скин.</summary>
	public event Action<SkinId>? SkinSelected;

	/// <summary>Игрок запросил покупку скина.</summary>
	public event Action<SkinId>? SkinPurchased;

	/// <summary>Экран закрыт кнопкой «Назад».</summary>
	public event Action? Closed;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.9f),
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

	/// <summary>Показывает галерею с готовыми пунктами.</summary>
	public void Show(SkinGalleryEntry[] entries)
	{
		_entries = entries ?? Array.Empty<SkinGalleryEntry>();
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

	/// <summary>Пересобирает содержимое: вызывается при показе и при смене языка.</summary>
	private void Rebuild()
	{
		if (_box == null) return;
		foreach (Node child in _box.GetChildren())
		{
			_box.RemoveChild(child);
			child.QueueFree();
		}
		_box.AddChild(UiStyle.MakeLabel(Loc.T(L.SkinsTitle), 38, new Color("ffd45e")));
		AddGroup(SkinSlot.Hat, L.SkinSlotHat);
		AddGroup(SkinSlot.Gloves, L.SkinSlotGloves);
		AddGroup(SkinSlot.Stand, L.SkinSlotStand);

		var backRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		Button back = UiStyle.MakeButton(Loc.T(L.SkinsBack), 0.9f, new Color("b4e863"), new Vector2(220f, 60f));
		back.Pressed += Close;
		backRow.AddChild(back);
		_box.AddChild(backRow);
	}

	private void AddGroup(SkinSlot slot, string titleKey)
	{
		if (_box == null) return;
		_box.AddChild(UiStyle.MakeLabel(Loc.T(titleKey), 20, new Color(1f, 1f, 1f, 0.92f)));
		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", (int)Gap);
		foreach (SkinGalleryEntry entry in _entries)
		{
			if (entry.Slot != slot) continue;
			var card = new SkinCard();
			card.Setup(entry);
			card.Selected += id => SkinSelected?.Invoke(id);
			card.PurchaseRequested += id => SkinPurchased?.Invoke(id);
			row.AddChild(card);
		}
		_box.AddChild(row);
	}
}
