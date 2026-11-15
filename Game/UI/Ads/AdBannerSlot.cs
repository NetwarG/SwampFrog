using Godot;

namespace SwampFrog;

/// <summary>
/// Полоса рекламы внизу главного меню: держит место под баннер рекламной сети и показывает
/// дом-баннер, пока настоящего объявления нет. Высота полосы отдаётся наружу, чтобы меню
/// поднималось над баннером (см. docs/ADS.md).
/// </summary>
public partial class AdBannerSlot : Control
{
	/// <summary>Отступы полосы от боковых краёв меню и от нижней кромки экрана.</summary>
	private const float SideMargin = 24f;
	private const float BottomMargin = 12f;

	/// <summary>Акцент пустой полосы: приглушённый тон воды, чтобы не спорить с фоном меню.</summary>
	private static readonly Color SpaceAccent = new("8aa8a0");

	private AdBannerService? _ads;
	private StickerBackground? _panel;
	private AdBannerView? _house;

	/// <summary>Высота полосы в логических единицах: на неё поднимается меню.</summary>
	public float ReservedHeight { get; private set; }

	/// <summary>Показан ли дом-баннер: настоящего объявления нет.</summary>
	public bool IsHouseAdVisible => _house?.Visible ?? false;

	/// <summary>Показано ли пустое место под объявление рекламной сети.</summary>
	public bool IsAdSpaceVisible => _panel?.Visible ?? false;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.BottomWide);
		_panel = new StickerBackground
		{
			Name = "AdSpace",
			LightColor = StickerStyle.Light(SpaceAccent),
			DarkColor = StickerStyle.Dark(SpaceAccent),
			CornerRadius = UiStyle.CornerRadius * 0.8f,
			OutlineWidth = StickerStyle.OutlineWidth
		};
		AddChild(_panel);
		_house = new AdBannerView { Name = "HouseAd", Visible = false };
		AddChild(_house);
		Refresh();
	}

	public override void _ExitTree()
	{
		if (_ads != null)
			_ads.Changed -= ApplyState;
	}

	/// <summary>Связывает слот с сервисом рекламы и считает высоту полосы.</summary>
	public void Bind(AdBannerService ads)
	{
		if (_ads != ads)
		{
			if (_ads != null) _ads.Changed -= ApplyState;
			_ads = ads;
			_ads.Changed += ApplyState;
		}
		Refresh();
	}

	/// <summary>Пересчитывает высоту полосы под текущий экран. Возвращает высоту в единицах.</summary>
	public float Refresh()
	{
		ReservedHeight = _ads == null || !_ads.Enabled ? 0f : AdConfig.HeightUnits(PixelsPerUnit());
		OffsetLeft = SideMargin;
		OffsetRight = -SideMargin;
		OffsetBottom = -BottomMargin;
		OffsetTop = -(BottomMargin + ReservedHeight);
		ApplyState();
		return ReservedHeight;
	}

	/// <summary>Пикселей на логическую единицу: масштаб растяжения окна относительно 540×960.</summary>
	private float PixelsPerUnit()
	{
		if (!IsInsideTree())
			return 1f;
		Vector2 viewport = GetViewport().GetVisibleRect().Size;
		Vector2 window = DisplayServer.WindowGetSize();
		return viewport.X <= 0f || window.X <= 0f ? 1f : window.X / viewport.X;
	}

	/// <summary>Дом-баннер виден без SDK, пустая полоса — под объявление рекламной сети.</summary>
	private void ApplyState()
	{
		if (_ads == null)
			return;
		bool placeholder = _ads.ShowsPlaceholder;
		Visible = _ads.State != AdBannerState.Hidden;
		if (_house != null) _house.Visible = placeholder;
		if (_panel != null) _panel.Visible = !placeholder;
	}
}
