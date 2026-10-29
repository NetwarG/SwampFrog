using Godot;

namespace SwampFrog;

/// <summary>
/// Подсказка мини-игры: круглая кнопка «?» у правого верхнего края поля и полупрозрачное
/// модальное окно с правилами и цепочкой слияния фруктов.
/// Часть класса SuikaGame: см. SuikaGame.cs (раунд и ввод) и SuikaUi.cs (остальной интерфейс).
/// </summary>
public partial class SuikaGame
{
	/// <summary>Отступ кнопки «?» от верхнего края: под превью «Следующий».</summary>
	private const float HelpButtonTop = 100f;

	/// <summary>Ширина окна подсказки: строки правил переносятся по этой ширине.</summary>
	private const float HelpBoxWidth = 460f;

	private void BuildHelp(Control parent)
	{
		_helpButton = new HelpCircleButton { ZIndex = 35 };
		_helpButton.Pressed += OpenHelp;
		parent.AddChild(_helpButton);
		UpdateHelpButtonPosition();

		_helpRoot = new HelpShade { Name = "HelpDialog", Visible = false, ZIndex = 40 };
		_helpRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_helpRoot.Pressed += CloseHelp;
		parent.AddChild(_helpRoot);

		var box = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			CustomMinimumSize = new Vector2(HelpBoxWidth, 0f)
		};
		box.AddThemeConstantOverride("separation", 12);
		ModalContent.Mount(_helpRoot, box);

		box.AddChild(MakeLabel("Как играть", 40, new Color("ffd45e")));
		box.AddChild(MakeRuleLabel("Води палец по экрану — лягушка целится. Отпусти — фрукт летит в кувшин."));
		box.AddChild(MakeRuleLabel("Два одинаковых фрукта при касании сливаются в следующий фрукт."));
		box.AddChild(MakeRuleLabel("Фрукты берутся из запаса, собранного в классическом режиме."));
		box.AddChild(MakeRuleLabel("Не дай кувшину переполниться: линия переполнения — предел."));
		box.AddChild(MakeLabel("Цепочка слияния:", 20, new Color("b4e863")));
		box.AddChild(new FruitChainView { CustomMinimumSize = new Vector2(HelpBoxWidth, 58f) });

		Button close = MakeButton("Понятно", 22, new Color("b4e863"), new Vector2(300f, 54f));
		close.Pressed += CloseHelp;
		box.AddChild(close);
	}

	/// <summary>Строка правил: переносится по словам и выравнивается по левому краю окна.</summary>
	private Label MakeRuleLabel(string text)
	{
		Label label = MakeLabel(text, 20, new Color("ffffff"));
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.HorizontalAlignment = HorizontalAlignment.Left;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.CustomMinimumSize = new Vector2(HelpBoxWidth, 0f);
		return label;
	}

	private void OpenHelp()
	{
		if (_helpRoot == null || _helpOpen) return;
		_helpOpen = true;
		CancelInput();
		_helpRoot.Visible = true;
		UpdateHelpButton();
	}

	private void CloseHelp()
	{
		if (!_helpOpen) return;
		_helpOpen = false;
		if (_helpRoot != null) _helpRoot.Visible = false;
		UpdateHelpButton();
	}

	/// <summary>Кнопка «?» нужна только в идущем раунде: окна поверх поля её вытесняют.</summary>
	private void UpdateHelpButton()
	{
		if (_helpButton == null) return;
		_helpButton.Visible = _active && !_gameOver && !_helpOpen;
		UpdateHelpButtonPosition();
	}

	private void UpdateHelpButtonPosition()
	{
		if (_helpButton == null) return;
		_helpButton.ResetSize();
		_helpButton.Position = new Vector2(
			ViewSize.X - HelpCircleButton.MarginRight - HelpCircleButton.Diameter, HelpButtonTop);
	}
}
