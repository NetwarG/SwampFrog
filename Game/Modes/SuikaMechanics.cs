using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Способности Suika: разблокировка по очкам, подсказки, предохранитель от вылета фруктов.
/// Часть класса SuikaGame: см. SuikaShake.cs, SuikaBomb.cs и SuikaShrink.cs.
/// </summary>
public partial class SuikaGame
{
	private readonly SuikaAbilities _abilities = new();
	private readonly SuikaShakeInput _shakeInput = new();
	private float _abilityGuard;
	private float _abilityUnlockTimer;
	private string _abilityUnlockText = string.Empty;
	private bool _hintDismissed;
	private int _lastAbilityScore = -1;

	/// <summary>Состояние способностей: нужно интерфейсу и проверкам.</summary>
	public SuikaAbilities Abilities => _abilities;

	/// <summary>Насколько верхний фрукт вышел за кромку кувшина (0 — всё внутри).</summary>
	public float FruitsAboveRim
	{
		get
		{
			float rim = _bowl.Position.Y;
			float worst = 0f;
			foreach (Piece piece in _pieces)
			{
				if (piece.Removed || piece.IsBomb) continue;
				float top = piece.Body.Position.Y - ShapeExtent(piece, Vector2.Up);
				worst = Mathf.Max(worst, rim - top);
			}
			return worst;
		}
	}

	/// <summary>Наименьший радиус фрукта в кувшине: диагностика уменьшения.</summary>
	public float MinimumFruitRadius
	{
		get
		{
			float smallest = float.MaxValue;
			foreach (Piece piece in _pieces)
			{
				if (piece.Removed || piece.IsBomb) continue;
				smallest = Mathf.Min(smallest, piece.Radius);
			}
			return smallest == float.MaxValue ? 0f : smallest;
		}
	}

	/// <summary>Число фруктов в кувшине без бомб.</summary>
	public int FruitCount
	{
		get
		{
			int count = 0;
			foreach (Piece piece in _pieces)
				if (!piece.Removed && !piece.IsBomb) count++;
			return count;
		}
	}

	/// <summary>Позиции фруктов в кувшине: диагностика смещения от способностей.</summary>
	public Vector2[] CaptureFruitPositions()
	{
		var positions = new List<Vector2>();
		foreach (Piece piece in _pieces)
			if (!piece.Removed && !piece.IsBomb) positions.Add(piece.Body.Position);
		return positions.ToArray();
	}

	/// <summary>Сбрасывает способности на новый раунд.</summary>
	private void ResetMechanics()
	{
		_abilities.Reset();
		_shakeInput.Cancel();
		_shakeJoltsLeft = 0;
		_shakeSwipeActive = false;
		_bombArmed = false;
		_abilityGuard = 0f;
		_abilityUnlockTimer = 0f;
		_abilityUnlockText = string.Empty;
		_hintDismissed = false;
		_lastAbilityScore = -1;
		BlastCount = 0;
		UpdateBombVisuals();
		UpdateAbilityButtons();
		UpdateHint();
	}

	/// <summary>Снимает активные эффекты при завершении раунда и выходе.</summary>
	private void CancelMechanics()
	{
		_shakeInput.Cancel();
		_shakeJoltsLeft = 0;
		_shakeSwipeActive = false;
		_bombArmed = false;
		_abilityGuard = 0f;
		UpdateBombVisuals();
		UpdateAbilityButtons();
		UpdateHint();
	}

	/// <summary>Начисляет заряды по накопленным очкам раунда.</summary>
	private void SyncAbilities()
	{
		int granted = _abilities.SetScore(_score);
		if (granted != 0) OnChargesGranted(granted);
		// Состояние могли изменить и снаружи (например, проверка или будущий мета-бонус):
		// кнопки обязаны догнать счёт, даже если новых зарядов в этом кадре нет.
		if (_abilities.Score == _lastAbilityScore) return;
		_lastAbilityScore = _abilities.Score;
		UpdateAbilityButtons();
	}

