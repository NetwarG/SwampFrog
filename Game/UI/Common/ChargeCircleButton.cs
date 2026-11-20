using Godot;

namespace SwampFrog;

/// <summary>Значок круглой кнопки-способности.</summary>
public enum ChargeGlyph
{
	Waves,
	Bomb,
	Shrink
}

/// <summary>
/// Круглая кнопка способности Suika: значок, заряды и прогресс разблокировки.
/// Стиль общий со стикерами игры (см. <see cref="StickerStyle"/>).
/// </summary>
public partial class ChargeCircleButton : TouchArea
{
	/// <summary>Диаметр кнопки в логических единицах.</summary>
	public const float Diameter = 64f;

	private const float ProgressWidth = 5f;
	private static readonly Color GlyphColor = new("2b170f");
	private static readonly Color LockedLight = new(0.52f, 0.52f, 0.52f, 0.95f);
	private static readonly Color LockedDark = new(0.34f, 0.34f, 0.34f, 0.95f);

	private ChargeGlyph _glyph = ChargeGlyph.Waves;
	private Color _accent = new("b4e863");
	private bool _charged;
	private int _charges;
	private float _progress;
	private bool _armed;
	private bool _selected;
	private Label? _badge;
	private Label? _progressLabel;
	private float _pulse;

	/// <summary>Значок кнопки.</summary>
	public ChargeGlyph Glyph
	{
		get => _glyph;
		set { _glyph = value; QueueRedraw(); }
	}

	/// <summary>Акцентный цвет стикера.</summary>
	public Color Accent
	{
		get => _accent;
		set { _accent = value; QueueRedraw(); }
	}

	/// <summary>Есть готовый заряд: кнопка цветная.</summary>
	public bool Charged => _charged;

	/// <summary>Остаток зарядов.</summary>
	public int Charges => _charges;

	/// <summary>Прогресс разблокировки 0..1.</summary>
	public float Progress => _progress;

	/// <summary>Кнопка ждёт встряхивания: рамка пульсирует.</summary>
	public bool Armed
	{
		get => _armed;
		set { _armed = value; QueueRedraw(); }
	}

	/// <summary>Кнопка выбрана: лягушка держит бомбу.</summary>
	public bool Selected
	{
		get => _selected;
		set { _selected = value; QueueRedraw(); }
	}

