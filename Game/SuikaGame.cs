using System;
using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Мини-игра Suika: лягушка двигается по верхней границе и бросает фрукты в кувшин.
/// Фрукты используют тот же FallingItem, что и основной режим, поэтому их внешний вид
/// и последовательность развития всегда остаются синхронизированы с FruitCatalog.
/// </summary>
public partial class SuikaGame : Node2D
{
	private sealed class Piece
	{
		public required FallingItem View;
		public FruitKind Kind;
		public Vector2 Position;
		public Vector2 Velocity;
		public float Radius;
		public float Age;
	}

	private const float Gravity = 980f;
	private const float Restitution = 0.12f;
	private const float BowlSide = 34f;
	private const float BowlBottom = 88f;
	private const float BowlTop = 204f;
	private const float OverflowDelay = 1.15f;

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<Piece> _pieces = new();
	private FruitKind[] _startPool = Array.Empty<FruitKind>();
	private FruitKind _currentKind = FruitKind.Cherry;
	private FruitKind _nextKind = FruitKind.Cherry;
	private Rect2 _bowl;
	private float _aimX;
	private float _overflowTime;
	private float _dropCooldown;
	private float _throwTimer;
	private int _score;
	private int _bestScore;
	private bool _active;
	private bool _gameOver;

	private Frog? _frog;
	private FallingItem? _heldFruit;
	private FallingItem? _nextFruit;
	private CanvasLayer? _uiLayer;
	private Control? _uiRoot;
	private Label? _scoreLabel;
	private Label? _bestLabel;
	private Label? _nextLabel;
	private Label? _hintLabel;
	private Control? _gameOverRoot;
	private Label? _gameOverScore;

	public Main? Game { get; set; }
	public event Action? ExitRequested;

	public override void _Ready()
	{
		_rng.Randomize();
		_bestScore = LoadBestScore();
		BuildUi();

		_frog = new Frog { DecorativeOnly = true };
		AddChild(_frog);
		_frog.ZIndex = 3;

		_heldFruit = CreateVisualFruit(FruitKind.Cherry, 14f);
		_heldFruit.ZIndex = 4;
		_nextFruit = CreateVisualFruit(FruitKind.Cherry, 15f);
		_nextFruit.ZIndex = 4;
		GetViewport().SizeChanged += OnViewportResized;
		UpdateBowl();
		if (_uiRoot != null) _uiRoot.Visible = false;
		if (_uiLayer != null) _uiLayer.Visible = false;
		Visible = false;
	}

	public void Open(FruitKind[] availableKinds)
	{
		_startPool = availableKinds.Length > 0 ? availableKinds : new[] { FruitKind.Cherry };
		Visible = true;
		_uiRoot!.Visible = true;
		_uiLayer!.Visible = true;
		_active = true;
		_gameOver = false;
		_score = 0;
		_overflowTime = 0f;
		_dropCooldown = 0.25f;
		_throwTimer = 0f;
		_aimX = GetViewportRect().Size.X * 0.5f;
		ClearPieces();
		UpdateBowl();
		_currentKind = PickStartFruit();
		_nextKind = PickStartFruit();
		UpdateVisuals();
		_gameOverRoot!.Visible = false;
		_scoreLabel!.Text = "Счёт  0";
		_bestLabel!.Text = $"Рекорд  {_bestScore}";
		_hintLabel!.Visible = true;
	}

	public void Close()
	{
		_active = false;
		_gameOver = false;
		ClearPieces();
		if (_gameOverRoot != null) _gameOverRoot.Visible = false;
		if (_uiRoot != null) _uiRoot.Visible = false;
		if (_uiLayer != null) _uiLayer.Visible = false;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!_active) return;
		float dt = Mathf.Min((float)delta, 0.033f);
		_dropCooldown = Mathf.Max(0f, _dropCooldown - dt);
		_throwTimer = Mathf.Max(0f, _throwTimer - dt);
		UpdateBowl();
		UpdateFrog(dt);
		if (_gameOver) return;

