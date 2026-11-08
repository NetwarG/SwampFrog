using Godot;

namespace SwampFrog;

/// <summary>
/// Фон пруда: градиент воды и мягкая тень у нижней кромки. Один источник для мира
/// (см. <see cref="Main"/>) и для экрана (см. <see cref="GameScreen"/>), чтобы палитра
/// воды не расходилась между полем и боковыми панелями. Боковых полос нет: любые
/// вертикальные стыки внутри поля читаются как артефакт затемнения.
/// </summary>
public static class PondBackdrop
{
	/// <summary>Полос градиента: чем больше, тем плавнее переход.</summary>
	private const int Steps = 64;

	/// <summary>Глубина тени у нижней кромки в долях поля, её предел и число шагов.</summary>
	private const float EdgeDepth = 0.1f;
	private const float EdgeAlpha = 0.1f;
	private const int EdgeSteps = 24;

	/// <summary>Заливает область водой: градиент продолжается по координатам поля.</summary>
	public static void DrawWater(CanvasItem target, Rect2 area, Rect2 field)
	{
		if (!area.HasArea() || field.Size.Y <= 0f)
			return;
		float strip = area.Size.Y / Steps;
		for (int i = 0; i < Steps; i++)
		{
			float y = area.Position.Y + i * strip;
			// Выше поля вода светлеет (поверхность), ниже границы — глубже и темнее.
			float t = Mathf.Clamp((y - field.Position.Y) / field.Size.Y, 0f, 1f);
			target.DrawRect(new Rect2(area.Position.X, y, area.Size.X, strip + 1f),
				Playfield.PondTop.Lerp(Playfield.PondBottom, t));
		}
	}

	/// <summary>Мягко затемняет нижнюю кромку поля: листья у дна «садятся» на место.</summary>
	public static void DrawBottomShade(CanvasItem target, Rect2 field)
	{
		if (!field.HasArea())
			return;
		float depth = field.Size.Y * EdgeDepth;
		float strip = depth / EdgeSteps;
		for (int i = 0; i < EdgeSteps; i++)
		{
			target.DrawRect(new Rect2(field.Position.X, field.End.Y - depth + i * strip, field.Size.X, strip + 1f),
				new Color(Playfield.PondBottom, EdgeAlpha * (i + 1) / EdgeSteps));
		}
	}
}
