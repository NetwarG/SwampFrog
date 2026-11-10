using System.Collections.Generic;
using Godot;

namespace SwampFrog;

/// <summary>
/// Якорь шляпы для позы лягушки: смещение относительно позы Idle (её якорь калиброван в
/// <see cref="CosmeticDraw.HatAnchorY"/>). Голова у поз нарисована по-разному, поэтому шляпа
/// приподнимается и сдвигается. Смещение считается по спрайту: «плотная» макушка — первая строка
/// с заметной шириной, что отсекает тонкие выступы (брови/глаза) — иначе шляпа садилась бы на лицо.
/// </summary>
public static class HatAnchor
{
	/// <summary>Минимальная ширина макушки в пикселях: отсекает тонкие выступы.</summary>
	private const int SolidWidth = 36;

	/// <summary>Высота полосы головы под макушкой для поиска её центра, пиксели.</summary>
	private const int HeadBand = 26;

	/// <summary>Порог альфы: ниже пиксель считается прозрачным (совпадает с замером по спрайтам).</summary>
	private const int AlphaThreshold = 16;

	private static readonly Dictionary<FrogPose, Vector2> Offsets = new();

	/// <summary>Смещение шляпы для позы относительно Idle, в локальных единицах лягушки.</summary>
	public static Vector2 Offset(FrogPose pose)
	{
		if (Offsets.TryGetValue(pose, out Vector2 cached))
			return cached;
		Vector2 offset = Measure(pose);
		Offsets[pose] = offset;
		return offset;
	}

	/// <summary>Макушка одного спрайта: строка «плотной» макушки, её центр и масштаб текстуры.</summary>
	private readonly record struct Head(int Row, float CenterX, float Scale);

	private static Vector2 Measure(FrogPose pose)
	{
		Head reference = Profile(FrogPose.Idle);
		Head current = Profile(pose);
		if (reference.Scale <= 0f || current.Scale <= 0f)
			return Vector2.Zero;
		return new Vector2(
			(current.CenterX - reference.CenterX) * reference.Scale,
			(current.Row - reference.Row) * reference.Scale);
	}

	private static Head Profile(FrogPose pose)
	{
		Texture2D? texture = GD.Load<Texture2D>(FrogPoses.TexturePath(pose));
		if (texture == null)
			return default;
		Image image = texture.GetImage();
		if (image == null)
			return default;
		if (image.GetFormat() != Image.Format.Rgba8)
			image.Convert(Image.Format.Rgba8);
		int width = image.GetWidth();
		int height = image.GetHeight();
		byte[] pixels = image.GetData();
		if (width <= 0 || height <= 0 || pixels.Length < width * height * 4)
			return default;

		int solid = -1;
		for (int y = 0; y < height && solid < 0; y++)
		{
			RowBounds(y, width, pixels, out int left, out int right);
			if (right - left + 1 >= SolidWidth)
				solid = y;
		}
		if (solid < 0)
			return default;

		int minX = width;
		int maxX = -1;
		int end = Mathf.Min(solid + HeadBand, height - 1);
		for (int y = solid; y <= end; y++)
		{
			RowBounds(y, width, pixels, out int left, out int right);
			if (right < left)
				continue;
			minX = Mathf.Min(minX, left);
			maxX = Mathf.Max(maxX, right);
		}
		if (maxX < 0)
			return default;
		return new Head(solid, (minX + maxX) * 0.5f, Frog.BodyVisualHeight / height);
	}

	/// <summary>Границы непрозрачных пикселей в строке; у пустой строки left больше right.</summary>
	private static void RowBounds(int y, int width, byte[] pixels, out int left, out int right)
	{
		left = width;
		right = -1;
		int offset = y * width * 4 + 3;
		for (int x = 0; x < width; x++)
		{
			if (pixels[offset + x * 4] > AlphaThreshold)
			{
				if (x < left) left = x;
				right = x;
			}
		}
	}
}
