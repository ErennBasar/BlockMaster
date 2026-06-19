using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private float _spawnTimer = 0f;
    
    private readonly List<Vector2I[]> _shapeTemplates = new List<Vector2I[]>
    {
        new Vector2I[] { new(0,0), new(1,0), new(0,1), new(1,1) }, // Kare 
        new Vector2I[] { new(0,0), new(0,1), new(0,2), new(0,3) }, // Dikey Çubuk 
        new Vector2I[] { new(0,0), new(1,0), new(2,0), new(3,0) }, // Yatay Çubuk 
        new Vector2I[] { new(0,0), new(0,1), new(0,2), new(1,2) }, // L Şekli (Normal)
        new Vector2I[] { new(0,0), new(1,0), new(2,0), new(2,1) }, // L Şekli (Yatık)
        new Vector2I[] { new(0,0), new(1,0), new(1,1), new(2,1) }, // Z Şekli
        new Vector2I[] { new(0,0), new(1,0), new(2,0), new(0,1), new(1,1), new(2,1) }, // Dikdörtgen (3x2)
        new Vector2I[] { new(0,0), new(1,0), new(0,1) } // Küçük Köşe (3'lü L)
    };

    // Arka planda süzülecek cam bloklarımızın iskeleti
    private partial class GlassBlock : Control
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
            _spawnTimer = _rng.RandfRange(0.4f, 1.2f); // Doğma sıklığı
        }

        // Yaşayan tüm blokları aşağıya doğru süzülerek düşür
        Vector2 screenSize = GetViewportRect().Size;
        for (int i = _blocks.Count - 1; i >= 0; i--)
        {
            GlassBlock b = _blocks[i];
            b.Position += new Vector2(0, b.FallSpeed * d);
            b.Rotation += b.RotationSpeed * d;

            // Ekrandan tamamen çıkan blokları hafızadan (RAM) silme islemi
            if (b.Position.Y > screenSize.Y + 300)
            {
                b.QueueFree();
                _blocks.RemoveAt(i);
            }
        }
    }

    private void SpawnGlassBlock()
    {
        GlassBlock block = new GlassBlock();
        block.ZIndex = -1; 
        
        // RASTGELE BİR ŞEKİL VE HÜCRE BOYUTU 
        Vector2I[] shape = _shapeTemplates[_rng.RandiRange(0, _shapeTemplates.Count - 1)];
        float cellSize = _rng.RandfRange(25f, 45f); 

        // ŞEKLİN GENİŞLİĞİ
        int maxX = 0, maxY = 0;
        foreach (Vector2I pos in shape)
        {
            if (pos.X > maxX) maxX = pos.X;
            if (pos.Y > maxY) maxY = pos.Y;
        }
        
        block.Size = new Vector2((maxX + 1) * cellSize, (maxY + 1) * cellSize);
        block.PivotOffset = block.Size / 2f; 
        
        StyleBoxFlat style = new StyleBoxFlat();
        style.BgColor = new Color(1, 1, 1, _rng.RandfRange(0.02f, 0.08f));
        style.BorderWidthTop = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthBottom = 1;
        style.BorderWidthRight = 1;
        style.BorderColor = new Color(1, 1, 1, 0.15f);
        
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        
        foreach (Vector2I pos in shape)
        {
            Panel cell = new Panel();
            cell.Size = new Vector2(cellSize, cellSize);
            cell.Position = new Vector2(pos.X * cellSize, pos.Y * cellSize);
            cell.AddThemeStyleboxOverride("panel", style);
            block.AddChild(cell);
        }
        
        Vector2 screenSize = GetViewportRect().Size;
        block.Position = new Vector2(_rng.RandfRange(-100, screenSize.X + 50), -300);
        
        block.FallSpeed = _rng.RandfRange(40f, 130f);
        block.RotationSpeed = _rng.RandfRange(-1.5f, 1.5f);

        _blocks.Add(block);
        AddChild(block);
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://node_2d.tscn"); 
    }
}