using Godot;

namespace SwampFrog;

/// <summary>Отображение мира в безопасной области. Масштабируется изображение, не физические тела.</summary>
public partial class GameScreen : Control
{
	private SubViewport _world = null!;
	private TextureRect _image = null!;
	private Control _ui = null!;
	private Rect2 _lastSafeArea;
	private Vector2 _lastPixelScale;
	public Main Game { get; private set; } = null!;
	public Rect2 FieldRect { get; private set; }
	public Control UiRoot => _ui;
	// Используется проверками разных вырезов экрана без изменения физических устройств.
	public Rect2? SafeAreaOverride { get; set; }

	/// <summary>Высота верхней полосы UI (счёт, сердечки): над рамкой поля всегда оставляется это место.</summary>
	private const float TopHudInset = 80f;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_world = new SubViewport
		{
			Name = "GameWorld", Size = (Vector2I)Playfield.Size,
			Size2DOverride = (Vector2I)Playfield.Size, Size2DOverrideStretch = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always, HandleInputLocally = true,
			Disable3D = true, GuiDisableInput = true, World2D = new World2D()
		};
		AddChild(_world);
		_image = new TextureRect
		{
			Name = "Playfield", Texture = _world.GetTexture(),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_image);
		_ui = new Control { Name = "SafeUI", MouseFilter = MouseFilterEnum.Ignore };
		AddChild(_ui);
		RefreshLayout();
		Game = GD.Load<PackedScene>("res://Scenes/Main.tscn").Instantiate<Main>();
		Game.UiHost = _ui;
		Game.WorldToUi = WorldToUi;
		// Мировая координата верхней кромки экрана: 0, если верх поля совпадает с верхом экрана,
		// отрицательная на высоких экранах. Используется для спавна предметов за краем.
		Game.ScreenTopWorldY = () => -FieldRect.Position.Y / FieldRect.Size.Y * Playfield.Size.Y;
		Game.ProcessMode = ProcessModeEnum.Pausable;
		_world.AddChild(Game);
	}

	public Vector2 ScreenToWorld(Vector2 point) => (point - FieldRect.Position) * Playfield.Size / FieldRect.Size;
	public Vector2 WorldToScreen(Vector2 point) => FieldRect.Position + point * FieldRect.Size / Playfield.Size;
	public Vector2 WorldToUi(Vector2 point) => (WorldToScreen(point) - _ui.Position) / _ui.Scale;

	private Rect2 ReadSafeArea()
	{
		Rect2 full = new(Vector2.Zero, Size);
		if (SafeAreaOverride is Rect2 forced) return full.Intersection(forced);
		if (!OS.HasFeature("android") && !OS.HasFeature("ios")) return full;
		Rect2 pixels = DisplayServer.GetDisplaySafeArea();
		if (!pixels.HasArea()) return full;
		Rect2 safe = full.Intersection(GetScreenTransform().AffineInverse() * pixels);
		return safe.HasArea() ? safe : full;
	}

	public override void _Process(double delta) => RefreshLayout();

	public void RefreshLayout()
	{
		Rect2 safe = ReadSafeArea();
		if (!safe.HasArea() || _world == null) return;
		Transform2D screen = GetScreenTransform();
		Vector2 pixelScale = new(screen.X.Length(), screen.Y.Length());
		if (safe == _lastSafeArea && pixelScale == _lastPixelScale) return;
		_lastSafeArea = safe;
		_lastPixelScale = pixelScale;
		// Счёт и сердечки живут над рамкой поля: поле вписывается в безопасную область за вычетом верхней полосы UI.
		float uiScale = Mathf.Max(0.01f, Mathf.Min(safe.Size.X / 540f, safe.Size.Y / 960f));
		float inset = Mathf.Min(TopHudInset * uiScale, safe.Size.Y * 0.5f);
		Rect2 fieldArea = new(safe.Position.X, safe.Position.Y + inset, safe.Size.X, safe.Size.Y - inset);
		FieldRect = Playfield.Fit(fieldArea);
		_image.Position = FieldRect.Position;
		_image.Size = FieldRect.Size;
		// Нативное разрешение картинки при неизменных 540×960 координатах мира.
		_world.Size = new Vector2I(Mathf.Max(2, Mathf.RoundToInt(FieldRect.Size.X * pixelScale.X)),
			Mathf.Max(2, Mathf.RoundToInt(FieldRect.Size.Y * pixelScale.Y)));
		_ui.Position = safe.Position;
		_ui.Scale = Vector2.One * uiScale;
		_ui.Size = safe.Size / uiScale;
		QueueRedraw();
	}

	public override void _Input(InputEvent @event)
	{
		// Отпускание над кнопкой или за пределами поля также завершает игровой жест.
		if (@event is InputEventScreenTouch { Pressed: false } or InputEventMouseButton { Pressed: false })
			ForwardInput(@event);
	}

	public override void _UnhandledInput(InputEvent @event) => ForwardInput(@event);

	private void ForwardInput(InputEvent @event)
	{
		if (Game == null || !FieldRect.HasArea()) return;
		Vector2? point = @event switch
		{
			InputEventScreenTouch touch => touch.Position,
			InputEventScreenDrag drag => drag.Position,
			InputEventMouse mouse => mouse.Position,
			_ => null
		};
		bool pressed = @event is InputEventScreenTouch { Pressed: true } or InputEventMouseButton { Pressed: true };
		if (pressed && point.HasValue && !FieldRect.HasPoint(point.Value)) return;
		Vector2 scale = Playfield.Size / FieldRect.Size;
		var transform = new Transform2D(new Vector2(scale.X, 0), new Vector2(0, scale.Y), -FieldRect.Position * scale);
		using InputEvent mapped = @event.XformedBy(transform);
		Game.HandleGameInput(mapped);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
			Game?.CancelGameInput();
	}

	public override void _Draw()
	{
		Rect2 full = new(Vector2.Zero, Size);
		if (FieldRect.HasArea()) DrawPondBackground(full);
		else DrawRect(full, Playfield.PondBottom);
		if (FieldRect.HasArea()) DrawRect(FieldRect.Grow(2f), new Color("408773"), false, 2f);
	}

	/// <summary>Продолжает градиент воды за границы поля, чтобы свободное место выглядело водой, а не полосой.</summary>
	private void DrawPondBackground(Rect2 full)
	{
		const int steps = 64;
		float strip = full.Size.Y / steps;
		for (int i = 0; i < steps; i++)
		{
			float y = full.Position.Y + i * strip;
			// Выше поля вода светлеет (поверхность), ниже границы — глубже и темнее.
			float t = (y - FieldRect.Position.Y) / FieldRect.Size.Y;
			DrawRect(new Rect2(full.Position.X, y, full.Size.X, strip + 1f), Playfield.PondTop.Lerp(Playfield.PondBottom, t));
		}
	}
}
