using System;
using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Оверлей рук лягушки: затухающий след ладони («Скорострельность»), пунктирная окружность
/// радиуса ловли («Липкие ладони») и окружность магнита на высоких уровнях. Лежит под
/// предметами и рисуется только пока руки вытянуты, поэтому кадровой нагрузки не даёт.
/// </summary>
public partial class HandOverlayView : Node2D
{
	/// <summary>Сколько точек хранит след каждой ладони.</summary>
	private const int TrailLength = 8;

	/// <summary>Сколько времени след держится после возврата рук, секунды.</summary>
	private const float TrailFade = 0.12f;

	/// <summary>Минимальный сдвиг ладони для новой точки следа, мировые единицы.</summary>
	private const float MinStep = 5f;

	/// <summary>Сколько пунктиров в окружностях радиуса.</summary>
	private const int DashCount = 28;

	private readonly List<Vector2>[] _trail = { new(), new() };
	private readonly Vector2[] _points = new Vector2[TrailLength];
	private readonly Color[] _outline = new Color[TrailLength];
	private readonly Color[] _core = new Color[TrailLength];
	private readonly Vector2[] _dash = new Vector2[DashCount * 2];

	private Vector2[]? _hands;
	private float _catchRadius;
	private float _magnetRadius;
	private float _fade;

	/// <summary>Есть ли сейчас след ладони (проверки).</summary>
	public bool HasTrail => _trail[0].Count > 1 || _trail[1].Count > 1;

	/// <summary>Сколько точек в следе конкретной ладони (проверки).</summary>
	public int TrailPoints(int handIndex) => _trail[handIndex].Count;

	/// <summary>Показываемый радиус ловли (0 — руки не вытянуты).</summary>
	public float CatchRadius => _hands == null ? 0f : _catchRadius;

	/// <summary>Показываемый радиус магнита (0 — магнит не активен).</summary>
	public float MagnetRadius => _hands == null ? 0f : _magnetRadius;

	/// <summary>Передаёт позиции ладоней текущего кадра; null — руки не вытянуты, след гаснет.</summary>
	public void SetHands(Vector2[]? hands, float catchRadius, float magnetRadius, float dt)
	{
		_hands = hands;
		_catchRadius = catchRadius;
		_magnetRadius = magnetRadius;
		if (hands == null)
		{
			_fade = Mathf.Max(0f, _fade - dt);
			if (_fade <= 0f)
			{
				ClearTrail();
			}
			QueueRedraw();
			return;
		}
		_fade = TrailFade;
		int count = Mathf.Min(hands.Length, _trail.Length);
		for (int i = 0; i < count; i++)
		{
			PushTrail(i, hands[i]);
		}
		QueueRedraw();
	}

	/// <summary>Убирает след и окружности: рестарт, выход в меню, Game Over.</summary>
	public void Clear()
	{
		ClearTrail();
		_hands = null;
		_fade = 0f;
		QueueRedraw();
	}

	private void ClearTrail()
	{
		for (int i = 0; i < _trail.Length; i++)
		{
			_trail[i].Clear();
		}
	}

	/// <summary>Новая точка следа: одна на кадр, старые вытесняются из конца буфера.</summary>
	private void PushTrail(int handIndex, Vector2 at)
	{
		List<Vector2> trail = _trail[handIndex];
		if (trail.Count == 0 || trail[0].DistanceTo(at) >= MinStep)
		{
			trail.Insert(0, at);
		}
		else
		{
			trail[0] = at;
		}
		while (trail.Count > TrailLength)
		{
			trail.RemoveAt(trail.Count - 1);
		}
	}

	public override void _Draw()
	{
		if (_hands == null && _fade <= 0f)
		{
			return;
		}
		float fade = _hands == null ? Mathf.Clamp(_fade / TrailFade, 0f, 1f) : 1f;
		for (int i = 0; i < _trail.Length; i++)
		{
			DrawTrail(i, fade);
		}
		if (_hands == null)
		{
			return;
		}
		// Радиус ловли виден всегда, радиус магнита — только с 4-го уровня «Липких ладоней».
		int hands = Mathf.Min(_hands.Length, _trail.Length);
		for (int i = 0; i < hands; i++)
		{
			if (_catchRadius > 4f)
			{
				DrawDashedCircle(_hands[i], _catchRadius, new Color(EffectStyle.White, 0.14f), 0.55f);
			}
			if (_magnetRadius > _catchRadius)
			{
				DrawDashedCircle(_hands[i], _magnetRadius, new Color(EffectStyle.Syrup, 0.30f), 0.75f);
			}
		}
	}

	/// <summary>Затухающая ломаная по следу ладони: ярче у ладони, прозрачнее к хвосту.</summary>
	private void DrawTrail(int handIndex, float fade)
	{
		List<Vector2> trail = _trail[handIndex];
		int count = trail.Count;
		if (count < 2 || fade <= 0f)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			float k = 1f - i / (float)count;
			float alpha = k * k * fade;
			_points[i] = trail[i];
			_outline[i] = new Color(EffectStyle.Outline, 0.45f * alpha);
			_core[i] = new Color(EffectStyle.Gold, 0.75f * alpha);
		}
		// Тот же приём, что у остальных эффектов: тёмная обводка читается и на воде, и на лилии.
		DrawPolylineColors(_points.AsSpan(0, count), _outline.AsSpan(0, count), 6f, false);
		DrawPolylineColors(_points.AsSpan(0, count), _core.AsSpan(0, count), 2.5f, false);
	}

	/// <summary>Пунктирная окружность: показывает радиус ловли и радиус магнита.</summary>
	private void DrawDashedCircle(Vector2 center, float radius, Color color, float coverage)
	{
		float slice = Mathf.Tau / DashCount;
		for (int i = 0; i < DashCount; i++)
		{
			float from = slice * i;
			float to = from + slice * coverage;
			_dash[i * 2] = center + new Vector2(Mathf.Cos(from), Mathf.Sin(from)) * radius;
			_dash[i * 2 + 1] = center + new Vector2(Mathf.Cos(to), Mathf.Sin(to)) * radius;
		}
		DrawMultiline(_dash, color, 2f);
	}
}
