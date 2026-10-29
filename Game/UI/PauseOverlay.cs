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
		Label title = UiStyle.MakeLabel("Пауза", (int)Mathf.Round(52f * ui), new Color("ffd45e"));
		_statsLabel = UiStyle.MakeLabel(string.Empty, (int)Mathf.Round(28f * ui), new Color(1f, 1f, 1f, 0.88f));
		_subPerkLabel = MakeSubPerkLabel(ui);
		Button resume = UiStyle.MakeButton("Продолжить", ui, new Color("b4e863"));
		Button menu = UiStyle.MakeButton("В меню", ui, new Color("ffd45e"));
		resume.Pressed += () => ResumePressed?.Invoke();
		menu.Pressed += () => MenuPressed?.Invoke();

		box.AddChild(title);
		box.AddChild(_statsLabel);
		box.AddChild(_subPerkLabel);
		box.AddChild(resume);
		box.AddChild(menu);

		Visible = false;
	}

	public float UiScale => 1f;

	/// <summary>Показывает оверлей с показателями партии и списком активных субперков.</summary>
	public void Show(int score, int lives, SubPerkSummary[] subPerks)
	{
		if (_statsLabel != null)
		{
			_statsLabel.Text = $"Счёт: {score}    HP: {lives}";
		}
		if (_subPerkLabel != null)
		{
			_subPerkLabel.Text = FormatSubPerks(subPerks);
		}
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
			return "Субперков пока нет";

		var parts = new System.Collections.Generic.List<string>();
		foreach (SubPerkSummary perk in subPerks)
		{
			parts.Add($"{perk.Name} ур.{perk.Level} ({perk.Percent}%)");
		}
		return "Субперки:\n" + string.Join(" · ", parts);
	}
}
