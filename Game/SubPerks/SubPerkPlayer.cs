using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Рантайм-логика субперков: очередь предложений, случайный прирост 1–5% и выбор.
/// Мир читается через публичные доступы <see cref="Main"/>, «чистая» математика — в
/// <see cref="SubPerkEffects"/>. Субперки не имеют максимального уровня: чем выше
/// уровень жабы, тем реже они предлагаются (см. <see cref="SubPerkState"/>).
/// </summary>
public sealed class SubPerkPlayer
{
	/// <summary>Ссылка на корень игры (устанавливается из Main._Ready).</summary>
	public Main? Game { get; set; }

	private readonly SubPerkState _state = new();

	/// <summary>Показанный прирост каждой карточки текущего предложения.</summary>
	private readonly Dictionary<SubPerkId, int> _offerGains = new();

	/// <summary>Состояние субперков (уровни, проценты и очередь выдачи).</summary>
	public SubPerkState State => _state;

	// ---------- Сброс забега ----------

	public void Reset()
	{
		_state.Reset();
		_offerGains.Clear();
	}

	// ---------- Очередь предложений ----------

	/// <summary>Обработка уровня жабы: субперк предлагается всё реже по мере роста уровня.</summary>
	public void OnLevelUp(int newLevel)
	{
		TryOffer(newLevel);
	}

	/// <summary>
	/// Повторная проверка очереди: нужна, если предложение не открылось сразу —
	/// например, когда опыт поднял уровень основного перка и игра уже была на паузе.
	/// </summary>
	public void TryOfferPending()
	{
		if (Game != null)
			TryOffer(Game.Xp.Level);
	}

	private void TryOffer(int level)
	{
		if (Game == null || !Game.IsPlaying)
			return;
		if (!_state.WantsOffer(level))
			return;
		OpenSelection(level);
	}

	/// <summary>
	/// Останавливает игру и показывает выбор из трёх случайных субперков одной полярности.
	/// Игра возобновляется только после выбора (OnPicked) — пропустить взятие нельзя.
	/// </summary>
	private void OpenSelection(int level)
	{
		if (Game == null)
			return;
		SubPerkId[] offers = _state.OfferCandidates(3, Game.GameRng);
		if (offers.Length == 0)
			return;

		// Прирост 1–5% бросается сразу и показывается на карточке до выбора.
		_offerGains.Clear();
		var cards = new List<SubPerkOffer>();
		foreach (SubPerkId id in offers)
		{
			SubPerkDefinition def = SubPerkCatalog.Find(id);
			int gain = SubPerkEffects.RollGain(Game.GameRng);
			_offerGains[id] = gain;
			cards.Add(new SubPerkOffer(id, def.Name, def.Tagline, def.Polarity, _state.LevelOf(id), gain, def.Accent));
		}

		Game.StartSubPerkPause();
		Game.HudNode?.ShowSubPerkSelection(level, cards.ToArray(), _state.NextPolarity);
	}

	/// <summary>Игрок выбрал субперк: начисляем уровень с показанным приростом и двигаем очередь.</summary>
	public void OnPicked(SubPerkId id)
	{
		if (Game == null || !Game.IsChoosingSubPerk)
			return;

		int gain = _offerGains.TryGetValue(id, out int rolled) ? rolled : SubPerkEffects.MinGainPercent;
		_state.Increment(id, gain);
		_state.RegisterOfferTaken(Game.Xp.Level);
		_offerGains.Clear();

		Game.HudNode?.HideSubPerkSelection();
		Game.EndSubPerkPause();
	}
}
