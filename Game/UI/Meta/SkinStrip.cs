using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Горизонтальная лента карточек скинов: обрезаемое окно, плавный сдвиг стрелками и свайпом
/// с прилипанием к карточке. Лента строится из списка скинов, поэтому новый скин просто
/// удлиняет её — интерфейс перерисовывать не нужно.
/// </summary>
public partial class SkinStrip : Control
{
	/// <summary>Зазор между карточками.</summary>
	public const float Gap = 16f;

	private const float SlideTime = 0.22f;

	private Control? _clip;
	private HBoxContainer? _row;
	private float _offset;
	private Tween? _slide;
	private bool _dragging;
	private int _pointer = -1;
	private float _dragOrigin;
	private float _dragOffset;

	/// <summary>Игрок выбрал скин или запросил покупку.</summary>
	public event Action<SkinId>? SkinSelected;
	public event Action<SkinId>? SkinPurchased;

	/// <summary>Содержимое или сдвиг изменились: стрелки перечитывают доступность.</summary>
	public event Action? Changed;

	/// <summary>Сколько карточек в ленте.</summary>
	public int CardCount => _row?.GetChildCount() ?? 0;

	/// <summary>Текущий сдвиг ленты в пикселях.</summary>
	public float Offset => _offset;

	public bool CanPageLeft => _offset > 0.5f;
	public bool CanPageRight => _offset < MaxOffset - 0.5f;

	private static float Step => SkinCard.CardWidth + Gap;
	private float MaxOffset => Mathf.Max(0f, ContentWidth - Size.X);
	private float ContentWidth => CardCount <= 0 ? 0f : CardCount * Step - Gap;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		_clip = new Control { Name = "Clip", ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(_clip);
		_clip.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_row = new HBoxContainer { Name = "Row", MouseFilter = MouseFilterEnum.Ignore };
		_row.AddThemeConstantOverride("separation", (int)Gap);
		_clip.AddChild(_row);

		Resized += OnResized;
	}

	/// <summary>Пересобирает ленту из переданных скинов.</summary>
	public void SetEntries(SkinGalleryEntry[] entries)
	{
		if (_row == null)
			return;
		foreach (Node child in _row.GetChildren())
		{
			_row.RemoveChild(child);
			child.QueueFree();
		}
		foreach (SkinGalleryEntry entry in entries)
		{
			var card = new SkinCard();
			card.Setup(entry);
			card.Selected += id => SkinSelected?.Invoke(id);
			card.PurchaseRequested += id => SkinPurchased?.Invoke(id);
			_row.AddChild(card);
		}
		_row.Size = new Vector2(ContentWidth, SkinCard.CardHeight);
		SetOffset(0f);
	}

	/// <summary>Сдвигает ленту на одну карточку в указанную сторону.</summary>
	public void PageBy(int direction) => MoveTo(_offset + direction * Step);

	private void MoveTo(float target)
	{
		float clamped = Mathf.Clamp(target, 0f, MaxOffset);
		_slide?.Kill();
		_slide = CreateTween();
		_slide.TweenMethod(Callable.From<float>(SetOffset), _offset, clamped, SlideTime)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
	}

	private void SetOffset(float value)
	{
		_offset = value;
		if (_row != null)
			_row.Position = new Vector2(-_offset, 0f);
		Changed?.Invoke();
	}

	private void OnResized()
	{
		if (_row != null)
			_row.Size = new Vector2(ContentWidth, SkinCard.CardHeight);
		SetOffset(Mathf.Clamp(_offset, 0f, MaxOffset));
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed && !touch.Canceled)
				BeginDrag(touch.Position.X, touch.Index);
			else if (_dragging && _pointer == touch.Index)
				EndDrag();
		}
		else if (@event is InputEventScreenDrag drag && _dragging && drag.Index == _pointer)
			SetOffset(Mathf.Clamp(_dragOffset - (drag.Position.X - _dragOrigin), 0f, MaxOffset));
		else if (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left)
		{
			if (mouse.Pressed)
				BeginDrag(mouse.Position.X, -1);
			else if (_dragging && _pointer == -1)
				EndDrag();
		}
		else if (@event is InputEventMouseMotion motion && _dragging && _pointer == -1)
			SetOffset(Mathf.Clamp(_dragOffset - (motion.Position.X - _dragOrigin), 0f, MaxOffset));
	}

	private void BeginDrag(float x, int pointer)
	{
		_slide?.Kill();
		_dragging = true;
		_pointer = pointer;
		_dragOrigin = x;
		_dragOffset = _offset;
	}

	/// <summary>Отпускание пальца или мыши: лента прилипает к ближайшей карточке.</summary>
	private void EndDrag()
	{
		_dragging = false;
		_pointer = -1;
		MoveTo(Mathf.Round(_offset / Step) * Step);
	}
}
