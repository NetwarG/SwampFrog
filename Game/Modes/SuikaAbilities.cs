using System;

namespace SwampFrog;

/// <summary>Эффект-способность мини-игры Suika.</summary>
public enum SuikaAbility
{
	Shake,
	Bomb,
	Shrink
}

/// <summary>
/// Заряды способностей: каждый заряд копится по очкам раунда заново, в том числе после траты.
/// Пока зарядов нет, кнопка серая; максимум зарядов задаётся настройками.
/// </summary>
public sealed class SuikaAbilities
{
	private const int Count = 3;

	private readonly int[] _charges = new int[Count];
	/// <summary>Счёт, с которого копится текущий заряд.</summary>
	private readonly int[] _meter = new int[Count];
	private int _score;

	/// <summary>Наибольший счёт раунда.</summary>
	public int Score => _score;

	/// <summary>Сколько очков раунда стоит один заряд.</summary>
	public static int ChargeScore(SuikaAbility ability) => ability switch
	{
		SuikaAbility.Shake => SuikaMechanicsTuning.ShakeChargeScore,
		SuikaAbility.Bomb => SuikaMechanicsTuning.BombChargeScore,
		_ => SuikaMechanicsTuning.ShrinkChargeScore
	};

	/// <summary>Сколько зарядов можно держать одновременно.</summary>
	public static int MaxCharges(SuikaAbility ability) => ability switch
	{
		SuikaAbility.Shake => SuikaMechanicsTuning.ShakeCharges,
		SuikaAbility.Bomb => SuikaMechanicsTuning.BombCharges,
		_ => SuikaMechanicsTuning.ShrinkCharges
	};

	/// <summary>Остаток зарядов способности.</summary>
	public int Charges(SuikaAbility ability) => _charges[(int)ability];

	/// <summary>Есть ли готовый заряд.</summary>
	public bool IsReady(SuikaAbility ability) => _charges[(int)ability] > 0;

	/// <summary>Прогресс накопления следующего заряда 0..1 (0, когда заряды на максимуме).</summary>
	public float Progress(SuikaAbility ability)
	{
		int index = (int)ability;
		if (_charges[index] >= MaxCharges(ability)) return 0f;
		return Math.Clamp((float)(_score - _meter[index]) / ChargeScore(ability), 0f, 1f);
	}

	/// <summary>
	/// Обновляет счёт (только вверх) и выдаёт заряды, накопленные по очкам.
	/// Возвращает битовую маску способностей, получивших заряд в этом вызове.
	/// </summary>
	public int SetScore(int score)
	{
		if (score > _score) _score = score;
		int granted = 0;
		for (int i = 0; i < Count; i++)
		{
			SuikaAbility ability = (SuikaAbility)i;
			int cost = ChargeScore(ability);
			while (_charges[i] < MaxCharges(ability) && _score - _meter[i] >= cost)
			{
				_charges[i]++;
				_meter[i] += cost;
				granted |= 1 << i;
			}
		}
		return granted;
	}

	/// <summary>Тратит заряд: следующий снова копится с текущего счёта.</summary>
	public bool TrySpend(SuikaAbility ability)
	{
		int index = (int)ability;
		if (_charges[index] <= 0) return false;
		_charges[index]--;
		_meter[index] = _score;
		return true;
	}

	/// <summary>Сбрасывает заряды и накопление на новый раунд.</summary>
	public void Reset()
	{
		Array.Clear(_charges, 0, Count);
		Array.Clear(_meter, 0, Count);
		_score = 0;
	}
}