	/// <summary>Подсказка и звук при начислении заряда способности.</summary>
	private void OnChargesGranted(int mask)
	{
		var names = new System.Collections.Generic.List<string>();
		foreach (SuikaAbility ability in new[]
			{ SuikaAbility.Shake, SuikaAbility.Bomb, SuikaAbility.Shrink })
		{
			if ((mask & (1 << (int)ability)) == 0) continue;
			names.Add(Loc.T(AbilityNameKey(ability)));
		}
		if (names.Count == 0) return;
		_abilityUnlockText = names.Count == 1
			? Loc.T(L.SuikaAbilityReady, names[0])
			: Loc.T(L.SuikaAbilitiesReady);
		_abilityUnlockTimer = 2f;
		Sfx.Play(SfxId.LevelUp);
		UpdateAbilityButtons();
		UpdateHint();
	}

	/// <summary>Гасит подсказку о разблокировке по времени.</summary>
	private void UpdateAbilityHint(float dt)
	{
		if (_abilityUnlockTimer <= 0f) return;
		_abilityUnlockTimer = Mathf.Max(0f, _abilityUnlockTimer - dt);
		if (_abilityUnlockTimer <= 0f) UpdateHint();
	}

	/// <summary>Нижняя подсказка: разблокировка, ожидание тряски, бомба или бросок.</summary>
	private void UpdateHint()
	{
		if (_hintLabel == null) return;
		string text;
		if (_abilityUnlockTimer > 0f && _abilityUnlockText.Length > 0)
			text = _abilityUnlockText;
		else if (_shakeInput.Armed)
			text = Loc.T(L.SuikaShakeHint);
		else if (_bombArmed)
			text = Loc.T(L.SuikaBombHint);
		else
			text = Loc.T(L.SuikaHintDrop);
		_hintLabel.Text = text;
		_hintLabel.Visible = !_stockExhausted
			&& (!_hintDismissed || _shakeInput.Armed || _bombArmed || _abilityUnlockTimer > 0f);
	}

	/// <summary>Перерисовывает три кнопки способностей по текущему состоянию.</summary>
	private void UpdateAbilityButtons()
	{
		UpdateAbilityButton(_shakeButton, SuikaAbility.Shake);
		UpdateAbilityButton(_bombButton, SuikaAbility.Bomb);
		UpdateAbilityButton(_shrinkButton, SuikaAbility.Shrink);
		if (_shakeButton != null) _shakeButton.Armed = _shakeInput.Armed;
		if (_bombButton != null) _bombButton.Selected = _bombArmed;
	}

	private void UpdateAbilityButton(ChargeCircleButton? button, SuikaAbility ability)
	{
		if (button == null) return;
		int charges = _abilities.Charges(ability);
		button.SetState(charges > 0, charges, _abilities.Progress(ability));
		button.Visible = _active && !_gameOver && !_helpOpen;
	}

	/// <summary>
	/// После способности фрукты не поднимаются выше кромки: позиция подтягивается вниз,
	/// скорость вверх гасится. Брошенный только что фрукт не трогаем — он ещё летит сверху.
	/// </summary>
	private void UpdateGuard(float dt)
	{
		if (_abilityGuard <= 0f) return;
		_abilityGuard = Mathf.Max(0f, _abilityGuard - dt);
		float rim = _bowl.Position.Y + SuikaMechanicsTuning.AbilityGuardMargin;
		foreach (Piece piece in _pieces)
		{
			if (piece.Removed || piece.IsBomb) continue;
			if (piece.Age < 0.45f) continue;
			float extent = ShapeExtent(piece, Vector2.Up);
			float top = piece.Body.Position.Y - extent;
			if (top >= rim) continue;
			piece.Body.Position = new Vector2(piece.Body.Position.X,
				Mathf.Max(piece.Body.Position.Y, rim + extent));
			Vector2 velocity = piece.Body.LinearVelocity;
			if (velocity.Y < 0f)
				piece.Body.LinearVelocity = new Vector2(velocity.X, 0f);
			piece.Body.Sleeping = false;
			piece.Body.ResetPhysicsInterpolation();
		}
	}

	/// <summary>Ключ названия способности для подсказок.</summary>
	private static string AbilityNameKey(SuikaAbility ability) => ability switch
	{
		SuikaAbility.Shake => L.SuikaShake,
		SuikaAbility.Bomb => L.SuikaBomb,
		_ => L.SuikaShrink
	};
}
