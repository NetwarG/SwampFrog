using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Выдвижная панель паузы: скруглённый слева полупрозрачный прямоугольник, который
/// выезжает из-за правого края экрана и содержит круглую кнопку паузы.
/// </summary>
public partial class PausePanel : TouchArea
{
	private static readonly Vector2 PanelSize = new(104f, 88f);
	private const float SlideDuration = 0.16f;
	private const float CornerRadius = 16f;

	private static readonly Color Background = new(0.02f, 0.12f, 0.09f, 0.55f);
	private static readonly Color Outline = new("9ee4c3", 0.55f);

	private PauseCircleButton? _button;
	private Tween? _slide;
	private bool _open;

	/// <summary>Игрок нажал круглую кнопку паузы.</summary>
	public event Action? PausePressed;

	/// <summary>Игрок нажал фон панели: панель нужно свернуть.</summary>
	public event Action? BackgroundPressed;

	/// <summary>Панель открыта: язычок в это время скрыт.</summary>
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

	public override void _Draw()
	{
		// Скруглённые левые углы: правый край панели совпадает с краем экрана.
		DrawRect(new Rect2(CornerRadius, 0f, Size.X - CornerRadius, Size.Y), Background);
		DrawRect(new Rect2(0f, CornerRadius, CornerRadius, Size.Y - CornerRadius * 2f), Background);
		DrawCircle(new Vector2(CornerRadius, CornerRadius), CornerRadius, Background);
		DrawCircle(new Vector2(CornerRadius, Size.Y - CornerRadius), CornerRadius, Background);

		DrawLine(new Vector2(CornerRadius, 1f), new Vector2(Size.X, 1f), Outline, 2f);
		DrawLine(new Vector2(CornerRadius, Size.Y - 1f), new Vector2(Size.X, Size.Y - 1f), Outline, 2f);
		DrawLine(new Vector2(1f, CornerRadius), new Vector2(1f, Size.Y - CornerRadius), Outline, 2f);
		DrawArc(new Vector2(CornerRadius, CornerRadius), CornerRadius, Mathf.Pi, Mathf.Pi * 1.5f, 12, Outline, 2f);
		DrawArc(new Vector2(CornerRadius, Size.Y - CornerRadius), CornerRadius, Mathf.Pi * 0.5f, Mathf.Pi, 12, Outline, 2f);
	}

	private void UpdateButtonPosition()
	{
		if (_button == null) return;
		_button.ResetSize();
		_button.Position = (Size - _button.Size) * 0.5f;
	}
}
