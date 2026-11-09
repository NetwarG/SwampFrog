using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Оверлей паузы: показывается поверх HUD, когда игра остановлена
/// (SceneTree.Paused). Благодаря ProcessMode.Always кнопки продолжают
/// работать на паузе — иначе играть было бы нечем её снять.
/// </summary>
public partial class PauseOverlay : Control
{
	private const float ContentWidth = 330f;

	private Label? _statsLabel;
	private Label? _subPerkLabel;
	private Label? _titleLabel;
	private Button? _resumeButton;
	private Button? _menuButton;
	private int _score;
	private int _lives;
	private SubPerkSummary[] _subPerks = System.Array.Empty<SubPerkSummary>();

	public event Action? ResumePressed;
	public event Action? MenuPressed;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var backdrop = new ColorRect
		{
			Color = new Color(0.02f, 0.12f, 0.09f, 0.72f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(backdrop);
		backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 16);
		ModalContent.Mount(this, box);

		float ui = UiScale;
		_titleLabel = UiStyle.MakeLabel(Loc.T(L.PauseTitle), (int)Mathf.Round(52f * ui), new Color("ffd45e"));
		_statsLabel = UiStyle.MakeLabel(string.Empty, (int)Mathf.Round(28f * ui), new Color(1f, 1f, 1f, 0.88f));
		_subPerkLabel = MakeSubPerkLabel(ui);
		_resumeButton = UiStyle.MakeButton(Loc.T(L.Resume), ui, new Color("b4e863"));
		_menuButton = UiStyle.MakeButton(Loc.T(L.ToMenu), ui, new Color("ffd45e"));
		_resumeButton.Pressed += () => ResumePressed?.Invoke();
		_menuButton.Pressed += () => MenuPressed?.Invoke();

		box.AddChild(_titleLabel);
		box.AddChild(_statsLabel);
		box.AddChild(_subPerkLabel);
		box.AddChild(_resumeButton);
		box.AddChild(_menuButton);

		Visible = false;
		Loc.Changed += Localize;
	}

	public override void _ExitTree() => Loc.Changed -= Localize;

	/// <summary>Перечитывает подписи окна при смене языка.</summary>
	private void Localize()
	{
		if (_titleLabel != null) _titleLabel.Text = Loc.T(L.PauseTitle);
		if (_resumeButton != null) _resumeButton.Text = Loc.T(L.Resume);
		if (_menuButton != null) _menuButton.Text = Loc.T(L.ToMenu);
		RefreshText();
	}

	/// <summary>Обновляет показатели партии и список субперков на текущем языке.</summary>
	private void RefreshText()
	{
		if (_statsLabel != null)
		{
			_statsLabel.Text = Loc.T(L.PauseStats, _score, _lives);
		}
		if (_subPerkLabel != null)
		{
			_subPerkLabel.Text = FormatSubPerks(_subPerks);
		}
	}

	public float UiScale => 1f;

	/// <summary>Показывает оверлей с показателями партии и списком активных субперков.</summary>
	public void Show(int score, int lives, SubPerkSummary[] subPerks)
	{
		_score = score;
		_lives = lives;
		_subPerks = subPerks ?? System.Array.Empty<SubPerkSummary>();
		RefreshText();
		Visible = true;
	}

	public void Hide() => Visible = false;

	/// <summary>Панель субперков без бара: список активных субперков с уровнями и процентами.</summary>
	private static Label MakeSubPerkLabel(float ui)
	{
		var label = UiStyle.MakeLabel(string.Empty, (int)Mathf.Round(20f * ui), new Color(1f, 1f, 1f, 0.82f));
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.CustomMinimumSize = new Vector2(ContentWidth * ui, 0f);
		return label;
	}

	private static string FormatSubPerks(SubPerkSummary[] subPerks)
	{
		if (subPerks == null || subPerks.Length == 0)
			return Loc.T(L.SubPerksNone);

		var parts = new System.Collections.Generic.List<string>();
		foreach (SubPerkSummary perk in subPerks)
		{
			parts.Add(Loc.T(L.SubPerkEntry, perk.Name, perk.Level, perk.Percent));
		}
		return Loc.T(L.SubPerksHeader) + "\n" + string.Join(" · ", parts);
	}
}
