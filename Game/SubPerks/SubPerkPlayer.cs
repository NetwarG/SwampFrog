using Godot;

namespace SwampFrog;

/// <summary>
/// Рантайм-логика субперков: субперк выдаётся автоматически в фоне на уровнях
/// предложения — один случайный субперк текущей полярности со случайным приростом 1–5%.
/// Мир читается через публичные доступы <see cref="Main"/>, «чистая» математика — в
/// <see cref="SubPerkEffects"/>. Максимального уровня нет: чем выше уровень жабы,
/// тем реже выдача (см. <see cref="SubPerkState"/>).
/// </summary>
public sealed class SubPerkPlayer
{
	/// <summary>Ссылка на корень игры (устанавливается из Main._Ready).</summary>
	public Main? Game { get; set; }

	private readonly SubPerkState _state = new();

	/// <summary>Состояние субперков (уровни, проценты и очередь выдачи).</summary>
	public SubPerkState State => _state;

	// ---------- Сброс забега ----------

	public void Reset()
	{
		_state.Reset();
	}

	// ---------- Выдача субперков ----------

	/// <summary>Обработка уровня жабы: субперк выдаётся автоматически и всё реже по мере роста уровня.</summary>
	public void OnLevelUp(int newLevel)
	{
		if (Game == null)
			return;
		if (!_state.WantsOffer(newLevel))
			return;

		// Один случайный субперк текущей полярности: без оверлея и без паузы.
		SubPerkId[] pick = _state.OfferCandidates(1, Game.GameRng);
		if (pick.Length == 0)
			return;

		_state.Increment(pick[0], SubPerkEffects.RollGain(Game.GameRng));
		_state.RegisterOfferTaken(newLevel);
	}
}
