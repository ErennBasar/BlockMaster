using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private float _spawnTimer = 0f;

    // Arka planda süzülecek cam bloklarımızın iskeleti
    private partial class GlassBlock : Panel
    {
        public float FallSpeed;
        public float RotationSpeed;
    }

    private List<GlassBlock> _blocks = new List<GlassBlock>();

    public override void _Ready()
    {
        // ARKA PLAN 
        TextureRect bg = new TextureRect();
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        
        GradientTexture2D gradTex = new GradientTexture2D();
        Gradient grad = new Gradient();
        
        grad.SetColor(0, new Color("#0B1B29")); // Çok Koyu Gece Mavisi
        grad.SetColor(1, new Color("#164A41")); // Derin Orman Yeşili
        
        gradTex.Gradient = grad;
        gradTex.Fill = GradientTexture2D.FillEnum.Linear;
        gradTex.FillFrom = new Vector2(0, 0); 
        bg.Texture = gradTex;
        bg.ZIndex = -10; 
        AddChild(bg);

        // PLAY BUTONU 
        Button playBtn = new Button();
        playBtn.Text = "PLAY";
        playBtn.AddThemeFontSizeOverride("font_size", 48);
        
        // Butonun Normal Hali
        StyleBoxFlat btnStyle = new StyleBoxFlat();
        btnStyle.BgColor = new Color(0, 0, 0, 0.4f); // Hafif saydam siyah
        btnStyle.BorderWidthBottom = 4;
        btnStyle.BorderColor = new Color("#CDE8B5"); // fosforlu yeşilin
        btnStyle.CornerRadiusTopLeft = 8;
        btnStyle.CornerRadiusTopRight = 8;
        btnStyle.CornerRadiusBottomLeft = 8;
        btnStyle.CornerRadiusBottomRight = 8;
        btnStyle.ContentMarginLeft = 80;
        btnStyle.ContentMarginRight = 80;
        btnStyle.ContentMarginTop = 20;
        btnStyle.ContentMarginBottom = 20;
        playBtn.AddThemeStyleboxOverride("normal", btnStyle);
        
        // Hover effect
        StyleBoxFlat hoverStyle = (StyleBoxFlat)btnStyle.Duplicate();
        hoverStyle.BgColor = new Color("#CDE8B5").Darkened(0.6f); 
        playBtn.AddThemeStyleboxOverride("hover", hoverStyle);

        playBtn.ZIndex = 10; 
        AddChild(playBtn);
        
        CallDeferred(nameof(CenterButton), playBtn);
        
        playBtn.Pressed += OnPlayPressed;
    }

    private void CenterButton(Button btn)
    {
        Vector2 screenSize = GetViewportRect().Size;
        btn.Position = new Vector2((screenSize.X - btn.Size.X) / 2, (screenSize.Y - btn.Size.Y) / 2);
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _spawnTimer -= d;
        
        // Rastgele aralıklarla yeni bir cam blok doğur
        if (_spawnTimer <= 0)
        {
            SpawnGlassBlock();
            _spawnTimer = _rng.RandfRange(0.2f, 0.7f); // Doğma sıklığı
        }

        // Yaşayan tüm blokları aşağıya doğru süzülerek düşür
        Vector2 screenSize = GetViewportRect().Size;
        for (int i = _blocks.Count - 1; i >= 0; i--)
        {
            GlassBlock b = _blocks[i];
            b.Position += new Vector2(0, b.FallSpeed * d);
            b.Rotation += b.RotationSpeed * d;

            // Ekrandan tamamen çıkan blokları hafızadan (RAM) silme islemi
            if (b.Position.Y > screenSize.Y + 200)
            {
                b.QueueFree();
                _blocks.RemoveAt(i);
            }
        }
    }

    private void SpawnGlassBlock()
    {
        GlassBlock block = new GlassBlock();
        
        float size = _rng.RandfRange(40f, 120f);
        block.Size = new Vector2(size, size);
        
        // BUZLU CAM EFEKTİ 
        StyleBoxFlat style = new StyleBoxFlat();
        style.BgColor = new Color(1, 1, 1, _rng.RandfRange(0.02f, 0.08f)); 
        style.BorderWidthTop = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthBottom = 1;
        style.BorderWidthRight = 1;
        style.BorderColor = new Color(1, 1, 1, 0.15f); 
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        
        block.AddThemeStyleboxOverride("panel", style);
        
        Vector2 screenSize = GetViewportRect().Size;
        block.Position = new Vector2(_rng.RandfRange(-50, screenSize.X), -150);
        
        block.PivotOffset = new Vector2(size / 2, size / 2);
        
        block.FallSpeed = _rng.RandfRange(30f, 150f);
        block.RotationSpeed = _rng.RandfRange(-1.5f, 1.5f);
        
         block.ZIndex = -1;
        
        _blocks.Add(block);
        AddChild(block);
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://node_2d.tscn"); 
    }
}