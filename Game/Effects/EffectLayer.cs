using Godot;

namespace SwampFrog;

/// <summary>
/// Слой эффектов перков: добавляется в мир после предметов, поэтому рисуется поверх них
/// и под HUD. Эффекты сами тикают и удаляются по таймеру — слою нужен только выпуск,
/// а <see cref="Clear"/> убирает всё при рестарте, выходе в меню и Game Over.
/// </summary>
public partial class EffectLayer : Node2D
{
	/// <summary>Сколько эффектов сейчас живо: используется проверками.</summary>
	public int ActiveCount => GetChildCount();

	/// <summary>Ударная волна от точки: взрыв, золотая поимка, возрождение.</summary>
	public Shockwave SpawnShockwave(Vector2 at, float radius, Color color, float duration = 0.35f,
		float width = 6f, bool fill = true)
	{
		var effect = new Shockwave
		{
			Position = at,
			MaxRadius = radius,
			Color = color,
			Duration = duration,
			Width = width,
			Fill = fill
		};
		AddChild(effect);
		return effect;
	}

	/// <summary>Молния между двумя точками мира («Язык-молния»).</summary>
	public Bolt SpawnBolt(Vector2 from, Vector2 to, Color color, float duration = 0.22f, float width = 7f)
	{
		var effect = new Bolt { From = from, To = to, Color = color, Duration = duration, Width = width };
		AddChild(effect);
		return effect;
	}

	/// <summary>Разлёт искр из точки: осколки, брызги, распад мусора.</summary>
	public Sparks SpawnSparks(Vector2 at, Color color, int count = 10, float speed = 230f, float duration = 0.5f)
	{
		var effect = new Sparks
		{
			Position = at,
			Color = color,
			Count = count,
			Speed = speed,
			Duration = duration
		};
		AddChild(effect);
		return effect;
	}

	/// <summary>Волна по всему полю: заморозка василиска и полная остановка зоны.</summary>
	public FlashWave SpawnFlashWave(Vector2 origin, Vector2 fieldSize, Color color, float duration = 0.7f)
	{
		var effect = new FlashWave
		{
			OriginPoint = origin,
			FieldSize = fieldSize,
			Color = color,
			Duration = duration
		};
		AddChild(effect);
		return effect;
	}

	/// <summary>Убирает живые эффекты так, чтобы счётчик обнулился сразу.</summary>
	public void Clear()
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}
	}
}
