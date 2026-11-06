using Godot;

namespace SwampFrog;

/// <summary>
/// Управление звуком в правом верхнем углу главного меню: кнопка полного отключения
/// звука и раскрывающийся рычажком ползунок громкости слева от неё.
/// Живёт только на стартовом экране: в партии и оверлеях его нет.
/// </summary>
public partial class SoundControls : Control
{
	/// <summary>Отступ от верхнего и правого края поля.</summary>
	private const float Margin = 16f;

	/// <summary>Зазор между кнопкой mute, рычажком и ползунком.</summary>
	private const float Gap = 6f;

	private const float SlideDuration = 0.16f;

	private SoundMuteButton? _mute;
	private SliderHandle? _handle;
	private VolumeSlider? _slider;
	private Tween? _slide;
	private bool _expanded;
	private float _expandedX;
	private float _collapsedX;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Порядок добавления задаёт рисование: ползунок уезжает под рычажок и кнопку mute.
		_slider = new VolumeSlider { ZIndex = 1, Visible = false };
		_slider.ValueChanged += value => SoundSettings.SetVolume(value, false);
		_slider.ValueCommitted += value => SoundSettings.SetVolume(value);
		AddChild(_slider);

		_handle = new SliderHandle { ZIndex = 2 };
		_handle.Pressed += ToggleSlider;
		AddChild(_handle);

		_mute = new SoundMuteButton { ZIndex = 3 };
		_mute.Pressed += SoundSettings.ToggleMute;
		AddChild(_mute);

		SoundSettings.Load();
		SyncFromSettings();
		SoundSettings.Changed += SyncFromSettings;

		Resized += UpdatePositions;
		UpdatePositions();
	}

	public override void _ExitTree() => SoundSettings.Changed -= SyncFromSettings;

	private void SyncFromSettings()
	{
		_mute?.SetMuted(SoundSettings.Muted);
		_slider?.SetValue(SoundSettings.Volume);
		_slider?.SetMuted(SoundSettings.Muted);
	}

	private void ToggleSlider()
	{
		_expanded = !_expanded;
		_handle?.SetExpanded(_expanded);
		if (_expanded && _slider != null)
			_slider.Visible = true;
		Slide(_expanded ? _expandedX : _collapsedX);
	}

	private void Slide(float targetX)
	{
		if (_slider == null)
			return;
		_slide?.Kill();
		_slide = CreateTween();
		_slide.TweenProperty(_slider, "position:x", targetX, SlideDuration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		_slide.Finished += OnSlideFinished;
	}

	private void OnSlideFinished()
	{
		if (!_expanded && _slider != null)
			_slider.Visible = false;
	}

	/// <summary>Ползунок раскрывается влево от рычажка, кнопка mute прижата к правому краю.</summary>
	private void UpdatePositions()
	{
		if (_mute == null || _handle == null || _slider == null)
			return;
		_mute.ResetSize();
		_handle.ResetSize();
		_slider.ResetSize();

		float top = Margin;
		_mute.Position = new Vector2(Size.X - Margin - _mute.Size.X, top);
		_handle.Position = new Vector2(_mute.Position.X - Gap - _handle.Size.X, top);

		float sliderY = top + (_mute.Size.Y - _slider.Size.Y) * 0.5f;
		_expandedX = _handle.Position.X - Gap - _slider.Size.X;
		_collapsedX = _handle.Position.X;
		_slider.Position = new Vector2(_expanded ? _expandedX : _collapsedX, sliderY);
	}
}
