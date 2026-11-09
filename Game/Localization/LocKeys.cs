namespace SwampFrog;

/// <summary>
/// Ключи строк интерфейса. Значение константы совпадает с ключом в словарях
/// <see cref="LocText"/>, поэтому опечатка не компилируется.
/// Строки данных (фрукты, перки, субперки) живут в своих каталогах через <see cref="Loc.Pick(string, string)"/>.
/// </summary>
public static class L
{
	// ---------- Главное меню и HUD ----------
	public const string MenuSubtitle1 = "menu.subtitle1";
	public const string MenuSubtitle2 = "menu.subtitle2";
	public const string MenuPlay = "menu.play";
	public const string MenuExit = "menu.exit";
	public const string MenuMiniGame = "menu.minigame";
	public const string HintTouch = "hud.hintTouch";
	public const string NeedFruits = "hud.needFruits";
	public const string Score = "hud.score";
	public const string GameOverTitle = "hud.gameOverTitle";
	public const string HighScore = "hud.highScore";
	public const string Restart = "hud.restart";
	public const string ToMenu = "hud.toMenu";
	public const string LevelUp = "hud.levelUp";
	public const string LevelShort = "hud.levelShort";

	// ---------- Оверлей паузы ----------
	public const string PauseTitle = "pause.title";
	public const string Resume = "pause.resume";
	public const string PauseStats = "pause.stats";
	public const string SubPerksNone = "pause.subPerksNone";
	public const string SubPerksHeader = "pause.subPerksHeader";
	public const string SubPerkEntry = "pause.subPerkEntry";

	// ---------- Выбор перка ----------
	public const string PerkNewTitle = "perk.newTitle";
	public const string PerkCount = "perk.count";
	public const string PerkCard = "perk.card";
	public const string PerkFooter = "perk.footer";

	// ---------- Мини-игра Suika ----------
	public const string SuikaScore = "suika.score";
	public const string SuikaBest = "suika.best";
	public const string SuikaStock = "suika.stock";
	public const string SuikaNext = "suika.next";
	public const string SuikaHintDrop = "suika.hintDrop";
	public const string SuikaOverflow = "suika.overflow";
	public const string SuikaNoFruits = "suika.noFruits";
	public const string SuikaGameOverScore = "suika.gameOverScore";
	public const string SuikaRetry = "suika.retry";
	public const string SuikaStockTitle = "suika.stockTitle";
	public const string SuikaStockRow = "suika.stockRow";
	public const string SuikaStockTotal = "suika.stockTotal";
	public const string SuikaStart = "suika.start";
	public const string SuikaHowTo = "suika.howTo";
	public const string SuikaRule1 = "suika.rule1";
	public const string SuikaRule2 = "suika.rule2";
	public const string SuikaRule3 = "suika.rule3";
	public const string SuikaRule4 = "suika.rule4";
	public const string SuikaChain = "suika.chain";
	public const string SuikaGotIt = "suika.gotIt";

	// ---------- Всплывающие подписи ----------
	public const string PopStone = "popup.stone";
	public const string PopBounce = "popup.bounce";
	public const string PopMiss = "popup.miss";
	public const string PopRevive = "popup.revive";
	public const string PopDoubleCatch = "popup.doubleCatch";
	public const string PopCombo = "popup.combo";
	public const string PopBonusHp = "popup.bonusHp";
	public const string PopExplosion = "popup.explosion";
	public const string PopGaze = "popup.gaze";

	// ---------- Профиль игрока ----------
	public const string MenuProfile = "menu.profile";
	public const string ProfileTitle = "profile.title";
	public const string ProfileBack = "profile.back";
	public const string ProfileBestClassic = "profile.bestClassic";
	public const string ProfileBestSuika = "profile.bestSuika";
	public const string ProfileStock = "profile.stock";
	public const string ProfileRuns = "profile.runs";
	public const string ProfileCaught = "profile.caught";
	public const string ProfileGolden = "profile.golden";
	public const string ProfileMerges = "profile.merges";
}
