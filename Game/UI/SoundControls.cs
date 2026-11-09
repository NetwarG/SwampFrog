using Godot;

namespace SwampFrog;

/// <summary>
/// Управление звуком в правом верхнем углу главного меню: кнопка полного отключения
/// звука и раскрывающийся рычажком блок из двух вертикальных ползунков — музыка и эффекты.
/// Над каждым ползунком иконка, поясняющая, чем он управляет.
/// Живёт только на стартовом экране: в партии и оверлеях его нет.
/// </summary>
public partial class SoundControls : Control
{
	/// <summary>Отступ от верхнего и правого края поля.</summary>
	private const float Margin = 16f;

	/// <summary>Зазор между кнопкой mute, рычажком и блоком ползунков.</summary>
	private const float Gap = 6f;

	private const float SlideDuration = 0.16f;

	/// <summary>Насколько блок ползунков прячется вверх в свёрнутом виде.</summary>
	private const float HiddenOffset = 16f;

	/// <summary>Цвет заливки ползунка музыки: зелёный, как у остальных кнопок.</summary>
	private static readonly Color MusicAccent = new("b4e863");

	/// <summary>Цвет заливки ползунка эффектов: голубой, чтобы отличать от музыки.</summary>
	private static readonly Color SfxAccent = new("7fd6f2");

	private SoundMuteButton? _mute;
	private SliderHandle? _handle;
	private VolumePanel? _panel;
	private VolumeSlider? _musicSlider;
	private VolumeSlider? _sfxSlider;
	private VolumeIcon? _musicIcon;
	private VolumeIcon? _sfxIcon;
	private Tween? _slide;
	private bool _expanded;
	private float _expandedY;
	private float _collapsedY;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Порядок добавления задаёт рисование: блок ползунков под рычажком и кнопкой mute.
		_panel = new VolumePanel { ZIndex = 1, Visible = false };
		AddChild(_panel);
		_musicIcon = AddIcon(VolumeIconKind.Music);
		_sfxIcon = AddIcon(VolumeIconKind.Sfx);
		_musicSlider = AddSlider(MusicAccent);
		_sfxSlider = AddSlider(SfxAccent);
		_musicSlider.ValueChanged += value => SoundSettings.SetMusicVolume(value, false);
		_musicSlider.ValueCommitted += value => SoundSettings.SetMusicVolume(value);
		_sfxSlider.ValueChanged += value => SoundSettings.SetSfxVolume(value, false);
		_sfxSlider.ValueCommitted += value => SoundSettings.SetSfxVolume(value);

		_handle = new SliderHandle { ZIndex = 2 };
		_handle.Pressed += TogglePanel;
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

	private VolumeIcon AddIcon(VolumeIconKind kind)
	{
		var icon = new VolumeIcon { Kind = kind, MouseFilter = MouseFilterEnum.Ignore };
		_panel!.AddChild(icon);
		return icon;
	}

	private VolumeSlider AddSlider(Color accent)
	{
		var slider = new VolumeSlider { Accent = accent };
		_panel!.AddChild(slider);
		return slider;
	}

	private void SyncFromSettings()
	{
		_mute?.SetMuted(SoundSettings.Muted);
		_musicSlider?.SetValue(SoundSettings.MusicVolume);
		_sfxSlider?.SetValue(SoundSettings.SfxVolume);
		_musicSlider?.SetMuted(SoundSettings.Muted);
		_sfxSlider?.SetMuted(SoundSettings.Muted);
	}

	private void TogglePanel()
	{
		_expanded = !_expanded;
		_handle?.SetExpanded(_expanded);
		if (_expanded && _panel != null)
			_panel.Visible = true;
		Slide(_expanded ? _expandedY : _collapsedY);
	}

	/// <summary>Блок ползунков выезжает вниз из-под кнопок и растворяется при сворачивании.</summary>
	private void Slide(float targetY)
	{
		if (_panel == null)
			return;
		_slide?.Kill();
		_slide = CreateTween();
		_slide.SetParallel(true);
		_slide.TweenProperty(_panel, "position:y", targetY, SlideDuration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		_slide.TweenProperty(_panel, "modulate:a", _expanded ? 1f : 0f, SlideDuration);
		_slide.Finished += OnSlideFinished;
	}

	private void OnSlideFinished()
	{
		if (!_expanded && _panel != null)
			_panel.Visible = false;
	}

	/// <summary>Кнопки прижаты к правому верхнему краю, блок ползунков — под ними.</summary>
	private void UpdatePositions()
	{
		if (_mute == null || _handle == null || _panel == null)
			return;
		_mute.ResetSize();
		_handle.ResetSize();
		_panel.ResetSize();
		LayoutPanel();

		float top = Margin;
		_mute.Position = new Vector2(Size.X - Margin - _mute.Size.X, top);
		_handle.Position = new Vector2(_mute.Position.X - Gap - _handle.Size.X, top);

		float panelX = Size.X - Margin - _panel.Size.X;
		_expandedY = top + _mute.Size.Y + Gap;
		_collapsedY = _expandedY - HiddenOffset;
		_panel.Position = new Vector2(panelX, _expanded ? _expandedY : _collapsedY);
	}

	/// <summary>Две колонки внутри блока: иконка сверху, ползунок под ней.</summary>
	private void LayoutPanel()
	{
		if (_panel == null)
			return;
		float iconWidth = VolumeIcon.IconSize.X;
		float leftX = VolumePanel.Pad;
		float rightX = VolumePanel.Pad + iconWidth + VolumePanel.ColumnGap;
		float iconY = VolumePanel.Pad;
		float sliderY = iconY + VolumeIcon.IconSize.Y + VolumePanel.IconGap;

		_musicIcon?.ResetSize();
		_sfxIcon?.ResetSize();
		_musicSlider?.ResetSize();
		_sfxSlider?.ResetSize();
		if (_musicIcon != null) _musicIcon.Position = new Vector2(leftX, iconY);
		if (_sfxIcon != null) _sfxIcon.Position = new Vector2(rightX, iconY);
		if (_musicSlider != null) _musicSlider.Position = new Vector2(leftX, sliderY);
		if (_sfxSlider != null) _sfxSlider.Position = new Vector2(rightX, sliderY);
	}
}
