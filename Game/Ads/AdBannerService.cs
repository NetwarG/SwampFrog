using System;

namespace SwampFrog;

/// <summary>
/// Точка входа рекламы для интерфейса: держит адаптер рекламной сети, состояние баннера
/// и его высоту. UI знает только этот класс, поэтому подключение SDK не меняет меню.
/// </summary>
public sealed class AdBannerService
{
	private readonly IAdBannerAdapter _adapter;
	private AdBannerState _state = AdBannerState.Hidden;

	/// <param name="adapter">Адаптер рекламной сети; без него — заглушка без нативных вызовов.</param>
	public AdBannerService(IAdBannerAdapter? adapter = null)
	{
		_adapter = adapter ?? new MockBannerAdapter();
		_adapter.StateChanged += SetState;
	}

	/// <summary>Состояние баннера: по нему слот решает, показывать дом-баннер или место под объявление.</summary>
	public AdBannerState State => _state;

	/// <summary>Реклама включена настройками игры.</summary>
	public bool Enabled => AdConfig.Enabled;

	/// <summary>Платформа и SDK позволяют запросить настоящее объявление.</summary>
	public bool IsAvailable => AdConfig.Enabled && AdPlatform.IsMobile && AdPlatform.HasSdk && _adapter.IsAvailable;

	/// <summary>Вместо объявления показывается дом-баннер.</summary>
	public bool ShowsPlaceholder => _state != AdBannerState.Loaded;

	/// <summary>Состояние баннера изменилось: слот пересобирает вид.</summary>
	public event Action? Changed;

	/// <summary>Меню открылось: запрашивает объявление, если реклама доступна.</summary>
	public void Require()
	{
		if (!AdConfig.Enabled)
		{
			SetState(AdBannerState.Hidden);
			return;
		}
		if (!IsAvailable)
		{
			SetState(AdBannerState.Unsupported);
			return;
		}
		SetState(AdBannerState.Loading);
		_adapter.Load();
	}

	/// <summary>Меню закрылось: баннер прячется на время партии.</summary>
	public void Release()
	{
		if (_state == AdBannerState.Hidden)
			return;
		_adapter.Hide();
		SetState(AdBannerState.Hidden);
	}

	private void SetState(AdBannerState state)
	{
		if (_state == state)
			return;
		_state = state;
		Changed?.Invoke();
	}
}
