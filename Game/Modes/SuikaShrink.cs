using Godot;

namespace SwampFrog;

/// <summary>
/// Уменьшение фруктов: все плоды в кувшине становятся на 20% меньше.
/// Часть класса SuikaGame: см. SuikaMechanics.cs и SuikaPile.cs.
/// </summary>
public partial class SuikaGame
{
	/// <summary>Кнопка «Уменьшить»: сжимает фрукты, которые уже лежат в кувшине.</summary>
	private void UseShrink()
	{
		if (!_active || _gameOver || _helpOpen || _stockExhausted) return;
		if (!_abilities.TrySpend(SuikaAbility.Shrink)) return;
		_bombArmed = false;
		foreach (Piece piece in _pieces)
		{
			if (piece.Removed || piece.IsBomb) continue;
			ResizePiece(piece, piece.Radius * SuikaMechanicsTuning.ShrinkFactor);
		}
		_abilityGuard = SuikaMechanicsTuning.AbilityGuardTime;
		_settleTime = 0f;
		_completionTimer = 0f;
		Sfx.Play(SfxId.SuikaShrink);
		UpdateBombVisuals();
		UpdateHint();
		UpdateAbilityButtons();
	}

	/// <summary>Меняет размер куска: силуэт тела, масса и картинка.</summary>
	private void ResizePiece(Piece piece, float radius)
	{
		piece.Radius = radius;
		if (piece.Collider != null) piece.Collider.Shape = piece.Shape.Shape(radius);
		piece.Body.Mass = Mathf.Pow(radius / FruitRadius(FruitKind.Cherry), MassExponent);
		if (piece.Visual != null)
			piece.Visual.Scale = Vector2.One * (radius / FruitCatalog.Get(piece.Kind).BaseRadius);
		piece.Body.Sleeping = false;
		piece.Body.ResetPhysicsInterpolation();
	}
}
