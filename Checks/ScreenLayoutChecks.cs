using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace SwampFrog.Checks;

/// <summary>Настоящие сцены и Rapier. Награды/расход запаса не вызываются: сохранение игрока не меняется.</summary>
public partial class ScreenLayoutChecks : Node
{
	private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
	private GameScreen _screen = null!;
	private Main Game => _screen.Game;
	private Frog Frog => Game.FrogNode!;
	private int _checks;
	private readonly Dictionary<int, Vector2> _pointers = new();
	private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target)!;
	private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private)!.SetValue(target, value);
	private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private)!.Invoke(target, args)!;
	private void Check(bool condition, string message)
	{
		if (!condition) throw new InvalidOperationException(message);
		_checks++;
	}
	private async Task Frames(int count = 3)
	{
		for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}
	private void Touch(Vector2 position, bool pressed, int index = 0, bool canceled = false)
	{
		_pointers[index] = position;
		using var input = new InputEventScreenTouch { Position = position, Pressed = pressed, Index = index, Canceled = canceled };
		GetViewport().PushInput(input, true);
	}
	private void Drag(Vector2 position, int index = 0)
	{
		Vector2 relative = position - _pointers.GetValueOrDefault(index, position);
		_pointers[index] = position;
		using var input = new InputEventScreenDrag { Position = position, Index = index, Relative = relative, Velocity = relative * 60f };
		GetViewport().PushInput(input, true);
	}
	private async Task Capture(string name)
	{
		if (!OS.GetCmdlineUserArgs().Contains("--screenshots")) return;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng("res://.godot/" + name + ".png");
	}
	private void CheckPerksFit(PerkSelectionOverlay overlay, int count)
	{
		Check(overlay.FindChildren("*", "ScrollContainer", true, false).Count == 0, "Skills must not contain a scroll container");
		VBoxContainer content = Field<VBoxContainer>(overlay, "_box");
		Transform2D toOverlay = overlay.GetGlobalTransformWithCanvas().AffineInverse();
		Rect2 bounds = new Rect2(Vector2.Zero, overlay.Size).Grow(0.1f);
		Rect2 contentRect = toOverlay * content.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, content.Size);
		Check(bounds.Encloses(contentRect), $"All skill content must fit the safe area: {contentRect} in {bounds}");
		Check(contentRect.GetCenter().DistanceTo(bounds.GetCenter()) < 1f, "Skill content must be centered horizontally and vertically");
		HoldCardButton[] cards = content.GetChildren().OfType<HoldCardButton>().ToArray();
		Check(cards.Length == count, "All offered skills must be shown together");
		foreach (Control child in content.GetChildren().OfType<Control>())
		{
			Rect2 rect = toOverlay * child.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, child.Size);
			Check(child.IsVisibleInTree() && bounds.Encloses(rect), "Every card and caption must be fully visible");
			Vector2 required = child.GetCombinedMinimumSize();
			Check(child.Size.X + 0.1f >= required.X && child.Size.Y + 0.1f >= required.Y, "Text must retain its full natural height without clipping");
		}
	}

	public override void _Ready() => Callable.From(Run).CallDeferred();

	private async void Run()
	{
		try
		{
			_screen = GD.Load<PackedScene>("res://Scenes/GameScreen.tscn").Instantiate<GameScreen>();
			AddChild(_screen);
			await Frames();
			Check(Game.ViewSize == Playfield.Size, "World must use 540x960 logical units");
			var award = (Action<FallingItem>)Delegate.CreateDelegate(typeof(Action<FallingItem>), Game,
				typeof(Main).GetMethod("OnCaughtItemReturned", Private)!);
			Frog.CaughtItemReturned -= award;
			Frog.CaughtItemReturned += item => item.Despawn();
			Set(Game, "_highScore", int.MaxValue);
			var menu = Field<Control>(Game.HudNode!, "_startRoot");
			await Capture("layout-menu");
			Button play = menu.FindChildren("*", "Button", true, false).Cast<Button>().Single(b => b.Text == "Играть");
			Vector2 playCenter = play.GetGlobalTransformWithCanvas() * (play.Size * 0.5f);
			Touch(playCenter, true);
			Touch(playCenter, false);
			Check(Game.IsPlaying, "The start button must respond to real touch input");
			Field<Timer>(Game, "_spawnTimer").Stop();
			Game.SetPhysicsProcess(false);
			var fruit = new FallingItem { Kind = FruitKind.Pear, Position = new Vector2(280, 250), FallSpeed = 150 };
			Game.ItemsNode!.AddChild(fruit);
			var body = fruit.GetNode<RigidBody2D>("PhysicsBody");
			fruit.Frozen = true;
			var shape = body.GetChild<CollisionShape2D>(0).Shape;
			var suika = Game.GetNode<SuikaGame>("Suika");
			Rect2 bowl = Field<Rect2>(suika, "_bowl");
			Set(suika, "_bestScore", int.MaxValue);
			object piece = Invoke(suika, "CreatePiece", FruitKind.Watermelon, new Vector2(270, 650), Vector2.Zero);
			var stacked = (RigidBody2D)piece.GetType().GetField("Body")!.GetValue(piece)!;
			stacked.Freeze = true;
			Vector2 initialFruit = body.Position, initialStack = stacked.Position, initialFrog = Frog.Position;
			Vector2[]? baselineHands = null;
			var sizes = new[] { new Vector2I(360, 640), new Vector2I(390, 844), new Vector2I(360, 800),
				new Vector2I(768, 1024), new Vector2I(800, 1200), new Vector2I(320, 568), new Vector2I(1280, 720) };
			foreach (Vector2I size in sizes)
			{
				GetWindow().Size = size;
				await Frames();
				_screen.RefreshLayout();
				Check(Game.ViewSize == Playfield.Size && Frog.Position == initialFrog, "Resize cannot reposition the frog");
				Check(body.Position == initialFruit && body.GetChild<CollisionShape2D>(0).Shape == shape,
					"Resize cannot move or resize a classic body");
				Check(stacked.Position == initialStack && Field<Rect2>(suika, "_bowl") == bowl,
					"Resize cannot change the bowl or its contents");
				Check(Mathf.IsEqualApprox(_screen.FieldRect.Size.X / _screen.FieldRect.Size.Y, 540f / 960f), "Field aspect must be constant");
				Check(new Rect2(Vector2.Zero, _screen.Size).Encloses(_screen.FieldRect), "Field must fit inside the screen");
				Frog.CancelInput();
				Frog.UpdateArms(2f);
				Vector2 target = new(500, 160);
				Touch(_screen.WorldToScreen(target), true);
				Frog.UpdateArms(0.25f);
				Check(Frog.IsCatching, "Mapped touch must extend the hands");
				Vector2[] hands = Frog.GetHandPositions();
				if (baselineHands == null) baselineHands = hands;
				else Check(hands[0].DistanceTo(baselineHands[0]) < 0.01f && hands[1].DistanceTo(baselineHands[1]) < 0.01f,
					"The same touch and elapsed time must produce the same hand positions on every screen");
				Touch(_screen.WorldToScreen(new Vector2(30, 700)), true, 1);
				Drag(_screen.WorldToScreen(new Vector2(60, 800)), 1);
				Touch(Vector2.Zero, false, 1);
				Check(Field<bool>(Frog, "_holding") && Field<Vector2>(Frog, "_aimTarget").DistanceTo(target) < 0.01f,
					"A second finger must not change or release the active gesture");
				Touch(new Vector2(-100, -100), false);
				Check(!Field<bool>(Frog, "_holding"), "Releasing outside the field must end the gesture");
				Frog.UpdateArms(2f);
				Touch(_screen.WorldToScreen(target), true);
				Touch(_screen.WorldToScreen(target), false, canceled: true);
				Check(!Field<bool>(Frog, "_holding"), "Canceled touches must release the hands");
				if (size.X == 768) await Capture("layout-tablet");
				fruit.Frozen = false;
				body.LinearVelocity = new Vector2(0, 150);
				body.AngularVelocity = 0;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				ulong tick = Engine.GetPhysicsFrames();
				float y = body.Position.Y;
				await Frames(12);
				float elapsed = (Engine.GetPhysicsFrames() - tick) / (float)Engine.PhysicsTicksPerSecond;
				Check(Mathf.Abs(body.Position.Y - y - elapsed * 150f) < 5.1f, $"Native fall speed must be independent of resolution and render FPS: {size}, delta={body.Position.Y-y}, expected={elapsed*150}, velocity={body.LinearVelocity}, frozen={body.Freeze}");
				fruit.Frozen = true;
				await Frames(2);
				initialFruit = body.Position;
				fruit.SyncFromPhysics();
			}
			// Изменяем окно прямо во время удержания: длины рук и цель остаются в мире.
			Vector2 liveTarget = new(500, 130);
			Touch(_screen.WorldToScreen(liveTarget), true);
			Frog.UpdateArms(0.3f);
			Vector2[] beforeResize = Frog.GetHandPositions();
			GetWindow().Size = new Vector2I(360, 800);
			await Frames();
			Check(Frog.GetHandPositions().SequenceEqual(beforeResize) && Field<Vector2>(Frog, "_aimTarget").DistanceTo(liveTarget) < 0.01f,
				"An active gesture must keep its lengths and aim when the display changes");
			bool independent = false;
			Vector2[] previous = beforeResize;
			for (int i = 0; i < 100; i++)
			{
				Frog.UpdateArms(0.01f);
				Vector2[] hands = Frog.GetHandPositions();
				independent |= (hands[0].IsEqualApprox(previous[0]) && hands[1].DistanceTo(previous[1]) > 1f)
					|| (hands[1].IsEqualApprox(previous[1]) && hands[0].DistanceTo(previous[0]) > 1f);
				foreach (Vector2 hand in hands)
					Check(new Rect2(new Vector2(24, 24), Playfield.Size - new Vector2(48, 48)).Grow(0.01f).HasPoint(hand),
						"Each hand must stop inside the visible playfield");
				previous = hands;
			}
			Check(independent, "One hand reaching the field edge must not stop the other");
			Touch(Vector2.Zero, false);
			Frog.UpdateArms(2f);

			GetWindow().Size = new Vector2I(390, 844);
			await Frames();
			_screen.SafeAreaOverride = new Rect2(18, 54, _screen.Size.X - 36, _screen.Size.Y - 96);
			_screen.RefreshLayout();
			await Frames();
			Check(_screen.SafeAreaOverride.Value.Encloses(_screen.FieldRect), "Field must avoid camera and system bars");
			Check(_screen.UiRoot.Position == _screen.SafeAreaOverride.Value.Position, "UI must use the same safe area");
			Vector2 popupAt = new(270, 480);
			Game.HudNode!.SpawnPopup(popupAt, "+10");
			Label popup = Field<Control>(Game.HudNode, "_popups").GetChildren().OfType<Label>().Last();
			Check((popup.GetGlobalTransformWithCanvas() * Vector2.Zero).DistanceTo(_screen.WorldToScreen(popupAt)) < 0.01f,
				"World popups must align with fruit after scaling and safe-area offsets");
			await Capture("layout-classic-safe-area");
			Game.StartPerkPause();
			var offers = PerkCatalog.All.Take(3).Select(p => new PerkOffer(p.Id, p.Name, p.Tagline, 0, p.LevelEffects[0], p.MaxLevel, p.Accent)).ToArray();
			Game.HudNode.ShowPerkSelection(2, offers);
			await Frames();
			var overlay = Field<PerkSelectionOverlay>(Game.HudNode, "_perkOverlay");
			VBoxContainer box = Field<VBoxContainer>(overlay, "_box");
			Check(Mathf.Abs((overlay.GetGlobalTransform().AffineInverse() * box.GlobalPosition).X + box.Size.X * 0.5f - overlay.Size.X * 0.5f) < 2f,
				"Perk selection must be centered inside safe UI");
			await Capture("layout-perks");
			var card = box.GetChildren().OfType<HoldCardButton>().First();
			Vector2 cardPoint = card.GetGlobalTransformWithCanvas() * (card.Size * 0.5f);
			Touch(cardPoint, true);
			await Frames(4);
			Check(card.HoldProgress > 0, "A scaled perk card must accept touch while paused");
			float progress = card.HoldProgress;
			Drag(cardPoint + new Vector2(0, 70));
			await Frames(4);
			Check(card.HoldProgress > progress, "Moving a finger inside the card must preserve and advance confirmation");
			Touch(cardPoint, false);
			Check(card.HoldProgress == 0, "Releasing a finger must cancel incomplete confirmation");
			GetViewport().NotifyMouseEntered();
			using (var down = new InputEventMouseButton { Position = cardPoint, GlobalPosition = cardPoint, ButtonIndex = MouseButton.Left, Pressed = true })
				GetViewport().PushInput(down, true);
			await Frames(4);
			progress = card.HoldProgress;
			Check(progress > 0, "Mouse press must start confirmation");
			Vector2 moved = card.GetGlobalTransformWithCanvas() * (card.Size * new Vector2(0.75f, 0.65f));
			using (var motion = new InputEventMouseMotion { Position = moved, GlobalPosition = moved, Relative = moved - cardPoint, ButtonMask = MouseButtonMask.Left })
				GetViewport().PushInput(motion, true);
			await Frames(4);
			Check(card.HoldProgress > progress, "Moving the mouse inside the card must preserve and advance confirmation");
			using (var up = new InputEventMouseButton { Position = moved, GlobalPosition = moved, ButtonIndex = MouseButton.Left, Pressed = false })
				GetViewport().PushInput(up, true);
			Check(card.HoldProgress == 0, "Releasing the mouse must cancel incomplete confirmation");
			var longOffers = offers.Select(o => o with { NextLevelEffect = string.Join(" ", Enumerable.Repeat(o.NextLevelEffect, 16)) }).ToArray();
			Game.HudNode.ShowPerkSelection(3, longOffers);
			await Frames();
			CheckPerksFit(overlay, 3);
			Check(box.Scale.X < 1f, "Long descriptions must fit by scaling the entire content");
			Vector2 fixedPosition = box.Position;
			card = box.GetChildren().OfType<HoldCardButton>().First();
			cardPoint = card.GetGlobalTransformWithCanvas() * (card.Size * 0.5f);
			Touch(cardPoint, true);
			Drag(cardPoint - new Vector2(0, 80));
			Touch(cardPoint - new Vector2(0, 80), false);
			Check(box.Position == fixedPosition && card.HoldProgress == 0, "Dragging must not scroll skills; releasing must cancel incomplete confirmation");
			await Capture("layout-perks-long");

			// Каждый реальный текст уровня, а также окна с одним и двумя доступными навыками.
			PerkOffer[] allOffers = PerkCatalog.All.SelectMany(p => p.LevelEffects.Select((effect, level) =>
				new PerkOffer(p.Id, p.Name, p.Tagline, level, effect, p.MaxLevel, p.Accent))).ToArray();
			_screen.SafeAreaOverride = null;
			foreach (Vector2I size in sizes)
			{
				GetWindow().Size = size;
				await Frames();
				foreach (PerkOffer[] batch in allOffers.Chunk(3))
				{
					Game.HudNode.ShowPerkSelection(99, batch);
					await Frames(4);
					CheckPerksFit(overlay, batch.Length);
				}
				Game.HudNode.ShowPerkSelection(2, offers.Take(1).ToArray());
				await Frames(4);
				CheckPerksFit(overlay, 1);
				Game.HudNode.ShowPerkSelection(2, offers.Take(2).ToArray());
				await Frames(4);
				CheckPerksFit(overlay, 2);
				Game.HudNode.ShowPerkSelection(2, offers);
				await Frames(4);
				CheckPerksFit(overlay, 3);
				if (size.X == 320 || size.X == 768) await Capture($"layout-perks-{size.X}");
			}
			// Изменение окна, когда выбор уже открыт, не требует повторного Show.
			GetWindow().Size = new Vector2I(390, 844);
			await Frames(4);
			_screen.SafeAreaOverride = new Rect2(18, 54, _screen.Size.X - 36, _screen.Size.Y - 96);
			_screen.RefreshLayout();
			await Frames(4);
			CheckPerksFit(overlay, 3);
			await Capture("layout-perks-safe-area");
			Game.HudNode.HidePerkSelection();
			Game.EndPerkPause();
			Field<Timer>(Game, "_spawnTimer").Stop();
			Invoke(suika, "ClearPieces");
			Game.HudNode.SetGameplayVisible(false);
			Frog.Visible = false;
			suika.Open();
			Invoke(suika, "StartFromStockDialog");
			Set(Game, "_state", GameState.Suika);
			Set(suika, "_dropCooldown", 100f); // Проверяем жесты без расхода запаса игрока.
			Vector2 suikaTarget = _screen.WorldToScreen(new Vector2(380, 400));
			Touch(suikaTarget, true);
			Touch(_screen.WorldToScreen(new Vector2(100, 400)), true, 1);
			Touch(Vector2.Zero, false, 1);
			Check(Field<int>(suika, "_pointerIndex") == 0 && Mathf.IsEqualApprox(Field<float>(suika, "_aimX"), 380),
				"Suika must track only the active finger");
			Touch(suikaTarget, false, canceled: true);
			Check(Field<int>(suika, "_pointerIndex") == -1, "Canceled Suika touches must not remain active");
			await Frames();
			await Capture("layout-suika");
			GD.Print($"SCREEN LAYOUT CHECKS PASSED: {_checks}");
			GetTree().Quit();
		}
		catch (Exception error)
		{
			GD.PushError(error.ToString());
			GetTree().Paused = false;
			GetTree().Quit(1);
		}
	}
}