		SimulatePieces(dt);
		ResolveCollisions();
		TryMergePieces();
		UpdatePieceViews(dt);
		CheckOverflow(dt);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_active) return;

		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				_aimX = touch.Position.X;
			}
			else
			{
				DropFruit();
			}
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventScreenDrag drag)
		{
			_aimX = drag.Position.X;
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseMotion motion)
		{
			_aimX = motion.Position.X;
			return;
		}

		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseDown)
		{
			_aimX = mouseDown.Position.X;
			return;
		}

		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
		{
			DropFruit();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
		{
			ExitRequested?.Invoke();
			GetViewport().SetInputAsHandled();
		}
		else if (@event is InputEventKey { Pressed: true, Keycode: Key.Space or Key.Enter })
		{
			DropFruit();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Draw()
	{
		Vector2 size = GetViewportRect().Size;
		if (size.X <= 0f || size.Y <= 0f) return;

		Color top = new("234f54");
		Color bottom = new("102d36");
		float strip = size.Y / 48f;
		for (int i = 0; i < 48; i++)
		{
			float t = i / 47f;
			DrawRect(new Rect2(0f, i * strip, size.X, strip + 1f), top.Lerp(bottom, t));
		}

		DrawCircle(new Vector2(size.X * 0.84f, size.Y * 0.19f), 94f, new Color(1f, 1f, 1f, 0.035f));
		DrawCircle(new Vector2(size.X * 0.15f, size.Y * 0.76f), 120f, new Color(0.2f, 0.9f, 0.67f, 0.035f));

		// Стеклянное тело кувшина и его толстый контур.
		DrawRect(_bowl, new Color(0.03f, 0.14f, 0.16f, 0.72f));
		DrawLine(new Vector2(_bowl.Position.X, _bowl.Position.Y), new Vector2(_bowl.End.X, _bowl.Position.Y), new Color("9ee4c3"), 3f);
		DrawLine(new Vector2(_bowl.Position.X, _bowl.Position.Y), new Vector2(_bowl.Position.X + 18f, _bowl.End.Y), new Color("7acbb2"), 6f);
		DrawLine(new Vector2(_bowl.End.X, _bowl.Position.Y), new Vector2(_bowl.End.X - 18f, _bowl.End.Y), new Color("7acbb2"), 6f);
		DrawLine(new Vector2(_bowl.Position.X + 18f, _bowl.End.Y), new Vector2(_bowl.End.X - 18f, _bowl.End.Y), new Color("9ee4c3"), 8f);
		DrawLine(new Vector2(_bowl.Position.X, _bowl.Position.Y + 30f), new Vector2(_bowl.End.X, _bowl.Position.Y + 30f), new Color(0.62f, 0.93f, 0.8f, 0.18f), 2f);

		// Линия переполнения помогает понять, когда кувшин уже заполнен.
		float dangerY = _bowl.Position.Y + 56f;
		DrawLine(new Vector2(_bowl.Position.X + 5f, dangerY), new Vector2(_bowl.End.X - 5f, dangerY), new Color(1f, 0.55f, 0.38f, 0.33f), 2f);
	}

	private void BuildUi()
	{
		_uiLayer = new CanvasLayer { Layer = 20 };
		AddChild(_uiLayer);
		var root = new Control { MouseFilter = Control.MouseFilterEnum.Pass };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_uiRoot = root;
		_uiLayer.AddChild(root);

		_scoreLabel = MakeLabel("Счёт  0", 26, new Color("ffffff"));
		_scoreLabel.Position = new Vector2(18f, 16f);
		root.AddChild(_scoreLabel);
		_bestLabel = MakeLabel($"Рекорд  {_bestScore}", 17, new Color(1f, 1f, 1f, 0.62f));
		_bestLabel.Position = new Vector2(20f, 50f);
		root.AddChild(_bestLabel);

		_nextLabel = MakeLabel("Следующий", 17, new Color("b4e863"));
		_nextLabel.Position = new Vector2(0f, 15f);
		_nextLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_nextLabel.AnchorLeft = 1f;
		_nextLabel.AnchorRight = 1f;
		_nextLabel.OffsetLeft = -112f;
		_nextLabel.OffsetRight = -18f;
		_nextLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(_nextLabel);

		Button back = MakeButton("В меню", 17, new Color("ffd45e"), new Vector2(112f, 42f));
		back.Position = new Vector2(16f, 92f);
		back.Pressed += () => ExitRequested?.Invoke();
		root.AddChild(back);

		_hintLabel = MakeLabel("Отпусти, чтобы бросить", 18, new Color(1f, 1f, 1f, 0.78f));
		_hintLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_hintLabel.AnchorTop = 1f;
		_hintLabel.AnchorBottom = 1f;
		_hintLabel.OffsetTop = -50f;
		_hintLabel.OffsetBottom = -18f;
		_hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(_hintLabel);

		BuildGameOver(root);
	}

	private void BuildGameOver(Control parent)
	{
		_gameOverRoot = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
		_gameOverRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		parent.AddChild(_gameOverRoot);
		var shade = new ColorRect { Color = new Color(0.02f, 0.08f, 0.1f, 0.82f), MouseFilter = Control.MouseFilterEnum.Ignore };
		shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_gameOverRoot.AddChild(shade);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		box.AddThemeConstantOverride("separation", 14);
		_gameOverRoot.AddChild(box);
		box.AddChild(MakeLabel("Кувшин полон", 42, new Color("ffd45e")));
		_gameOverScore = MakeLabel("Счёт: 0", 28, new Color("ffffff"));
		box.AddChild(_gameOverScore);
		Button retry = MakeButton("Играть ещё", 22, new Color("b4e863"), new Vector2(300f, 54f));
		retry.Pressed += Restart;
		box.AddChild(retry);
		Button menu = MakeButton("В меню", 22, new Color("ffd45e"), new Vector2(300f, 54f));
		menu.Pressed += () => ExitRequested?.Invoke();
		box.AddChild(menu);
		CallDeferred(nameof(CenterGameOver));
	}

	private void CenterGameOver()
	{
		if (_gameOverRoot == null) return;
		foreach (Node child in _gameOverRoot.GetChildren())
		{
			if (child is VBoxContainer box)
			{
				box.ResetSize();
				Vector2 view = GetViewportRect().Size;
				box.Position = new Vector2(view.X * 0.5f - box.Size.X * 0.5f, view.Y * 0.5f - box.Size.Y * 0.5f);
			}
		}
	}

	private Label MakeLabel(string text, int size, Color color)
	{
		var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		return label;
	}

	private Button MakeButton(string text, int size, Color accent, Vector2 minSize)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = minSize,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		button.AddThemeFontSizeOverride("font_size", size);
		button.AddThemeColorOverride("font_color", new Color("173c34"));
		button.AddThemeColorOverride("font_pressed_color", new Color("ffffff"));
		button.AddThemeStyleboxOverride("normal", ButtonStyle(new Color(accent, 0.9f), accent));
		button.AddThemeStyleboxOverride("hover", ButtonStyle(accent, new Color("ffffff")));
		button.AddThemeStyleboxOverride("pressed", ButtonStyle(new Color(accent, 0.72f), new Color("ffffff")));
		button.AddThemeStyleboxOverride("focus", ButtonStyle(new Color(accent, 0.9f), accent));
		return button;
	}

	private static StyleBoxFlat ButtonStyle(Color background, Color border)
	{
		var style = new StyleBoxFlat { BgColor = background, BorderColor = new Color(border, 0.72f) };
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(12);
		style.ShadowColor = new Color(0f, 0f, 0f, 0.18f);
		style.ShadowSize = 4;
		return style;
	}

	private void UpdateBowl()
	{
		Vector2 view = GetViewportRect().Size;
		_bowl = new Rect2(BowlSide, BowlTop, Mathf.Max(180f, view.X - BowlSide * 2f), Mathf.Max(260f, view.Y - BowlTop - BowlBottom));
		QueueRedraw();
	}

	private void OnViewportResized()
	{
		UpdateBowl();
		CenterGameOver();
		UpdateVisuals();
	}

	private void UpdateFrog(float dt)
	{
		if (_frog == null) return;
		float x = Mathf.Clamp(_aimX, _bowl.Position.X + 45f, _bowl.End.X - 45f);
		_aimX = x;
		_frog.Position = new Vector2(x, 103f);
		float scale = Mathf.Clamp(Mathf.Min(GetViewportRect().Size.X / 540f, GetViewportRect().Size.Y / 960f), 0.85f, 1.7f);
		_frog.SyncUiScale(0.56f * scale);
		_frog.ThrowPose = _throwTimer > 0f;
		_frog.SetLookTarget(new Vector2(x, _bowl.Position.Y));
		if (_heldFruit != null)
		{
			_heldFruit.Position = new Vector2(x, _bowl.Position.Y - 28f);
		}
	}

	private void UpdateVisuals()
	{
		if (_heldFruit == null || _nextFruit == null) return;
		_heldFruit.Kind = _currentKind;
		_heldFruit.QueueRedraw();
		_nextFruit.Kind = _nextKind;
		_nextFruit.QueueRedraw();
		_nextFruit.Position = new Vector2(GetViewportRect().Size.X - 65f, 67f);
	}

	private FruitKind PickStartFruit()
	{
		return _startPool[_rng.RandiRange(0, _startPool.Length - 1)];
	}

	private void DropFruit()
	{
		if (!_active || _gameOver || _dropCooldown > 0f) return;
		float radius = FruitRadius(_currentKind);
		float x = Mathf.Clamp(_aimX, _bowl.Position.X + radius + 4f, _bowl.End.X - radius - 4f);
		CreatePiece(_currentKind, new Vector2(x, _bowl.Position.Y + radius + 4f), new Vector2(0f, 80f));
		_throwTimer = 0.24f;
		_currentKind = _nextKind;
		_nextKind = PickStartFruit();
		_dropCooldown = 0.3f;
		_hintLabel!.Visible = false;
		UpdateVisuals();
	}

	private Piece CreatePiece(FruitKind kind, Vector2 position, Vector2 velocity)
	{
		float radius = FruitRadius(kind);
		var item = CreateVisualFruit(kind, radius);
		item.Position = position;
		item.Rotation = _rng.RandfRange(-0.2f, 0.2f);
		var piece = new Piece { View = item, Kind = kind, Position = position, Velocity = velocity, Radius = radius };
		_pieces.Add(piece);
		return piece;
	}

	private FallingItem CreateVisualFruit(FruitKind kind, float radius)
	{
		var item = new FallingItem { ItemType = ItemType.Fruit, Kind = kind, ManualPhysics = true };
		AddChild(item);
		float baseRadius = FruitCatalog.Get(kind).BaseRadius;
		item.Scale = Vector2.One * (radius / baseRadius);
		item.Radius = radius;
		return item;
	}

	private float FruitRadius(FruitKind kind)
	{
		int order = (int)kind;
		return 16f * Mathf.Pow(1.18f, order);
	}

	private void SimulatePieces(float dt)
	{
		foreach (Piece piece in _pieces)
		{
			piece.Age += dt;
			piece.Velocity.Y += Gravity * dt;
			piece.Velocity.X *= Mathf.Pow(0.997f, dt * 60f);
			piece.Position += piece.Velocity * dt;

			float left = _bowl.Position.X + piece.Radius;
			float right = _bowl.End.X - piece.Radius;
			if (piece.Position.X < left)
			{
				piece.Position.X = left;
				piece.Velocity.X = Mathf.Abs(piece.Velocity.X) * 0.35f;
			}
			else if (piece.Position.X > right)
			{
				piece.Position.X = right;
				piece.Velocity.X = -Mathf.Abs(piece.Velocity.X) * 0.35f;
			}

			float floor = _bowl.End.Y - piece.Radius - 6f;
			if (piece.Position.Y > floor)
			{
				piece.Position.Y = floor;
				if (Mathf.Abs(piece.Velocity.Y) > 16f) piece.Velocity.Y = -Mathf.Abs(piece.Velocity.Y) * Restitution;
				else piece.Velocity.Y = 0f;
				piece.Velocity.X *= 0.86f;
			}
		}
	}

	private void ResolveCollisions()
	{
		for (int pass = 0; pass < 2; pass++)
		{
			for (int i = 0; i < _pieces.Count; i++)
			{
				for (int j = i + 1; j < _pieces.Count; j++)
				{
					Piece a = _pieces[i];
					Piece b = _pieces[j];
					Vector2 delta = a.Position - b.Position;
					float minDistance = a.Radius + b.Radius;
					float distanceSquared = delta.LengthSquared();
					if (distanceSquared >= minDistance * minDistance) continue;
					float distance = Mathf.Sqrt(Mathf.Max(distanceSquared, 0.0001f));
					Vector2 normal = distanceSquared > 0.0001f ? delta / distance : Vector2.Up;
					float overlap = minDistance - distance;
					a.Position += normal * (overlap * 0.5f);
					b.Position -= normal * (overlap * 0.5f);
					float relativeNormal = (a.Velocity - b.Velocity).Dot(normal);
					if (relativeNormal < 0f)
					{
						float impulse = -(1f + Restitution) * relativeNormal * 0.5f;
						a.Velocity += normal * impulse;
						b.Velocity -= normal * impulse;
					}
				}
			}
		}
	}

	private void TryMergePieces()
	{
		for (int i = 0; i < _pieces.Count; i++)
		{
			Piece a = _pieces[i];
			if ((int)a.Kind >= (int)FruitKind.Watermelon) continue;
			for (int j = i + 1; j < _pieces.Count; j++)
			{
				Piece b = _pieces[j];
				if (a.Kind != b.Kind) continue;
				// ResolveCollisions оставляет соприкасающиеся круги ровно на сумме
				// радиусов, поэтому слияние проверяется по факту контакта.
				if (a.Position.DistanceTo(b.Position) > (a.Radius + b.Radius) * 1.06f) continue;

				FruitKind mergedKind = (FruitKind)((int)a.Kind + 1);
				Vector2 position = (a.Position + b.Position) * 0.5f;
				Vector2 velocity = (a.Velocity + b.Velocity) * 0.5f;
				RemovePiece(a);
				RemovePiece(b);
				CreatePiece(mergedKind, position, velocity * 0.45f);
				_score += 10 * ((int)mergedKind + 1);
				_scoreLabel!.Text = $"Счёт  {_score}";
				return;
			}
		}
	}

	private void RemovePiece(Piece piece)
	{
		_pieces.Remove(piece);
		piece.View.QueueFree();
	}

	private void UpdatePieceViews(float dt)
	{
		foreach (Piece piece in _pieces)
		{
			piece.View.Position = piece.Position;
			piece.View.Rotation += Mathf.Clamp(piece.Velocity.X / Mathf.Max(30f, piece.Radius * 4f), -1.4f, 1.4f) * dt;
		}
	}

	private void CheckOverflow(float dt)
	{
		bool overflowing = false;
		float danger = _bowl.Position.Y + 56f;
		foreach (Piece piece in _pieces)
		{
			// Новый фрукт пересекает линию по пути вниз, поэтому учитываем только
			// объект, который уже пробыл в кувшине заметное время.
			if (piece.Age > 0.45f && piece.Position.Y - piece.Radius < danger)
			{
				overflowing = true;
				break;
			}
		}
		_overflowTime = overflowing ? _overflowTime + dt : Mathf.Max(0f, _overflowTime - dt * 2f);
		if (_overflowTime >= OverflowDelay) EndRound();
	}

	private void EndRound()
	{
		_gameOver = true;
		_active = true;
		if (_score > _bestScore)
		{
			_bestScore = _score;
			SaveBestScore(_bestScore);
		}
		_bestLabel!.Text = $"Рекорд  {_bestScore}";
		_gameOverScore!.Text = $"Счёт: {_score}";
		_gameOverRoot!.Visible = true;
	}

	private void Restart()
	{
		Open(_startPool);
	}

	private void ClearPieces()
	{
		foreach (Piece piece in _pieces) piece.View.QueueFree();
		_pieces.Clear();
	}

	private static int LoadBestScore()
	{
		var config = new ConfigFile();
		if (config.Load("user://save.cfg") != Error.Ok) return 0;
		return (int)config.GetValue("suika", "high", 0);
	}

	private static void SaveBestScore(int score)
	{
		var config = new ConfigFile();
		config.Load("user://save.cfg");
		config.SetValue("suika", "high", score);
		config.Save("user://save.cfg");
	}
}
