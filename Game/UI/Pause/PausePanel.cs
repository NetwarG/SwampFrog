using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Выдвижная панель паузы: скруглённый слева полупрозрачный прямоугольник, который
/// выезжает из-за правого края экрана и содержит круглую кнопку паузы.
/// </summary>
public partial class PausePanel : TouchArea
{
	private static readonly Vector2 PanelSize = new(112f, 96f);
	private const float SlideDuration = 0.16f;
	private const float CornerRadius = 16f;

	/// <summary>Тёмная панель в стиле кнопок: тот же коричневый контур, но без блика.</summary>
	private static readonly Color PanelLight = new(0.08f, 0.24f, 0.19f, 0.88f);
	private static readonly Color PanelDark = new(0.02f, 0.1f, 0.08f, 0.88f);

	private PauseCircleButton? _button;
	private Tween? _slide;
	private bool _open;

	/// <summary>Игрок нажал круглую кнопку паузы.</summary>
	public event Action? PausePressed;

	/// <summary>Игрок нажал фон панели: панель нужно свернуть.</summary>
	public event Action? BackgroundPressed;

	/// <summary>Панель открыта: кнопка с точками в это время скрыта.</summary>
	public bool IsOpen => _open;

	public override void _Ready()
	{
		CustomMinimumSize = PanelSize;
		Size = PanelSize;
		Visible = false;

		Pressed += () => BackgroundPressed?.Invoke();

		_button = new PauseCircleButton();
		AddChild(_button);
		_button.Pressed += () => PausePressed?.Invoke();
		UpdateButtonPosition();
		Resized += UpdateButtonPosition;
	}

	/// <summary>Выезжает из-за правого края экрана.</summary>
	public void SlideIn(float rightEdge)
	{
		_open = true;
		Visible = true;
		Slide(rightEdge - Size.X);
	}

	/// <summary>Уезжает за правый край экрана и прячется.</summary>
	public void SlideOut(float rightEdge)
	{
		_open = false;
		Slide(rightEdge);
	}

	private void Slide(float targetX)
	{
		_slide?.Kill();
		_slide = CreateTween();
		_slide.TweenProperty(this, "position:x", targetX, SlideDuration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		_slide.Finished += OnSlideFinished;
	}

	private void OnSlideFinished()
	{
		if (!_open) Visible = false;
	}

	/// <summary>Скруглены только левые углы: правый край панели совпадает с краем экрана.</summary>
	public override void _Draw() =>
		StickerStyle.DrawPanel(this, new Rect2(Vector2.Zero, Size), 0f, 0f, CornerRadius, CornerRadius,
			PanelLight, PanelDark, StickerStyle.OutlineWidth, false);

	private void UpdateButtonPosition()
	{
		if (_button == null) return;
		_button.ResetSize();
		_button.Position = (Size - _button.Size) * 0.5f;
	}
}
