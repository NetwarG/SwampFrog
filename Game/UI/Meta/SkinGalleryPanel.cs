using System;
using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Магазин скинов: вкладки слотов сверху и крупная горизонтальная карусель карточек
/// (стрелки и свайп). Активная вкладка сохраняется при обновлении после покупки,
/// а лента строится из каталога — новые скины не требуют правки интерфейса.
/// </summary>
public partial class SkinGalleryPanel : Control
{
	private const float StripWidth = 480f;
	private const float ArrowSize = 44f;
	private const float TabWidth = 112f;
	private const float TabHeight = 46f;

	private readonly Dictionary<SkinSlot, Button> _tabs = new();

	private VBoxContainer? _box;
	private SkinStrip? _strip;
	private Button? _leftArrow;
	private Button? _rightArrow;
	private SkinGalleryEntry[] _entries = Array.Empty<SkinGalleryEntry>();
	private SkinSlot _slot = SkinSlot.Hat;
	private int _coins;

	/// <summary>Игрок выбрал доступный скин.</summary>
	public event Action<SkinId>? SkinSelected;

	/// <summary>Игрок запросил покупку скина.</summary>
	public event Action<SkinId>? SkinPurchased;

	/// <summary>Магазин закрыт кнопкой «Назад».</summary>
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
		_box.AddThemeConstantOverride("separation", 12);
		ModalContent.Mount(this, _box);
		Visible = false;
		Loc.Changed += Rebuild;
	}

	public override void _ExitTree() => Loc.Changed -= Rebuild;

	/// <summary>Показывает магазин: пункты каталога и текущий баланс слизи.</summary>
	public void Show(SkinGalleryEntry[] entries, int coins)
	{
		_entries = entries ?? Array.Empty<SkinGalleryEntry>();
		_coins = coins;
		Rebuild();
		Visible = true;
	}

	/// <summary>Скрывает магазин.</summary>
	public void Hide() => Visible = false;

	private void Close()
	{
		Hide();
		Closed?.Invoke();
	}

	/// <summary>Собирает экран: заголовок с балансом, вкладки, карусель и «Назад».</summary>
	private void Rebuild()
	{
		if (_box == null)
			return;
		foreach (Node child in _box.GetChildren())
		{
			_box.RemoveChild(child);
			child.QueueFree();
		}
		_tabs.Clear();

		var header = new HBoxContainer { CustomMinimumSize = new Vector2(StripWidth, 0f) };
		header.AddThemeConstantOverride("separation", 20);
		header.AddChild(UiStyle.MakeLabel(Loc.T(L.SkinsTitle), 38, new Color("ffd45e")));
		header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		header.AddChild(UiStyle.MakeLabel(Loc.T(L.SlimeLabel, _coins), 24, new Color("b4e863")));
		_box.AddChild(header);

		_box.AddChild(BuildTabRow());

		_strip = new SkinStrip { CustomMinimumSize = new Vector2(StripWidth, SkinCard.CardHeight) };
		_strip.SkinSelected += id => SkinSelected?.Invoke(id);
		_strip.SkinPurchased += id => SkinPurchased?.Invoke(id);
		_strip.Changed += UpdateArrows;
		_box.AddChild(_strip);

		var backRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		Button back = UiStyle.MakeButton(Loc.T(L.SkinsBack), 0.9f, new Color("b4e863"), new Vector2(220f, 60f));
		back.Pressed += Close;
		backRow.AddChild(back);
		_box.AddChild(backRow);

		ApplySlot();
	}

	/// <summary>Строка вкладок слотов слева и стрелок прокрутки справа.</summary>
	private HBoxContainer BuildTabRow()
	{
		var row = new HBoxContainer { CustomMinimumSize = new Vector2(StripWidth, 0f) };
		row.AddThemeConstantOverride("separation", 10);

		var tabs = new HBoxContainer();
		tabs.AddThemeConstantOverride("separation", 10);
		tabs.AddChild(MakeTab(SkinSlot.Hat, L.SkinSlotHat));
		tabs.AddChild(MakeTab(SkinSlot.Gloves, L.SkinSlotGloves));
		tabs.AddChild(MakeTab(SkinSlot.Stand, L.SkinSlotStand));
		row.AddChild(tabs);

		row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

		var arrows = new HBoxContainer();
		arrows.AddThemeConstantOverride("separation", 8);
		_leftArrow = MakeArrow("\u2039", -1);
		_rightArrow = MakeArrow("\u203a", 1);
		arrows.AddChild(_leftArrow);
		arrows.AddChild(_rightArrow);
		row.AddChild(arrows);
		return row;
	}

	private Button MakeTab(SkinSlot slot, string titleKey)
	{
		Button tab = UiStyle.MakeButton(Loc.T(titleKey), 0.72f, TabAccent(slot), new Vector2(TabWidth, TabHeight));
		tab.Pressed += () =>
		{
			_slot = slot;
			ApplySlot();
		};
		_tabs[slot] = tab;
		return tab;
	}

	private Button MakeArrow(string glyph, int direction)
	{
		Button arrow = UiStyle.MakeButton(glyph, 1f, new Color("7fd6f2"), new Vector2(ArrowSize, ArrowSize));
		arrow.Pressed += () => _strip?.PageBy(direction);
		return arrow;
	}

	private Color TabAccent(SkinSlot slot) => slot == _slot ? new Color("ffd45e") : new Color("b4e863");

	/// <summary>Показывает карточки активного слота и подсвечивает выбранную вкладку.</summary>
	private void ApplySlot()
	{
		if (_strip != null)
		{
			var list = new List<SkinGalleryEntry>();
			foreach (SkinGalleryEntry entry in _entries)
			{
				if (entry.Slot == _slot)
					list.Add(entry);
			}
			_strip.SetEntries(list.ToArray());
		}
		foreach (KeyValuePair<SkinSlot, Button> tab in _tabs)
		{
			if (tab.Value is StickerButton sticker)
				sticker.SetAccent(TabAccent(tab.Key));
		}
		UpdateArrows();
	}

	/// <summary>Стрелки активны только когда лента действительно может сдвинуться.</summary>
	private void UpdateArrows()
	{
		SetArrowState(_leftArrow, _strip != null && _strip.CanPageLeft);
		SetArrowState(_rightArrow, _strip != null && _strip.CanPageRight);
	}

	private static void SetArrowState(Button? arrow, bool enabled)
	{
		if (arrow == null)
			return;
		arrow.Disabled = !enabled;
		arrow.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.4f);
	}
}
