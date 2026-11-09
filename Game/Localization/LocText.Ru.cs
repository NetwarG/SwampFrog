using System.Collections.Generic;

namespace SwampFrog;

/// <summary>Русские строки интерфейса (исходный язык). Ключи — константы <see cref="L"/>.</summary>
internal static partial class LocText
{
	internal static readonly Dictionary<string, string> Ru = new()
	{
		[L.MenuSubtitle1] = "Собирай фрукты, не лови мусор!",
		[L.MenuSubtitle2] = "Упустишь фрукт — потеряешь жизнь.",
		[L.MenuPlay] = "Играть",
		[L.MenuExit] = "Выйти",
		[L.MenuMiniGame] = "Мини-игра: Suika",
		[L.HintTouch] = "Держи палец на экране — руки тянутся к нему",
		[L.NeedFruits] = "Сначала собери фрукты в основном режиме!",
		[L.Score] = "Счёт: {0}",
		[L.GameOverTitle] = "Игра окончена!",
		[L.HighScore] = "Рекорд: {0}",
		[L.Restart] = "Заново",
		[L.ToMenu] = "В меню",
		[L.LevelUp] = "Уровень {0}!",
		[L.LevelShort] = "LV {0}",

		[L.PauseTitle] = "Пауза",
		[L.Resume] = "Продолжить",
		[L.PauseStats] = "Счёт: {0}    HP: {1}",
		[L.SubPerksNone] = "Субперков пока нет",
		[L.SubPerksHeader] = "Субперки:",
		[L.SubPerkEntry] = "{0} ур.{1} ({2}%)",

		[L.PerkNewTitle] = "Новый перк!  Уровень {0}",
		[L.PerkCount] = "Перки: {0}/{1} — выбери одно улучшение, удерживай карточку 1 сек",
		[L.PerkCard] = "{0}\nУр. {1}/{2} → {3}\n{4}",
		[L.PerkFooter] = "За забег можно взять {0} перков — потом их можно докачать до максимума",

		[L.SuikaScore] = "Счёт  {0}",
		[L.SuikaBest] = "Рекорд  {0}",
		[L.SuikaStock] = "Фрукты: {0}",
		[L.SuikaNext] = "Следующий",
		[L.SuikaHintDrop] = "Отпусти, чтобы бросить",
		[L.SuikaOverflow] = "Кувшин полон",
		[L.SuikaNoFruits] = "Фрукты закончились",
		[L.SuikaGameOverScore] = "Счёт: {0}",
		[L.SuikaRetry] = "Играть ещё",
		[L.SuikaStockTitle] = "Собранные фрукты",
		[L.SuikaStockRow] = "{0} — {1}",
		[L.SuikaStockTotal] = "Всего: {0}",
		[L.SuikaStart] = "Начать игру",
		[L.SuikaHowTo] = "Как играть",
		[L.SuikaRule1] = "Води палец по экрану — лягушка целится. Отпусти — фрукт летит в кувшин.",
		[L.SuikaRule2] = "Два одинаковых фрукта при касании сливаются в следующий фрукт.",
		[L.SuikaRule3] = "Фрукты берутся из запаса, собранного в классическом режиме.",
		[L.SuikaRule4] = "Не дай кувшину переполниться: линия переполнения — предел.",
		[L.SuikaChain] = "Цепочка слияния:",
		[L.SuikaGotIt] = "Понятно",

		[L.PopStone] = "Камень!",
		[L.PopBounce] = "Отскок!",
		[L.PopMiss] = "Мимо!",
		[L.PopRevive] = "Возрождение!",
		[L.PopDoubleCatch] = "Двойной хват!",
		[L.PopCombo] = "Комбо x{0}",
		[L.PopBonusHp] = "+1 HP",
		[L.PopExplosion] = "Взрыв +{0}",
		[L.PopGaze] = "Взгляд василиска!",
	};
}
