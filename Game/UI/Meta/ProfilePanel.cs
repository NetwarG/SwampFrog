using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Экран профиля игрока: постоянная статистика поверх главного меню.
/// Затемняет меню, показывает показатели и закрывается кнопкой «Назад».
/// </summary>
public partial class ProfilePanel : Control
{
	/// <summary>Ширина строк показателей: широкие строки не сжимаются в узкое окно.</summary>
	private const float ContentWidth = 390f;

	private Label? _titleLabel;
	private Label? _bestClassic;
	private Label? _bestSuika;
	private Label? _stock;
	private Label? _runs;
	private Label? _caught;
	private Label? _golden;
	private Label? _merges;
	private Button? _backButton;
	private ProfileSnapshot _snapshot;

	/// <summary>Игрок закрыл экран профиля.</summary>
	public event Action? Closed;

	public float UiScale => 1f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.85f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(backdrop);
		backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 12);
		ModalContent.Mount(this, box);

		float ui = UiScale;
		_titleLabel = UiStyle.MakeLabel(Loc.T(L.ProfileTitle), (int)Mathf.Round(46f * ui), new Color("ffd45e"));
		box.AddChild(_titleLabel);

		_bestClassic = MakeRow(box, ui);
		_bestSuika = MakeRow(box, ui);
		_stock = MakeRow(box, ui);
		_runs = MakeRow(box, ui);
		_caught = MakeRow(box, ui);
		_golden = MakeRow(box, ui);
		_merges = MakeRow(box, ui);

		_backButton = UiStyle.MakeButton(Loc.T(L.ProfileBack), ui, new Color("b4e863"));
		_backButton.Pressed += Close;
		box.AddChild(_backButton);

		Visible = false;
		Loc.Changed += RefreshText;
	}

	public override void _ExitTree() => Loc.Changed -= RefreshText;

	/// <summary>Показывает профиль с переданными показателями.</summary>
	public void Show(ProfileSnapshot snapshot)
	{
		_snapshot = snapshot;
		RefreshText();
		Visible = true;
	}

	/// <summary>Скрывает экран профиля.</summary>
	public void Hide() => Visible = false;

	private void Close()
	{
		Hide();
		Closed?.Invoke();
	}

	/// <summary>Перечитывает подписи и значения при смене языка и при показе.</summary>
	private void RefreshText()
	{
		if (_titleLabel != null) _titleLabel.Text = Loc.T(L.ProfileTitle);
		if (_bestClassic != null) _bestClassic.Text = Loc.T(L.ProfileBestClassic, _snapshot.BestClassic);
		if (_bestSuika != null) _bestSuika.Text = Loc.T(L.ProfileBestSuika, _snapshot.BestSuika);
		if (_stock != null) _stock.Text = Loc.T(L.ProfileStock, _snapshot.StockTotal);
		if (_runs != null) _runs.Text = Loc.T(L.ProfileRuns, _snapshot.RunsPlayed);
		if (_caught != null) _caught.Text = Loc.T(L.ProfileCaught, _snapshot.FruitsCaught);
		if (_golden != null) _golden.Text = Loc.T(L.ProfileGolden, _snapshot.GoldenCaught);
		if (_merges != null) _merges.Text = Loc.T(L.ProfileMerges, _snapshot.SuikaMerges);
		if (_backButton != null) _backButton.Text = Loc.T(L.ProfileBack);
	}

	/// <summary>Строка показателя: одна метка по центру, текст задаётся при обновлении.</summary>
	private static Label MakeRow(Control box, float ui)
	{
		var label = UiStyle.MakeLabel(string.Empty, (int)Mathf.Round(22f * ui), new Color(1f, 1f, 1f, 0.92f));
		label.CustomMinimumSize = new Vector2(ContentWidth * ui, 0f);
		box.AddChild(label);
		return label;
	}
}
