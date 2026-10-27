using Godot;

namespace SwampFrog;

/// <summary>Отображение мира в игровой колонке экрана. Масштабируется изображение, не физические тела.</summary>
public partial class GameScreen : Control
{
	private SubViewport _world = null!;
	private TextureRect _image = null!;
	private Control _ui = null!;
	private Rect2 _lastScreen;
	private Vector2 _lastPixelScale;
	private Vector2 _worldSize = Playfield.Size;
	public Main Game { get; private set; } = null!;
	/// <summary>Общая колонка HUD и игрового поля: вся высота экрана.</summary>
	public Rect2 ArenaRect { get; private set; }
	public Rect2 FieldRect { get; private set; }
	public Control UiRoot => _ui;

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
		Game.ConfigureViewSize(_worldSize);
		Game.UiHost = _ui;
		Game.WorldToUi = WorldToUi;
		Game.ProcessMode = ProcessModeEnum.Pausable;
		_world.AddChild(Game);
	}

	public Vector2 ScreenToWorld(Vector2 point) => (point - FieldRect.Position) * _worldSize / FieldRect.Size;
	public Vector2 WorldToScreen(Vector2 point) => FieldRect.Position + point * FieldRect.Size / _worldSize;
	public Vector2 WorldToUi(Vector2 point) => (WorldToScreen(point) - _ui.Position) / _ui.Scale;

	public override void _Process(double delta) => RefreshLayout();

	public void RefreshLayout()
	{
		Rect2 screen = new(Vector2.Zero, Size);
		if (!screen.HasArea() || _world == null) return;
		Transform2D transform = GetScreenTransform();
		Vector2 pixelScale = new(transform.X.Length(), transform.Y.Length());
		if (screen == _lastScreen && pixelScale == _lastPixelScale) return;
		_lastScreen = screen;
		_lastPixelScale = pixelScale;
		// Поле и HUD занимают одну колонку во всю высоту экрана: сверху и снизу
		// неигровых полос нет, на широких экранах по бокам остаётся декоративный фон.
		ArenaRect = Playfield.ColumnFor(screen);
		float uiScale = Mathf.Max(0.01f, ArenaRect.Size.X / Playfield.Size.X);
		_worldSize = Playfield.SizeFor(ArenaRect);
		FieldRect = Playfield.Fit(ArenaRect, _worldSize);
		_image.Position = FieldRect.Position;
		_image.Size = FieldRect.Size;
		_world.Size2DOverride = (Vector2I)_worldSize;
		// Нативный размер текстуры соответствует фактическому размеру поля.
		_world.Size = new Vector2I(Mathf.Max(2, Mathf.RoundToInt(FieldRect.Size.X * pixelScale.X)),
			Mathf.Max(2, Mathf.RoundToInt(FieldRect.Size.Y * pixelScale.Y)));
		// Интерфейс лежит на той же колонке, поэтому затемнения меню закрывают весь экран.
		_ui.Position = ArenaRect.Position;
		_ui.Scale = Vector2.One * uiScale;
		_ui.Size = ArenaRect.Size / uiScale;
		Game?.ConfigureViewSize(_worldSize);
		Game?.HudNode?.UpdatePopupTransform();
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
		Vector2 scale = _worldSize / FieldRect.Size;
		var transform = new Transform2D(new Vector2(scale.X, 0), new Vector2(0, scale.Y), -FieldRect.Position * scale);
		using InputEvent mapped = @event.XformedBy(transform);
		Game.HandleGameInput(mapped);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
			Game?.CancelGameInput();
		else if (what == NotificationWMGoBackRequest)
			Game?.HandleBackRequest();
	}

	public override void _Draw()
	{
		Rect2 full = new(Vector2.Zero, Size);
		if (FieldRect.HasArea())
		{
			DrawPondBackground(full);
			DrawSideDividers(full);
		}
		else DrawRect(full, Playfield.PondBottom);
	}

	/// <summary>Продолжает градиент воды под HUD и фоном по бокам арены.</summary>
	private void DrawPondBackground(Rect2 full)
	{
		const int steps = 64;
		float strip = full.Size.Y / steps;
		for (int i = 0; i < steps; i++)
		{
			float y = full.Position.Y + i * strip;
			// Выше поля вода светлеет (поверхность), ниже границы — глубже и темнее.
			float t = Mathf.Clamp((y - FieldRect.Position.Y) / FieldRect.Size.Y, 0f, 1f);
			DrawRect(new Rect2(full.Position.X, y, full.Size.X, strip + 1f), Playfield.PondTop.Lerp(Playfield.PondBottom, t));
		}
	}

	/// <summary>Только визуальные разделители: не добавляют Control, коллизии или ограничения ввода.</summary>
	private void DrawSideDividers(Rect2 full)
	{
		// Вырезы телефона сами по себе не включают боковые панели.
		if (Size.X - ArenaRect.Size.X <= 1f) return;
		float left = ArenaRect.Position.X;
		float right = ArenaRect.End.X;
		Color shade = new(0.01f, 0.05f, 0.05f, 0.68f);
		DrawRect(new Rect2(full.Position.X, full.Position.Y, left - full.Position.X, full.Size.Y), shade);
		DrawRect(new Rect2(right, full.Position.Y, full.End.X - right, full.Size.Y), shade);

		float scale = ArenaRect.Size.X / Playfield.Size.X;
		float line = 2f * scale;
		float shadow = 16f * scale;
		for (int i = 0; i < 8; i++)
		{
			float offset = i * shadow / 8f;
			Color edgeShadow = new(0f, 0f, 0f, 0.26f * (1f - i / 8f));
			DrawRect(new Rect2(left - offset - shadow / 8f, full.Position.Y, shadow / 8f, full.Size.Y), edgeShadow);
			DrawRect(new Rect2(right + offset, full.Position.Y, shadow / 8f, full.Size.Y), edgeShadow);
		}
		Color divider = new("70bba1");
		DrawRect(new Rect2(left - line, full.Position.Y, line, full.Size.Y), divider);
		DrawRect(new Rect2(right, full.Position.Y, line, full.Size.Y), divider);
	}
}
