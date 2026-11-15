using Godot;

namespace SwampFrog;

/// <summary>
/// Дом-баннер: собственная заглушка на месте объявления рекламной сети. Рисуется в стиле
/// кнопок игры, поэтому блок виден там, где нативной рекламы нет (редактор, ПК, тесты).
/// </summary>
public partial class AdBannerView : Control
{
	/// <summary>Высота полосы, при которой вторая строка с призывом ещё читается.</summary>
	public const float ClaimMinHeight = 56f;

	/// <summary>Акцент полосы: приглушённое золото, чтобы блок не спорил с кнопками меню.</summary>
	private static readonly Color Accent = new("e0c05a");

	private Label? _title;
	private Label? _claim;
	private Label? _tag;

	/// <summary>Показана ли вторая строка с призывом.</summary>
	public bool ClaimVisible => _claim?.Visible ?? false;

	/// <summary>Название игры на полосе.</summary>
	public string TitleText => _title?.Text ?? string.Empty;

	/// <summary>Подпись «Реклама» в углу полосы.</summary>
	public string TagText => _tag?.Text ?? string.Empty;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(new StickerBackground
		{
			Name = "AdPanel",
			LightColor = StickerStyle.Light(Accent),
			DarkColor = StickerStyle.Dark(Accent),
			CornerRadius = UiStyle.CornerRadius * 0.8f,
			OutlineWidth = StickerStyle.OutlineWidth
		});
		_title = MakeLine("Swamp Frog", "AdTitle", 26, new Color("173c34"));
		_claim = MakeLine(Loc.T(L.MenuAdClaim), "AdClaim", 18, new Color("173c34"));
		_tag = MakeLine(Loc.T(L.MenuAdLabel), "AdTag", 16, new Color(0.09f, 0.24f, 0.2f, 0.55f));
		_tag.HorizontalAlignment = HorizontalAlignment.Right;
		Loc.Changed += RefreshText;
		Resized += LayoutContent;
		LayoutContent();
	}

	public override void _ExitTree() => Loc.Changed -= RefreshText;

	private Label MakeLine(string text, string name, int fontSize, Color color)
	{
		Label label = UiStyle.MakeLabel(text, fontSize, color);
		label.Name = name;
		label.VerticalAlignment = VerticalAlignment.Center;
		AddChild(label);
		return label;
	}

	private void RefreshText()
	{
		if (_claim != null) _claim.Text = Loc.T(L.MenuAdClaim);
		if (_tag != null) _tag.Text = Loc.T(L.MenuAdLabel);
	}

	/// <summary>Низкая полоса: призыв уступает место названию игры и подписи «Реклама».</summary>
	private void LayoutContent()
	{
		if (_title == null || _claim == null || _tag == null)
			return;
		float height = Size.Y;
		float width = Size.X;
		bool claimed = height >= ClaimMinHeight;
		_claim.Visible = claimed;
		_title.Position = new Vector2(0f, height * 0.06f);
		_title.Size = new Vector2(width, height * (claimed ? 0.48f : 0.78f));
		_claim.Position = new Vector2(0f, height * 0.52f);
		_claim.Size = new Vector2(width, height * 0.42f);
		_tag.Position = new Vector2(0f, 4f);
		_tag.Size = new Vector2(Mathf.Max(0f, width - 12f), 20f);
	}
}
