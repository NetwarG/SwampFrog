using Godot;

namespace SwampFrog;

/// <summary>
/// Список активных субперков для окна паузы: положительные и отрицательные
/// сгруппированы под своими заголовками, каждая запись — отдельной строкой.
/// </summary>
public partial class SubPerkListView : VBoxContainer
{
	/// <summary>Цвет нейтральных подписей: заголовка списка, подсказки и записей.</summary>
	private static readonly Color NeutralColor = new(1f, 1f, 1f, 0.82f);

	/// <summary>Цвет заголовка положительных субперков.</summary>
	private static readonly Color PositiveColor = new("b4e863");

	/// <summary>Цвет заголовка отрицательных субперков.</summary>
	private static readonly Color NegativeColor = new("ff8c7a");

	private SubPerkSummary[] _items = System.Array.Empty<SubPerkSummary>();

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		AddThemeConstantOverride("separation", 6);
		Loc.Changed += Rebuild;
	}

	public override void _ExitTree() => Loc.Changed -= Rebuild;

	/// <summary>Показывает список: пустой — подсказка, иначе группы по полярности.</summary>
	public void SetItems(SubPerkSummary[] items)
	{
		_items = items ?? System.Array.Empty<SubPerkSummary>();
		Rebuild();
	}

	/// <summary>Пересобирает строки: при показе окна и при смене языка.</summary>
	private void Rebuild()
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}

		if (_items.Length == 0)
		{
			AddRow(Loc.T(L.SubPerksNone), 20, NeutralColor, HorizontalAlignment.Center);
			return;
		}

		AddRow(Loc.T(L.SubPerksHeader), 20, NeutralColor, HorizontalAlignment.Center);
		AddGroup(L.SubPerksPositive, SubPerkPolarity.Positive, PositiveColor);
		AddGroup(L.SubPerksNegative, SubPerkPolarity.Negative, NegativeColor);
	}

	/// <summary>Заголовок группы и её записи. Группа без записей не показывается.</summary>
	private void AddGroup(string titleKey, SubPerkPolarity polarity, Color color)
	{
		if (!HasPolarity(polarity))
			return;

		AddRow(Loc.T(titleKey), 21, color, HorizontalAlignment.Center);
		foreach (SubPerkSummary item in _items)
		{
			if (item.Polarity != polarity)
				continue;
			AddRow(Loc.T(L.SubPerkEntry, item.Name, item.Level, item.Percent), 19, NeutralColor, HorizontalAlignment.Left);
		}
	}

	/// <summary>Есть ли среди активных субперков хотя бы один с такой полярностью.</summary>
	private bool HasPolarity(SubPerkPolarity polarity)
	{
		foreach (SubPerkSummary item in _items)
		{
			if (item.Polarity == polarity)
				return true;
		}
		return false;
	}

	private void AddRow(string text, int fontSize, Color color, HorizontalAlignment alignment)
	{
		Label label = UiStyle.MakeLabel(text, fontSize, color);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.HorizontalAlignment = alignment;
		AddChild(label);
	}
}