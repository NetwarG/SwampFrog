using Godot;

namespace SwampFrog;

/// <summary>
/// Палитра мухи и плесени протухшего фрукта. Отдельная от <see cref="EffectStyle"/>
/// («палитра эффектов перков»), чтобы цвета насекомого и порчи не смешивались с перками.
/// </summary>
internal static class FlyStyle
{
	/// <summary>Тело мухи.</summary>
	internal static readonly Color Body = new("3b3f4a");

	/// <summary>Полосы на теле.</summary>
	internal static readonly Color Stripe = new("14161c");

	/// <summary>Полупрозрачные крылья.</summary>
	internal static readonly Color Wing = new("bcd6e6");

	/// <summary>Зелёный налёт плесени.</summary>
	internal static readonly Color Mold = new("7bc44a");

	/// <summary>Тёмные пятна порчи.</summary>
	internal static readonly Color Stain = new("3d5a24");
}
