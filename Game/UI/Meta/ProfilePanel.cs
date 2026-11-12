using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Экран профиля игрока поверх главного меню: вкладки «Статистика» и «Достижения»
/// и кнопка «Назад». Общая ширина не даёт содержимому сжиматься в узкое окно.
/// </summary>
public partial class ProfilePanel : Control
{
	/// <summary>Ширина карточки профиля: строки статистики и сетка достижений.</summary>
	private const float ContentWidth = 470f;

	private Label? _titleLabel;
	private VBoxContainer? _content;
	private VBoxContainer? _statsBox;
	private Control? _achievementsBox;
	private StickerButton? _statsTab;
	private StickerButton? _achTab;
	private Label? _coins;
	private Label? _bestClassic;
	private Label? _bestSuika;
	private Label? _stock;
	private Label? _runs;
	private Label? _caught;
	private Label? _golden;
	private Label? _merges;
	private AchievementWall? _wall;
	private AchievementEntry[] _achievements = Array.Empty<AchievementEntry>();
	private Button? _backButton;
	private ProfileSnapshot _snapshot;
	private bool _showAchievements;

	/// <summary>Игрок закрыл экран профиля.</summary>
	public event Action? Closed;

	public float UiScale => 1f;

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

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 12);
		box.CustomMinimumSize = new Vector2(ContentWidth, 0f);
		ModalContent.Mount(this, box);

		float ui = UiScale;
		_titleLabel = UiStyle.MakeLabel(Loc.T(L.ProfileTitle), (int)Mathf.Round(34f * ui), new Color("ffd45e"));
		box.AddChild(_titleLabel);

		box.AddChild(BuildTabs());

		// Общая область контента фиксированной высоты: вкладки и «Назад» не смещаются.
		_content = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddChild(_content);

		_statsBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_statsBox.AddThemeConstantOverride("separation", 8);
		_content.AddChild(_statsBox);

		_bestClassic = MakeRow(_statsBox, ui);
		_bestSuika = MakeRow(_statsBox, ui);
		_stock = MakeRow(_statsBox, ui);
		_coins = MakeRow(_statsBox, ui);
		_runs = MakeRow(_statsBox, ui);
		_caught = MakeRow(_statsBox, ui);
		_golden = MakeRow(_statsBox, ui);
		_merges = MakeRow(_statsBox, ui);

		_achievementsBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_content.AddChild(_achievementsBox);
		_wall = new AchievementWall();
		_achievementsBox.AddChild(_wall);

		_backButton = UiStyle.MakeButton(Loc.T(L.ProfileBack), ui, new Color("b4e863"));
		_backButton.Pressed += Close;
		box.AddChild(_backButton);

		ShowTab(false);
		Visible = false;
		Loc.Changed += RefreshText;
	}

	public override void _ExitTree() => Loc.Changed -= RefreshText;

	/// <summary>Показывает профиль с переданными показателями и сеткой достижений.</summary>
	public void Show(ProfileSnapshot snapshot, AchievementEntry[] achievements)
	{
		_snapshot = snapshot;
		_achievements = achievements ?? Array.Empty<AchievementEntry>();
		_wall?.SetEntries(_achievements);
		// Высота области контента равна самой высокой вкладке — карточка не «прыгает».
		if (_content != null)
			_content.CustomMinimumSize = new Vector2(0f, AchievementWall.HeightFor(_achievements.Length));
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

	/// <summary>Ряд вкладок профиля: статистика и достижения.</summary>
	private Control BuildTabs()
	{
		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 14);

		_statsTab = (StickerButton)UiStyle.MakeButton(string.Empty, 0.72f, new Color("ffd45e"), new Vector2(186f, 52f));
		_statsTab.Pressed += () => ShowTab(false);
		row.AddChild(_statsTab);

		_achTab = (StickerButton)UiStyle.MakeButton(string.Empty, 0.72f, new Color("b4e863"), new Vector2(258f, 52f));
		_achTab.Pressed += () => ShowTab(true);
		row.AddChild(_achTab);

		return row;
	}

	/// <summary>Показывает вкладку статистики или достижений.</summary>
	private void ShowTab(bool achievements)
	{
		_showAchievements = achievements;
		if (_statsBox != null) _statsBox.Visible = !achievements;
		if (_achievementsBox != null) _achievementsBox.Visible = achievements;
		RefreshTabs();
	}

	/// <summary>Перечитывает подписи и подсветку вкладок.</summary>
	private void RefreshTabs()
	{
		int unlocked = 0;
		foreach (AchievementEntry entry in _achievements)
		{
			if (entry.Unlocked) unlocked++;
		}
		if (_statsTab != null)
		{
			_statsTab.Text = Loc.T(L.ProfileTabStats);
			_statsTab.SetAccent(_showAchievements ? new Color("b4e863") : new Color("ffd45e"));
		}
		if (_achTab != null)
		{
			_achTab.Text = $"{Loc.T(L.ProfileAchievements)} {Loc.T(L.AchievementsCount, unlocked, _achievements.Length)}";
			_achTab.SetAccent(_showAchievements ? new Color("ffd45e") : new Color("b4e863"));
		}
	}

	/// <summary>Перечитывает подписи и значения при смене языка и при показе.</summary>
	private void RefreshText()
	{
		if (_titleLabel != null) _titleLabel.Text = Loc.T(L.ProfileTitle);
		if (_bestClassic != null) _bestClassic.Text = Loc.T(L.ProfileBestClassic, _snapshot.BestClassic);
		if (_bestSuika != null) _bestSuika.Text = Loc.T(L.ProfileBestSuika, _snapshot.BestSuika);
		if (_stock != null) _stock.Text = Loc.T(L.ProfileStock, _snapshot.StockTotal);
		if (_coins != null) _coins.Text = Loc.T(L.SlimeLabel, _snapshot.Coins);
		if (_runs != null) _runs.Text = Loc.T(L.ProfileRuns, _snapshot.RunsPlayed);
		if (_caught != null) _caught.Text = Loc.T(L.ProfileCaught, _snapshot.FruitsCaught);
		if (_golden != null) _golden.Text = Loc.T(L.ProfileGolden, _snapshot.GoldenCaught);
		if (_merges != null) _merges.Text = Loc.T(L.ProfileMerges, _snapshot.SuikaMerges);
		RefreshTabs();
		_wall?.RefreshTiles();
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
