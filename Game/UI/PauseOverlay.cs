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
	private Label? _statsLabel;

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
		Button resume = UiStyle.MakeButton("Продолжить", ui, new Color("b4e863"));
		Button menu = UiStyle.MakeButton("В меню", ui, new Color("ffd45e"));
		resume.Pressed += () => ResumePressed?.Invoke();
		menu.Pressed += () => MenuPressed?.Invoke();

		box.AddChild(title);
		box.AddChild(_statsLabel);
		box.AddChild(resume);
		box.AddChild(menu);

		Visible = false;
	}

	public float UiScale => 1f;

	/// <summary>Показывает оверлей с показателями партии, на которой сделана пауза.</summary>
	public void Show(int score, int lives)
	{
		if (_statsLabel != null)
		{
			_statsLabel.Text = $"Счёт: {score}    HP: {lives}";
		}
		Visible = true;
	}

	public void Hide() => Visible = false;
}