	/// <summary>Можно ли нажимать: есть заряд или идёт выбор.</summary>
	public bool Available => _charges > 0 || _armed || _selected;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(Diameter, Diameter);
		Size = new Vector2(Diameter, Diameter);
		_progressLabel = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_progressLabel.AddThemeFontSizeOverride("font_size", 16);
		_progressLabel.AddThemeColorOverride("font_color", GlyphColor);
		AddChild(_progressLabel);
		_progressLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_badge = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_badge.AddThemeFontSizeOverride("font_size", 14);
		_badge.AddThemeColorOverride("font_color", GlyphColor);
		AddChild(_badge);
		Resized += UpdateLabels;
		UpdateLabels();
	}

	/// <summary>Состояние кнопки: есть ли заряд, сколько зарядов и прогресс накопления.</summary>
	public void SetState(bool charged, int charges, float progress)
	{
		_charged = charged;
		_charges = Mathf.Max(0, charges);
		_progress = Mathf.Clamp(progress, 0f, 1f);
		UpdateLabels();
		QueueRedraw();
	}

	public override void _GuiInput(InputEvent @event)
	{
		// Недоступная кнопка всё равно поглощает нажатие: иначе тап по ней бросил бы фрукт.
		if (!Available)
		{
			AcceptEvent();
			return;
		}
		base._GuiInput(@event);
	}

	public override void _Process(double delta)
	{
		if (!_armed && !_selected) return;
		_pulse += (float)delta * 7f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 center = Size * 0.5f;
		float radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 1f;
		Color light = _charged ? StickerStyle.Light(_accent) : LockedLight;
		Color dark = _charged ? StickerStyle.Dark(_accent) : LockedDark;
		StickerStyle.DrawCirclePanel(this, center, radius, light, dark, StickerStyle.OutlineWidth, true);
		// Значок виден только при готовом заряде; иначе центр занят процентом накопления.
		if (_charged) DrawGlyph(center, radius * 0.52f);
		if (_progress > 0f)
			DrawArc(center, radius - ProgressWidth * 0.5f - 1f, -Mathf.Pi * 0.5f,
				-Mathf.Pi * 0.5f + Mathf.Tau * _progress, 32, new Color(_accent, 0.95f), ProgressWidth, true);
		if (_charges > 0) DrawBadgeCircle();
		if (_armed || _selected)
		{
			float glow = 0.5f + 0.5f * Mathf.Sin(_pulse);
			DrawArc(center, radius + 2f, 0f, Mathf.Tau, 40, new Color(_accent, 0.3f + 0.5f * glow), 4f, true);
		}
	}

	/// <summary>Бейдж — только заряды; процент накопления стоит по центру вместо значка.</summary>
	private void UpdateLabels()
	{
		if (_progressLabel != null)
		{
			_progressLabel.Text = $"{Mathf.RoundToInt(_progress * 100f)}%";
			_progressLabel.Visible = _charges == 0;
		}
		if (_badge == null) return;
		_badge.Text = $"×{_charges}";
		_badge.Visible = _charges > 0;
		_badge.Position = new Vector2(Size.X - 28f, 2f);
		_badge.Size = new Vector2(28f, 22f);
	}

	private void DrawBadgeCircle()
	{
		Vector2 at = new(Size.X - 12f, 12f);
		DrawCircle(at, 12f, StickerStyle.Outline);
		DrawCircle(at, 10f, new Color(StickerStyle.Light(_accent)));
	}

	private void DrawGlyph(Vector2 center, float size)
	{
		Color color = new(GlyphColor, _charged ? 0.92f : 0.55f);
		switch (_glyph)
		{
			case ChargeGlyph.Waves:
				DrawWaves(center, size, color);
				break;
			case ChargeGlyph.Bomb:
				DrawBombGlyph(center, size, color);
				break;
			default:
				DrawShrinkGlyph(center, size, color);
				break;
		}
	}

	/// <summary>Тряска: три волны со стрелками.</summary>
	private void DrawWaves(Vector2 center, float size, Color color)
	{
		for (int i = -1; i <= 1; i++)
		{
			float y = center.Y + i * size * 0.62f;
			float left = center.X - size * 0.85f;
			float right = center.X + size * 0.85f;
			DrawLine(new Vector2(left, y), new Vector2(right, y), color, 2.4f);
			DrawColoredPolygon(new[]
			{
				new Vector2(right + 4.5f, y),
				new Vector2(right - 1.5f, y - 4.5f),
				new Vector2(right - 1.5f, y + 4.5f)
			}, color);
		}
	}

	/// <summary>Бомба: шар с фитилём и искрой.</summary>
	private void DrawBombGlyph(Vector2 center, float size, Color color)
	{
		Vector2 body = center + new Vector2(-size * 0.06f, size * 0.12f);
		DrawCircle(body, size * 0.66f, color);
		Vector2 fuseStart = body + new Vector2(size * 0.42f, -size * 0.5f);
		Vector2 fuseTip = fuseStart + new Vector2(size * 0.34f, -size * 0.34f);
		DrawLine(fuseStart, fuseTip, color, 2.4f);
		DrawCircle(fuseTip, size * 0.18f, new Color("ff9a3c"));
	}

	/// <summary>Уменьшение: круг и две стрелки внутрь.</summary>
	private void DrawShrinkGlyph(Vector2 center, float size, Color color)
	{
		DrawArc(center, size * 0.5f, 0f, Mathf.Tau, 24, color, 2.6f, true);
		foreach (float side in new[] { -1f, 1f })
		{
			float outer = center.X + side * size * 0.95f;
			float inner = center.X + side * size * 0.62f;
			DrawLine(new Vector2(outer, center.Y), new Vector2(inner, center.Y), color, 2.4f);
			DrawColoredPolygon(new[]
			{
				new Vector2(inner, center.Y),
				new Vector2(inner - side * 4.5f, center.Y - 4.5f),
				new Vector2(inner - side * 4.5f, center.Y + 4.5f)
			}, color);
		}
	}
}
