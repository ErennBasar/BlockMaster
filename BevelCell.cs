using Godot;

[GlobalClass]
public partial class BevelCell : Control
{
    private Color _mainColor;
    private bool  _isEmpty = true;

    public Color MainColor
    {
        get => _mainColor;
        set { _mainColor = value; QueueRedraw(); }
    }
    
    public bool IsEmpty
    {
        get => _isEmpty;
        set { _isEmpty = value; QueueRedraw(); }
    }

    public override void _Draw()
    {
        if (_isEmpty)
            DrawEmpty();
        else
            DrawFilled();
    }

    private void DrawEmpty()
    {
        // Boş hücre: Fotoğraftaki gibi sadece hafif karanlık, düz bir zemin ve ince bir çerçeve.
        Vector2 sz = Size;
        DrawRect(new Rect2(0, 0, sz.X, sz.Y), _mainColor);
        // Çok ince ve saydam bir iç kenarlık (Tahtanın ızgara çizgileri)
        DrawRect(new Rect2(0, 0, sz.X, sz.Y), new Color(0, 0, 0, 0.1f), false, 1f);
    }

    private void DrawFilled()
    {
        Vector2 sz = Size;
        
        // m (margin) = İçteki küçük karenin dışarıdan ne kadar içeride olacağı (Derinlik miktarı)
        float m = 20f; 

        // ── KÖŞE NOKTALARI (DIŞ KARE VE İÇ KARE) ──
        Vector2 pTL = new Vector2(0, 0);         // Dış Sol Üst
        Vector2 pTR = new Vector2(sz.X, 0);      // Dış Sağ Üst
        Vector2 pBL = new Vector2(0, sz.Y);      // Dış Sol Alt
        Vector2 pBR = new Vector2(sz.X, sz.Y);   // Dış Sağ Alt

        Vector2 iTL = new Vector2(m, m);                 // İç Sol Üst
        Vector2 iTR = new Vector2(sz.X - m, m);          // İç Sağ Üst
        Vector2 iBL = new Vector2(m, sz.Y - m);          // İç Sol Alt
        Vector2 iBR = new Vector2(sz.X - m, sz.Y - m);   // İç Sağ Alt

        // ── IŞIK VE GÖLGE RENKLERİ (Fotoğraftaki gibi sol-üst parlak, sağ-alt karanlık) ──
        Color colorTop    = _mainColor.Lightened(0.4f);  // Işığı tam alan tepe
        Color colorLeft   = _mainColor.Lightened(0.2f);  // Yandan ışık alan sol yüz
        Color colorRight  = _mainColor.Darkened(0.3f);   // Gölgede kalan sağ yüz
        Color colorBottom = _mainColor.Darkened(0.5f);   // En karanlık alt yüz
        Color colorCenter = _mainColor;                  // Bloğun kendi orijinal rengi (İç kare)

        // ── 1. POLİGONLARI (YAMUKLARI) ÇİZ ──
        // Üst Yüzey (Trapezoid)
        DrawPoly(new[] { pTL, pTR, iTR, iTL }, colorTop);
        // Sağ Yüzey
        DrawPoly(new[] { pTR, pBR, iBR, iTR }, colorRight);
        // Alt Yüzey
        DrawPoly(new[] { pBR, pBL, iBL, iBR }, colorBottom);
        // Sol Yüzey
        DrawPoly(new[] { pBL, pTL, iTL, iBL }, colorLeft);
        // Merkez (İç Küçük Kare)
        DrawPoly(new[] { iTL, iTR, iBR, iBL }, colorCenter);

        // ── 2. ÇİZGİLER (Fotoğraftaki o keskin detayları veren jilet dokunuş) ──
        // Çapraz (Diyagonal) bağlantı çizgileri
        Color edgeColor = new Color(0, 0, 0, 0.15f); // Saydam siyah ile derinliği keskinleştiriyoruz
        DrawLine(pTL, iTL, edgeColor, 1f);
        DrawLine(pTR, iTR, edgeColor, 1f);
        DrawLine(pBL, iBL, edgeColor, 1f);
        DrawLine(pBR, iBR, edgeColor, 1f);

        // İç karenin ve dış karenin etrafına ince bir çerçeve (Daha "tok" durması için)
        DrawRect(new Rect2(m, m, sz.X - 2 * m, sz.Y - 2 * m), edgeColor, false, 1f);
        DrawRect(new Rect2(0, 0, sz.X, sz.Y), edgeColor, false, 1f);
    }

    // Godot'nun DrawPolygon metodunu daha temiz kullanmak için yazdığımız ufak bir yardımcı (Helper) metot
    private void DrawPoly(Vector2[] pts, Color c)
    {
        DrawPolygon(pts, new Color[] { c, c, c, c });
    }
}