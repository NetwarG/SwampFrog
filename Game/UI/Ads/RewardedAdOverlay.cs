using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Ролик рекламы с наградой: дом-ролик, который играет, пока рекламная сеть не подключена.
/// Держит счётчик просмотра и по его окончании сообщает о награде (см. docs/ADS.md).
/// </summary>
public partial class RewardedAdOverlay : Control
{
	/// <summary>Ширина панели ролика в логических единицах.</summary>
	private const float ContentWidth = 330f;

	/// <summary>Высота панели ролика: место под название игры и призыв.</summary>
	private const float PanelHeight = 120f;

	private Label? _titleLabel;
	private Label? _statusLabel;
	private AdBannerView? _panel;
	private Button? _closeButton;
	private float _left;
	private bool _granted;
	private int _shown = -1;

	/// <summary>Ролик досмотрен: награду начисляет вызывающая сторона.</summary>
	public event Action? Watched;

	public float UiScale => 1f;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.78f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(backdrop);
		backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 18);
		ModalContent.Mount(this, box);

		float ui = UiScale;
		_titleLabel = UiStyle.MakeLabel(Loc.T(L.MenuAdLabel), (int)Mathf.Round(44f * ui), new Color("ffd45e"));
		_panel = new AdBannerView
		{
			Name = "AdVideo",
			FillParent = false,
			CustomMinimumSize = new Vector2(ContentWidth * ui, PanelHeight * ui)
		};
		_statusLabel = UiStyle.MakeLabel(string.Empty, (int)Mathf.Round(26f * ui), new Color("cfe9c2"));
		_closeButton = UiStyle.MakeButton(Loc.T(L.AdRewardClose), ui, new Color("ffd45e"));
		_closeButton.Pressed += HideAd;

		box.AddChild(_titleLabel);
		box.AddChild(_panel);
		box.AddChild(_statusLabel);
		box.AddChild(_closeButton);

		Visible = false;
		Loc.Changed += RefreshText;
	}

	public override void _ExitTree() => Loc.Changed -= RefreshText;

	/// <summary>Запускает ролик: счётчик просмотра начинается заново.</summary>
	public void ShowAd()
	{
		_left = Mathf.Max(1f, AdConfig.RewardAdSeconds);
		_granted = false;
		_shown = -1;
		Visible = true;
		RefreshText();
	}

	/// <summary>Скрывает ролик: закрытый раньше времени награды не даёт.</summary>
	public void HideAd() => Visible = false;

	public override void _Process(double delta)
	{
		if (!Visible || _granted)
			return;
		_left -= (float)delta;
		if (_left <= 0f)
		{
			_granted = true;
			Watched?.Invoke();
			RefreshText();
			return;
		}
		// Подпись счётчика обновляется только при смене целых секунд.
		int seconds = Mathf.CeilToInt(_left);
		if (seconds == _shown)
			return;
		_shown = seconds;
		RefreshText();
	}

	/// <summary>Подписи ролика: счётчик просмотра, награда и кнопка закрытия.</summary>
	private void RefreshText()
	{
		if (_titleLabel != null) _titleLabel.Text = Loc.T(L.MenuAdLabel);
		if (_closeButton != null) _closeButton.Text = Loc.T(L.AdRewardClose);
		if (_statusLabel == null)
			return;
		_statusLabel.Text = _granted
			? Loc.T(L.AdRewardDone, SlimeReward.AdWatched)
			: Loc.T(L.AdRewardWait, Mathf.Max(0, Mathf.CeilToInt(_left)));
	}
}