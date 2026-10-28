using Godot;

namespace SwampFrog;

/// <summary>
/// Фрукты в кувшине: тела по силуэту иконки, слияния и размещение в кувшине.
/// Часть класса SuikaGame: см. SuikaGame.cs (раунд и ввод) и SuikaUi.cs (интерфейс).
/// </summary>
public partial class SuikaGame
{
	private sealed class Piece
	{
		public required RigidBody2D Body;
		public FruitKind Kind;
		/// <summary>Радиус куска: форма тела повторяет силуэт иконки (см. FruitShape).</summary>
		public float Radius;
		/// <summary>Силуэт иконки: задаёт и форму тела, и её проекции.</summary>
		public FruitShape Shape = null!;
		public float Age;
		public bool Removed;
	}

	private Piece CreatePiece(FruitKind kind, Vector2 position, Vector2 velocity)
	{
		float radius = FruitRadius(kind);
		FruitShape shape = FruitShape.Of(kind);
		var body = new RigidBody2D
		{
			Name = $"Fruit_{kind}",
			CollisionLayer = SuikaCollisionLayer,
			CollisionMask = SuikaCollisionLayer,
			Mass = Mathf.Pow(radius / FruitRadius(FruitKind.Cherry), MassExponent),
			PhysicsMaterialOverride = _fruitMaterial,
			LinearDampMode = RigidBody2D.DampMode.Replace,
			LinearDamp = FruitLinearDamp,
			AngularDampMode = RigidBody2D.DampMode.Replace,
			AngularDamp = FruitAngularDamp,
			ContinuousCd = RigidBody2D.CcdMode.CastShape,
			ContactMonitor = true,
			MaxContactsReported = 16,
			CanSleep = true,
			LinearVelocity = velocity
		};
		var piece = new Piece { Body = body, Kind = kind, Radius = radius, Shape = shape };
		body.AddChild(new CollisionShape2D { Shape = shape.Shape(radius) });
		body.Position = FitPieceInsideBowl(piece, position);
		body.BodyEntered += other => QueueMerge(piece, other);
		_pieces.Add(piece);
		_piecesByBody.Add(body.GetInstanceId(), piece);
		AddChild(body);
		CreateVisualFruit(kind, radius, body);
		return piece;
	}

	private FallingItem CreateVisualFruit(FruitKind kind, float radius, Node2D? parent = null)
	{
		var item = new FallingItem { ItemType = ItemType.Fruit, Kind = kind, ManualPhysics = true };
		(parent ?? this).AddChild(item);
		float baseRadius = FruitCatalog.Get(kind).BaseRadius;
		item.Scale = Vector2.One * (radius / baseRadius);
		return item;
	}

	private float FruitRadius(FruitKind kind)
	{
		int order = (int)kind;
		// Базовый радиус куска (вишня) вдвое больше прежнего (16): фрукты в Suika
		// крупные, поэтому корзина вмещает заметно меньше плодов.
		return 32f * Mathf.Pow(1.18f, order);
	}

	/// <summary>Проекция повёрнутой формы на ось: для линии переполнения и размещения новых тел.</summary>
	private static float ShapeExtent(Piece piece, Vector2 axis) =>
		piece.Shape.ExtentAlong(axis, piece.Body.Rotation, piece.Radius);

	/// <summary>Размещение при создании/ресайзе; текущие столкновения решает только физический сервер.</summary>
	private Vector2 FitPieceInsideBowl(Piece piece, Vector2 position)
	{
		position.Y = Mathf.Min(position.Y, _bowl.End.Y - FloorInset - ShapeExtent(piece, Vector2.Down) - 1f);
		float left = _bowl.Position.X + ShapeExtent(piece, Vector2.Right) + 1f;
		float right = _bowl.End.X - ShapeExtent(piece, Vector2.Right) - 1f;
		position.X = left <= right ? Mathf.Clamp(position.X, left, right) : _bowl.GetCenter().X;
		return position;
	}

	/// <summary>
	/// Ограничивает горизонтальную точку броска стенами кувшина с учётом
	/// горизонтальной полуоси фрукта. Если фрукт шире кувшина, цель — в середине.
	/// </summary>
	private float ClampDropX(float x, float radius)
	{
		float left = _bowl.Position.X + radius + 4f;
		float right = _bowl.End.X - radius - 4f;
		if (left > right)
			return _bowl.Position.X + _bowl.Size.X * 0.5f;
		return Mathf.Clamp(x, left, right);
	}

	private void QueueMerge(Piece piece, Node other)
	{
		if (!_active || _gameOver || piece.Removed || piece.Kind == FruitKind.Watermelon) return;
		if (!_piecesByBody.TryGetValue(other.GetInstanceId(), out Piece? partner)
			|| partner.Removed || partner.Kind != piece.Kind) return;

		// Достаточно сигнала от любого тела. Повторные записи отсекаются флагом Removed.
		_pendingMerges.Enqueue((piece, partner));
	}

	private void MergeContactingPieces()
	{
		while (_pendingMerges.TryDequeue(out var pair))
		{
			Piece a = pair.A;
			Piece b = pair.B;
			// Одновременное касание трёх фруктов не должно использовать один фрукт дважды.
			if (a.Removed || b.Removed) continue;
			FruitKind mergedKind = (FruitKind)((int)a.Kind + 1);
			Vector2 position = (a.Body.Position + b.Body.Position) * 0.5f;
			Vector2 velocity = (a.Body.LinearVelocity + b.Body.LinearVelocity) * 0.5f;
			RemovePiece(a);
			RemovePiece(b);
			CreatePiece(mergedKind, position, velocity * 0.45f);
			_settleTime = 0f;
			_score += 10 * ((int)mergedKind + 1);
			_scoreLabel!.Text = $"Счёт  {_score}";
		}
	}

	private void RemovePiece(Piece piece)
	{
		piece.Removed = true;
		_pieces.Remove(piece);
		_piecesByBody.Remove(piece.Body.GetInstanceId());
		// QueueFree отложен до конца кадра: сразу исключаем старые тела из столкновений.
		piece.Body.CollisionLayer = 0;
		piece.Body.CollisionMask = 0;
		piece.Body.Freeze = true;
		piece.Body.Hide();
		piece.Body.QueueFree();
	}

	private void ClearPieces()
	{
		for (int i = _pieces.Count - 1; i >= 0; i--) RemovePiece(_pieces[i]);
		_pendingMerges.Clear();
		_piecesByBody.Clear();
	}
}
