using System;
using Godot;

namespace SwampFrog;

/// <summary>
/// Сетка достижений для карточки профиля: фиксированное число колонок, плитки
/// строятся из каталога — новое достижение просто удлиняет сетку.
/// </summary>
public partial class AchievementWall : GridContainer
{
	/// <summary>Число колонок сетки.</summary>
	public const int ColumnCount = 4;

	/// <summary>Зазор между колонками и рядами плиток.</summary>
	public const float ColumnSeparation = 10f;
	public const float RowSeparation = 12f;

	private AchievementEntry[] _entries = Array.Empty<AchievementEntry>();

	public override void _Ready()
	{
		Columns = ColumnCount;
		AddThemeConstantOverride("h_separation", (int)ColumnSeparation);
		AddThemeConstantOverride("v_separation", (int)RowSeparation);
		MouseFilter = MouseFilterEnum.Ignore;
		Rebuild();
	}

	/// <summary>Высота сетки для заданного числа плиток (для фиксированной области профиля).</summary>
	public static float HeightFor(int count)
	{
		int rows = Mathf.CeilToInt(count / (float)ColumnCount);
		if (rows <= 0)
			return 0f;
		return rows * AchievementTile.TileHeight + (rows - 1) * RowSeparation;
	}

	/// <summary>Задаёт достижения сетки и перестраивает плитки.</summary>
	public void SetEntries(AchievementEntry[] entries)
	{
		_entries = entries ?? Array.Empty<AchievementEntry>();
		Rebuild();
	}

	/// <summary>Обновляет подписи плиток (например, при смене языка).</summary>
	public void RefreshTiles()
	{
		foreach (Node child in GetChildren())
		{
			if (child is AchievementTile tile)
				tile.Refresh();
		}
	}

	private void Rebuild()
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}
		foreach (AchievementEntry entry in _entries)
		{
			var tile = new AchievementTile();
			tile.Setup(entry);
			AddChild(tile);
		}
	}
}
