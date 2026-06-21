using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BlockMaster : Node2D
{

	// 9x9 tahtayi temsil edecek 2 bouytlu matris
	private int[,] _grid = new int[8, 8]; 
	
	// Renkli kutucuklari tutacak matris
	// private Panel[,] _visualGrid = new Panel[8, 8];
	// private Panel[,] _bodyGrid  = new Panel[8, 8];
	private BevelCell[,] _cellGrid = new BevelCell[8, 8];
	private float _cellActualSize;
	private const float BodyHeight = 5f;

	private List<ColorPalette> _themeDatabase;
	private ColorPalette _currentTheme;

	private const int CellSize = 120; //Her bir karenin piksel boyutu
	private const int CellPadding = 5; //Kareler arasi bosluk
	private const int Step = CellSize + CellPadding;

	private const float GridStartX = 60f;
	private const float GridStartY = 350f;

	private readonly float[] _slotXPositions = { 200f, 580f, 960f };
	private const float SlotYPosition = 1800f;
	
	private int _consecutiveHardShapes = 0;
	private const int PITY_THRESHOLD = 5; // 5 zor şekil sonrası rahatlama

	private int _score = 0;
	private int _comboStreak = 0;
	private int _comboCount = 0;
	private Label _scoreLabel;
	private int _highScore = 0;
	private Label _highScoreLabel;
	private const string SaveFilePath = "user://highscore.cfg";

	private AudioStreamPlayer _dropSound;
	private AudioStreamPlayer _clearSound;
	private AudioStreamPlayer _perfectSound;

	private List<BlockShape> _shapeDatabase = new List<BlockShape>();
	private List<DraggableBlock> _activeBlocks = new List<DraggableBlock>();
	private List<Vector2I> _currentShadowCells = new List<Vector2I>();
	
	private List<int> _hoverClearRows = new List<int>();
	private List<int> _hoverClearCols = new List<int>();
	private bool _isHovering = false;
	private Control _highlightOverlay; // Tüm blokların üstüne çizeceğimiz katman
	private Vector2I _previewGridPos;
	private List<Vector2I> _previewShapeCoords;
	private Color _previewColor;
	private bool _isPreviewValid; // Blok oraya konabiliyor mu?
	
	// Sahne Referansı 
	private PackedScene _blockScene = GD.Load<PackedScene>("res://draggable_block.tscn");
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_themeDatabase = new List<ColorPalette>
		{
			
			new ColorPalette(
				new Color("#fdf0d5"), 
				new Color("#f18701"), 
				new Color("#669bbc"), 
				new Color("#dda15e"),
				new Color("#3d348b"),
				new Color("#7678ed")
			),
			new ColorPalette(
				new Color("#8C7AE6"), 
				new Color("#f18701"), 
				new Color("#3EBB9D"), 
				new Color("#dda15e"),
				new Color("#0F141E"),
				new Color("#1A2130")
			),
			new ColorPalette(
				new Color("#8C7AE6"), 
				new Color("#f18701"), 
				new Color("#3EBB9D"), 
				new Color("#dda15e"),
				new Color("#0F141E"),
				new Color("#1A2130")
			),
			new ColorPalette(
				new Color("#B56576"), 
				new Color("#4682B4"),
				new Color("#E56B6F"), 
				new Color("#D4A373"), 
				new Color("#EAE6DF"),
				new Color("#D7D1C7")
			),
			new ColorPalette(
				new Color("#7D5A80"), 
				new Color("#659DBD"),
				new Color("#C38D9E"), 
				new Color("#E27D60"), 
				new Color("#E2E6E9"),
				new Color("#D4D9DE")
			),
			new ColorPalette(
				new Color("#5C738B"), 
				new Color("#C19C70"),
				new Color("#BA7A70"), 
				new Color("#798A70"), 
				new Color("#F0ECE1"),
				new Color("#E0DCD1")
			),
			new ColorPalette(
				new Color("#8FBC8F"), 
				new Color("#798A70"), 
				new Color("#CDE8B5"), 
				new Color("#2E8B57"),
				new Color("#121A15"),
				new Color("#1A261F")
			),
			new ColorPalette(
				new Color("#48cae4"), 
				new Color("#ef476f"), 
				new Color("#caf0f8"), 
				new Color("#ade8f4"),
				new Color("#33415c"),
				new Color("#5c677d")
			),
			new ColorPalette(
				new Color("#D92525"), 
				new Color("#1E88E5"), 
				new Color("#3EBB9D"), 
				new Color("#F5A623"),
				new Color("#1C1E22"),
				new Color("#25282E")
			),
		};
		_currentTheme = _themeDatabase[Random.Shared.Next(_themeDatabase.Count)];
		RenderingServer.SetDefaultClearColor(_currentTheme.BgColor);
		
		InitializeGrid();
		DrawGridVisuals(); // Calisir calismaz pikselleri erkana basar
		LoadHighScore();
		SetupScoreUI();
		LoadShapeDatabase();

		_dropSound = GetNode<AudioStreamPlayer>("DropSoundPlayer");
		_clearSound = GetNode<AudioStreamPlayer>("ClearSoundPlayer");
		_perfectSound = GetNode<AudioStreamPlayer>("PerfectClearSoundPlayer");

		SpawnInitialBlocks();

	}
	
	public override void _Process(double delta)
	{
		// Ekranda patlayacak bir yer gösteriliyorsa gökkuşağı animasyonu aksın diye her frame overlay'i güncelle!
		if (_isHovering && (_hoverClearRows.Count > 0 || _hoverClearCols.Count > 0))
		{
			_highlightOverlay?.QueueRedraw(); 
		}
	}

	private void InitializeGrid()
	{
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 8; y++)
			{
				_grid[x, y] = 0; // Baslangicta tum tahtayi temizlemek icin
			}
		}
		
		GD.Print("Grid 9x9 olarak basariyla olusturuldu ve sifirlandi");
	}

	public bool IsCellEmpty(int x, int y)
	{
		// koordinat kontrolu
		if (x < 0 || x >= 8 || y < 0 || y >= 8)
			return false;

		// Tahtanin icindeyse hucrenin degeri 0 ise true doner
		return _grid[x, y] == 0;
	}

	// Bloğun hedef koordinata (targetX, targetY) sığıp sığmadığını denetler
	public bool CanPlaceBlock(BlockShape block, int targetX, int targetY)
	{
		foreach (Vector2I offset in block.LocalCoordinates)
		{
			int checkX = targetX + offset.X;
			int checkY = targetY + offset.Y;

			if (!IsCellEmpty(checkX, checkY))
			{
				return false; 
			}
			
		}
		return true; 
	}
	
	// Blogu tahtaya kalici olarak yerlestirir
	public void PlaceBlock(BlockShape block, int targetX, int targetY)
	{
		if (!CanPlaceBlock(block, targetX, targetY))
		{
			GD.PrintErr("Hata: Blok buraya yerleştirilemez!");
			return;
		}

		foreach (Vector2I offset in block.LocalCoordinates)
		{
			int finalX = targetX + offset.X;
			int finalY = targetY + offset.Y;

			_grid[finalX, finalY] = (int)block.Category + 1;
		}

		GD.Print($"Blok başarıyla {targetX}, {targetY} merkezine yerleştirildi.");

		int placementScore = block.LocalCoordinates.Count * 10;
		AddScore(placementScore, "Blok Yerlestirme");
		
		CheckAndClearlines();
	}

	public void CheckAndClearlines()
	{
		// Temizlenecek hucrelerin koordinatlarinin tutalacagi kume
		HashSet<Vector2I> cellsToClear = new HashSet<Vector2I>();

		int linesCleared = 0; // Kombo hesabi icin
		
		//Satir kontrolu
		for (int y = 0; y < 8; y++)
		{
			bool isRowFull = true;
			for (int x = 0; x < 8; x++)
			{
				if (_grid[x,y] == 0)
				{
					isRowFull = false;
					break;
				}
			}

			if (isRowFull)
			{
				linesCleared++;
				for (int x = 0; x < 8; x++)
				{
					cellsToClear.Add(new Vector2I(x, y));
				}
			}
		}
		
		//Sutun kontrolu
		for (int x = 0; x < 8; x++)
		{
			bool isColFull = true;
			for (int y = 0; y < 8; y++)
			{
				if (_grid[x,y] == 0)
				{
					isColFull = false;
					break;
				}
			}

			if (isColFull)
			{
				linesCleared++;
				for (int y = 0; y < 8; y++)
				{
					cellsToClear.Add(new Vector2I(x, y));
				}
			}
		}
		
		//Temizleme ve kombo erkani
		if (cellsToClear.Count > 0)
		{
			_clearSound.Play();
			_comboStreak++;
			
			foreach (Vector2I cell in cellsToClear)
			{
				_grid[cell.X, cell.Y] = 0;
			}
			
			GD.Print($"BOOM! {linesCleared} adet çizgi/bölge patlatıldı! Toplam silinen hücre: {cellsToClear.Count}");

			// int baseScorePerLine = 100;
			// int comboMultiplier = linesCleared + (_comboStreak - 1);
			// int clearScore = (linesCleared * baseScorePerLine) * comboMultiplier;
			
			int baseScore = (linesCleared * linesCleared) * 100; 
       
			// Çarpanı kombo serisiyle katlıyoruz!
			int clearScore = baseScore * _comboStreak;
			
			Vector2 boardCenter = new Vector2(GridStartX + (4 * Step), GridStartY + (4 * Step));
			
			// Eger birden fazla bolge ayni anda patlatildiysa
			if (linesCleared > 1 || _comboStreak > 1)
			{
				GD.Print($"BOOM! {linesCleared} ÇİZGİ PATLADI! | SERİ: x{_comboStreak} | ÇARPAN: X{_comboStreak}");
				AddScore(clearScore, $"X{_comboStreak} KOMBO PATLATMASI");
				
				// KOMBO YAZISINI FIRLAT (Altın sarısı ve 1.5x devasa boyutta)
				ShowFloatingText(boardCenter, $"{linesCleared} LINE COMBO!\n+{clearScore}", new Color(1f, 0.84f, 0f), 1.5f);
			}
			else
			{
				AddScore(clearScore, "Tekli Patlatma");
				
				// NORMAL PATLATMA YAZISI (Beyaz ve standart boyutta)
				ShowFloatingText(boardCenter, $"+{clearScore}", new Color(1f, 1f, 1f), 1.0f);
			}

			if (IsBoardEmpty())
			{
				GD.Print("MÜKEMMEL TEMİZLİK! TAHTADA HİÇBİR ŞEY KALMADI!");
				_perfectSound.Play();
				AddScore(4000,"PERFECT CLEAR!");
				
				// PERFECT YAZISI (Fosforlu Yeşil ve 2x devasa boyutta biraz daha yukarıdan)
				ShowFloatingText(boardCenter + new Vector2(0, -60), "PERFECT CLEAR!\n+4000", new Color(0.2f, 1f, 0.2f), 2.0f);
			}
		}
		else
		{
			if (_comboStreak > 7)
			{
				_comboStreak--;
			}
		}
		
		SyncVisuals();
	}

	private void DrawGridVisuals()
	{
		
		// ── 1. ANA OYUN TAHTASI ÇERÇEVESİ (KASA/BOARD FRAME) ──
		float padding = 12f; 
		Panel boardFrame = new Panel();
		boardFrame.Size = new Vector2((8 * Step) + (padding * 2), (8 * Step) + (padding * 2));
		boardFrame.Position = new Vector2(GridStartX - padding, GridStartY - padding);

		StyleBoxFlat frameStyle = new StyleBoxFlat();
		Color frameColor = GetColorForBlock(0).Darkened(0.2f); 
		frameStyle.BgColor = frameColor;
	   
		frameStyle.CornerRadiusTopLeft = 16;
		frameStyle.CornerRadiusTopRight = 16;
		frameStyle.CornerRadiusBottomLeft = 16;
		frameStyle.CornerRadiusBottomRight = 16;

		frameStyle.BorderWidthTop = 2;
		frameStyle.BorderWidthLeft = 2;
		frameStyle.BorderWidthBottom = 10; 
		frameStyle.BorderWidthRight = 10;
		frameStyle.BorderColor = frameColor.Darkened(0.3f);
	   
		frameStyle.ShadowColor = new Color(0f, 0f, 0f, 0.4f);
		frameStyle.ShadowSize = 12;
		frameStyle.ShadowOffset = new Vector2(6, 6);

		boardFrame.AddThemeStyleboxOverride("panel", frameStyle);
		AddChild(boardFrame);

		// ── 2. IZGARA (KLAVYE PLAKASI VE BOŞLUKLAR) ──
		float gap = 6f; 
		float actualSize = Step - gap;
		_cellActualSize  = actualSize;

		//Color slotColor = GetColorForBlock(0);
		
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 8; y++)
			{
				float xPos = GridStartX + x * Step + gap / 2;
				float yPos = GridStartY + y * Step + gap / 2;

				BevelCell cell   = new BevelCell();
				cell.Size        = new Vector2(actualSize, actualSize);
				cell.Position    = new Vector2(xPos, yPos);
				cell.MainColor   = GetColorForBlock(0).Lightened(0.15f); // boş yuva rengi
				AddChild(cell);
				_cellGrid[x, y] = cell;

			}
		}
		SyncVisuals();
		// DrawGridVisuals() metodunun en altı:
		_highlightOverlay = new Control();
		_highlightOverlay.ZIndex = 10; // Her şeyin en üstünde parlasın
		AddChild(_highlightOverlay);
		_highlightOverlay.Draw += DrawHighlights; // Çizim görevini metodumuza bağlıyoruz
	}
	
	private void LoadShapeDatabase()
	{
		// KOMBOCU BABALAR 
		_shapeDatabase.Add(new BlockShape(10, 30, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0,1), new Vector2I(1,1), new Vector2I(-1,1), new Vector2I(0,2), new Vector2I(1,2), new Vector2I(-1,2) })); // 3x3 kare
		_shapeDatabase.Add(new BlockShape(7, 45, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0,1), new Vector2I(1,1), new Vector2I(-1,1) })); // 3x2 dikdortgen yatay
		_shapeDatabase.Add(new BlockShape(11, 45, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(0, 1), new Vector2I(0, 0), new Vector2I(0, -1), new Vector2I(1,0), new Vector2I(1,1), new Vector2I(1,-1) })); // 3x2 dikdortgen dikey
		_shapeDatabase.Add(new BlockShape(6, 35, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(-2, 0), new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0) })); // 5 birim cubuk yatay
		_shapeDatabase.Add(new BlockShape(11, 35, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(0, 2), new Vector2I(0, 1), new Vector2I(0, 0), new Vector2I(0, -1), new Vector2I(0, -2) })); // 5 birim cubuk dikey
		_shapeDatabase.Add(new BlockShape(6, 40, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0) })); // 4 birim cubuk yatay
		_shapeDatabase.Add(new BlockShape(11, 40, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(0, 2), new Vector2I(0, 1), new Vector2I(0, 0), new Vector2I(0, -1) })); // 4 birim cubuk dikey
		_shapeDatabase.Add(new BlockShape(2, 55, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0) })); // 3 birim cubuk yatay
		_shapeDatabase.Add(new BlockShape(11, 55, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(0, 1), new Vector2I(0, 0), new Vector2I(0, -1) })); // 3 birim cubuk dikey
		_shapeDatabase.Add(new BlockShape(3, 55, ShapeCategory.ComboMaker,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0, 1), new Vector2I(1, 1) })); // 2x2 Kare
		
		// ORTA ŞEKİLLER 
		_shapeDatabase.Add(new BlockShape(8, 25, ShapeCategory.Medium,new List<Vector2I> {  new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1,0), new Vector2I(0,1) })); // T
		_shapeDatabase.Add(new BlockShape(8, 25, ShapeCategory.Medium,new List<Vector2I> {  new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1,0), new Vector2I(0,-1) })); // ters T
		_shapeDatabase.Add(new BlockShape(8, 25, ShapeCategory.Medium,new List<Vector2I> {  new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0,-1) })); // sol T
		_shapeDatabase.Add(new BlockShape(8, 25, ShapeCategory.Medium,new List<Vector2I> {  new Vector2I(1, 0), new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0,-1) })); // sag T
		_shapeDatabase.Add(new BlockShape(5, 15, ShapeCategory.Medium,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0), new Vector2I(0, -1), new Vector2I(0, -2) })); // Uzun L (5 birim)
		_shapeDatabase.Add(new BlockShape(11, 15, ShapeCategory.Medium,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0, 2), new Vector2I(-1, 0), new Vector2I(-2, 0) })); // Uzun L 180deg (5 birim)
		_shapeDatabase.Add(new BlockShape(1, 20, ShapeCategory.Medium,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0, -1), new Vector2I(0, -2) })); // Kisa L (4 birim)
		_shapeDatabase.Add(new BlockShape(11, 20, ShapeCategory.Medium,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0), new Vector2I(0, 1) })); // Kisa L 90deg (4 birim)
		_shapeDatabase.Add(new BlockShape(11, 20, ShapeCategory.Medium,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0, 2), new Vector2I(-1, 0) })); // Kisa L 180deg (4 birim)
		_shapeDatabase.Add(new BlockShape(11, 20, ShapeCategory.Medium,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, -1), new Vector2I(-2, 0), new Vector2I(-1, 0) })); // Kisa L 270deg (4 birim)
		
		// KUCUK SEKILLER
		_shapeDatabase.Add(new BlockShape(11, 20, ShapeCategory.Small,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(1, 0) })); // Mini L 90deg (3 birim)
		_shapeDatabase.Add(new BlockShape(11, 20, ShapeCategory.Small,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, -1), new Vector2I(-1, 0) })); // Mini L 270deg (3 birim)
		_shapeDatabase.Add(new BlockShape(11, 45, ShapeCategory.Small,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0) })); // 2x1 Dikdortgen yatay
		_shapeDatabase.Add(new BlockShape(4, 45, ShapeCategory.Small,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1) })); // 2x1 Dikdortgen dikey
		
		
		// PİS ŞEKİLLER 
		_shapeDatabase.Add(new BlockShape(9, 5, ShapeCategory.Nasty,new List<Vector2I> { new Vector2I(1, -1), new Vector2I(0, 0), new Vector2I(-1, 1) })); // o.ç 
		_shapeDatabase.Add(new BlockShape(11, 10, ShapeCategory.Small,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(-1, 1), new Vector2I(1, 0) })); // S 
		_shapeDatabase.Add(new BlockShape(11, 10, ShapeCategory.Small,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, -1), new Vector2I(-1, -1), new Vector2I(1, 0) })); // Z 
		_shapeDatabase.Add(new BlockShape(11, 10, ShapeCategory.Small,new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(1, -1), new Vector2I(1, 0) })); // Z 90deg 
	}
	
	// --- ÜRETİM SİSTEMİ ---
	private void SpawnInitialBlocks()
	{
		for (int i = 0; i < 3; i++) 
			SpawnSingleBlock(i);
	}

	public void SyncVisuals()
	{
		
		Color emptyColor = GetColorForBlock(0).Lightened(0.15f);
		
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 8; y++)
			{
				int cellValue = _grid[x, y];

				_cellGrid[x, y].IsEmpty    = cellValue == 0;
				_cellGrid[x, y].MainColor  = cellValue == 0
					? emptyColor
					: GetColorForBlock(cellValue);
			}
		}
	}

	private Color GetColorForBlock(int cellValue)
	{
		
		// Tahtadaki (Grid) değere göre doğrudan temanın rengini basıyoruz!
		return cellValue switch
		{
			0 => _currentTheme.EmptyGridColor, // Boş tahta hücresi
			1 => _currentTheme.ComboColor,    // (ComboMaker)
			2 => _currentTheme.MediumColor,   // (Medium)
			3 => _currentTheme.SmallColor,    // (Small)
			4 => _currentTheme.NastyColor,    // (Nasty)
			_ => _currentTheme.EmptyGridColor  // Güvenlik ağı
		};
	}

	private Vector2I PixelToGridIndex(Vector2 pixelPos)
	{
		
		int x = Mathf.FloorToInt((pixelPos.X - GridStartX) / (float)Step);
		int y = Mathf.FloorToInt((pixelPos.Y - GridStartY) / (float)Step);

		return new Vector2I(x, y);
	}
	
	// DraggableBlock birakildiginda tetiklenecek metot
	private void HandleBlockDropped(DraggableBlock block, Vector2 dropPosition)
	{
		// Gelen piksel matris indeksine cevir
		Vector2I targetIndex = PixelToGridIndex(dropPosition);
		ClearShadows();
		GD.Print($"Hedef indeks hesaplandi: X:{targetIndex.X}, Y:{targetIndex.Y}");
		
		// Blok hedefe sigiyormu ?
		if (CanPlaceBlock(block.ShapeData, targetIndex.X, targetIndex.Y))
		{
			_dropSound.Play();
			
			PlaceBlock(block.ShapeData, targetIndex.X, targetIndex.Y);
			_activeBlocks.Remove(block);
			
			CheckAndClearlines();
			ClearHoverHighlight();
			SyncVisuals();
			
			int emptiedSlot = block.SlotIndex;
			block.QueueFree();
			
			//SpawnSingleBlock(emptiedSlot); // Boşalan yere anında yenisi
			
			// Eğer elimizdeki 3 blok da bittiyse yeni 3'lü dalgayı çağır
			if (_activeBlocks.Count == 0)
			{
				GD.Print("Tüm bloklar yerleştirildi! Yeni 3'lü set geliyor...");
				SpawnInitialBlocks(); // Zaten hazırda olan 0, 1, 2 slotlarını dolduran metodumuz
			}

			if (CheckForGameOver())
			{
				GD.Print("HİÇBİR ŞEKİL SIĞMIYOR. GAME OVER USTA!");
				ShowGameOverScreen();
			}
		}
		else
		{
			GD.Print("HATA: Blok o bölgeye sığmıyor veya dışarı taşıyor!");

			// Blogun sag tarafta belirdigi yer
			Vector2 originalPosition = GetCenteredSlotPosition(block.ShapeData, block.SlotIndex);

			// Godot 4"un Tween motoru bu isi yapacak
			Tween tween = CreateTween();
			tween.SetParallel(true); // Pozisyon ve Boyut animasyonlarını aynı anda çalıştırır

			tween.TweenProperty(block, "position", originalPosition, 0.2f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			
			// Yolda giderken %50 boyutuna geri küçült
			tween.TweenProperty(block, "scale", new Vector2(0.5f, 0.5f), 0.2f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}
	}

	private void HandleBlockRotated(DraggableBlock block)
	{
		// Yeni ağırlık merkezine göre kusursuz pozisyonu hesaplıyoruz
		Vector2 newCenteredPos = GetCenteredSlotPosition(block.ShapeData, block.SlotIndex);
		
		// Şekil döndüğü an küt diye ışınlanmasın, jilet gibi pürüzsüzce merkeze kaysın
		Tween tween = CreateTween();
		tween.TweenProperty(block, "position", newCenteredPos, 0.1f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}

	private void SpawnSingleBlock(int slotIndex)
	{
		DraggableBlock newBlock = _blockScene.Instantiate<DraggableBlock>();
		
		// Random random = new Random();
		// BlockShape templateShape = _shapeDatabase[random.Next(_shapeDatabase.Count)];

		newBlock.OnBlockDragging += HandleBlockDragging;
		
		BlockShape templateShape = GetDynamicRandomShape();
		
		// Şeklin orijinalini bozmamak için koordinat listesinin bir kopyasını alıyoruz!
		List<Vector2I> clonedCoords = new List<Vector2I>(templateShape.LocalCoordinates);
	   
		newBlock.ShapeData = new BlockShape(templateShape.BlockId, templateShape.Weight, templateShape.Category, clonedCoords);
		newBlock.SlotIndex = slotIndex;
		
		newBlock.ActiveThemeColor = templateShape.Category switch
		{
			ShapeCategory.ComboMaker => _currentTheme.ComboColor,
			ShapeCategory.Medium     => _currentTheme.MediumColor,
			ShapeCategory.Small      => _currentTheme.SmallColor,
			ShapeCategory.Nasty      => _currentTheme.NastyColor,
			_                        => new Color(0.8f, 0.2f, 0.2f) // Güvenlik ağı
		};
		
		// Blok doğar doğmaz boyutunu %50 küçültüyoruz ki ekrana sığsın
		newBlock.Scale = new Vector2(0.5f, 0.5f);
		
		//newBlock.Position = new Vector2(_slotXPositions[slotIndex], SlotYPosition);
		newBlock.Position = GetCenteredSlotPosition(newBlock.ShapeData, slotIndex);
		
		newBlock.OnBlockDropped += HandleBlockDropped;
		newBlock.OnBlockRotated += HandleBlockRotated;
		
		AddChild(newBlock);
		_activeBlocks.Add(newBlock);
	}
	
	private void HandleBlockDragging(DraggableBlock block, Vector2 dragPosition)
	{
		Vector2I targetIndex = PixelToGridIndex(dragPosition);
		
		float firstCellCenterX = GridStartX + (Step / 2f);
		float firstCellCenterY = GridStartY + (Step / 2f);
		
		int gridx = Mathf.RoundToInt((dragPosition.X - firstCellCenterX) / Step);
		int gridy = Mathf.RoundToInt((dragPosition.Y - firstCellCenterY) / Step);
		
		Vector2I currentGridPos = new Vector2I(gridx, gridy);
		
		UpdateHoverHighlight(currentGridPos, block.ShapeData.LocalCoordinates, block.ActiveThemeColor);

		
		ClearShadows();

		// Eğer blok o anki yere tam sığıyorsa yeni gölgeleri hesapla ve çiz
		if (CanPlaceBlock(block.ShapeData, targetIndex.X, targetIndex.Y))
		{
			// Gölge rengini ID'den değil, Kategoriden çekiyoruz! ──
			Color shadowColor = block.ShapeData.Category switch
			{
				ShapeCategory.ComboMaker => _currentTheme.ComboColor,
				ShapeCategory.Medium     => _currentTheme.MediumColor,
				ShapeCategory.Small      => _currentTheme.SmallColor,
				ShapeCategory.Nasty      => _currentTheme.NastyColor,
				_                        => new Color(1f, 1f, 1f) // Güvenlik ağı
			};
	   
			shadowColor.A = 0.5f; // Rengi %50 saydam (şeffaf) yapıyoruz ki "Gölge" gibi dursun!

			foreach (Vector2I offset in block.ShapeData.LocalCoordinates)
			{
				int gridX = targetIndex.X + offset.X;
				int gridY = targetIndex.Y + offset.Y;

				_currentShadowCells.Add(new Vector2I(gridX, gridY));

				// Hücrenin Panel stilini al ve saydam renge boya
				StyleBoxFlat style = (StyleBoxFlat)_cellGrid[gridX, gridY].GetThemeStylebox("panel");
				style.BgColor = shadowColor;
			}
		}
	}

	private void ClearShadows()
	{
		if (_currentShadowCells.Count > 0)
		{
			_currentShadowCells.Clear();
			SyncVisuals(); // SyncVisuals çağırdığımızda tahta zaten gerçek/orijinal renklerine geri döner
		}
	}

	private void AddScore(int points, string reason)
	{
		_score += points;
		GD.Print($"+{points} PUAN ({reason}) ---> TOPLAM PUAN: {_score}");

		if (_scoreLabel != null)
		{
			_scoreLabel.Text = _score.ToString();
		}

		if (_score > _highScore)
		{
			_highScore = _score;
			if (_highScoreLabel != null)
			{
				_highScoreLabel.Text = $"🏆 {_highScore}";
			}
			SaveHighScore();
		}
	}

	private void SetupScoreUI()
{
	// ── 1. EKRANIN SOL ÜST KÖŞESİ (REKOR VE SEMBOL) ──
	_highScoreLabel = new Label();
	// Ekrana tam yapışmasın, sol üstten 30 piksel boşluk bıraksın
	_highScoreLabel.Position = new Vector2(80, 120);
	
	// Başına kral tacı veya kupa emojisi ekliyoruz. (Godot'un varsayılan fontu emojileri okur)
	_highScoreLabel.Text = $"🏆{_highScore}";
	_highScoreLabel.AddThemeFontSizeOverride("font_size", 54);
	_highScoreLabel.AddThemeColorOverride("font_color", new Color("#FFD700"));
	_highScoreLabel.AddThemeConstantOverride("outline_size", 6);
	_highScoreLabel.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
	_highScoreLabel.AddThemeConstantOverride("shadow_offset_y", 3);
	AddChild(_highScoreLabel);


	// ── 2. OYUN ALANININ BİRAZCIK ÜSTÜ, TAM ORTASI (ANLIK PUAN) ──
	_scoreLabel = new Label();
	
	// Yazıyı kendi içinde tam ortaya hizala
	_scoreLabel.HorizontalAlignment = HorizontalAlignment.Center;
	_scoreLabel.VerticalAlignment = VerticalAlignment.Center;
	
	// İŞTE SIR BURADA: Label'ın genişliğini tam olarak "8x8 Oyun Tahtası" kadar yapıyoruz.
	// Böylece X pozisyonunu tahtayla aynı başlattığımızda, yazı otomatik olarak tahtanın tam ortasına jilet gibi hizalanır!
	float gridTotalWidth = 8 * Step;
	_scoreLabel.Size = new Vector2(gridTotalWidth, 60); 
	
	// Tahtanın başlangıcından (GridStartY) 80 piksel yukarıya koyuyoruz
	_scoreLabel.Position = new Vector2(GridStartX, GridStartY - 120); 
	
	// Puanı sadece sayı olarak büyükçe yazdırırsan çok daha klas ve modern durur
	_scoreLabel.Text = "0"; 
	_scoreLabel.AddThemeFontSizeOverride("font_size", 64); // Puan kocaman ve heybetli olsun
	_scoreLabel.AddThemeColorOverride("font_color", new Color("#ffffff")); // Bembeyaz patlasın
	_scoreLabel.AddThemeConstantOverride("outline_size", 4);
	_scoreLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.5f));
	AddChild(_scoreLabel);
}
	
	private void DecorateScoreLabel(Label label, bool isHighScore)
	{
		StyleBoxFlat style = new StyleBoxFlat();
	
		// 1. Kasa Rengi: Askeri / Hacker temanın o neredeyse siyah arka planı
		style.BgColor = new Color("#0A120D"); 
	
		// Köşe kavisleri (Fiziksel, tok bir tuş/kasa hissiyatı)
		style.CornerRadiusTopLeft = 6;
		style.CornerRadiusTopRight = 6;
		style.CornerRadiusBottomLeft = 6;
		style.CornerRadiusBottomRight = 6;

		// 2. 3D Ağırlık Efekti (Sadece alt tarafı kalın kasa)
		style.BorderWidthTop = 0;
		style.BorderWidthLeft = 0;
		style.BorderWidthBottom = 4;
		style.BorderWidthRight = 0;
		style.BorderColor = new Color("#050806"); // Kasanın en altındaki zifiri karanlık gölge

		// 3. İç Boşluk (Yazı kasanın duvarlarına yapışmasın diye nefes aldırıyoruz)
		style.ContentMarginLeft = 16;
		style.ContentMarginRight = 16;
		style.ContentMarginTop = 8;
		style.ContentMarginBottom = 8;

		// Stili Label'ın beynine zorla çakıyoruz!
		label.AddThemeStyleboxOverride("normal", style);

		// 4. LED YAZI RENKLERİ (O hardcore yeşil palete uygun olarak)
		// Rekor yazısı bizim "Nasty" rengimizle (Açık Çay Yeşili) fosforlu yansın, diğeri tok yeşil olsun.
		Color textColor = isHighScore ? new Color("#CDE8B5") : new Color("#8FBC8F"); 
		label.AddThemeColorOverride("font_color", textColor);
	
		// Yazının arkasına LCD ekran gölgesi atıyoruz ki düz metin gibi durmasın
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.8f));
		label.AddThemeConstantOverride("shadow_offset_y", 2);
	}

	private void LoadHighScore()
	{
		ConfigFile config = new ConfigFile();
		Error err = config.Load(SaveFilePath);

		if (err == Error.Ok)
		{
			_highScore = (int)config.GetValue("Player", "HighScore", 0);
		}
		else
		{
			_highScore = 0;
		}
	}

	private void SaveHighScore()
	{
		ConfigFile config = new ConfigFile();
		config.SetValue("Player", "HighScore", _highScore);
		config.Save(SaveFilePath);
	}

	// Asagidaki 3'lu sekilleri hizalama
	private Vector2 GetCenteredSlotPosition(BlockShape shape, int slotIndex)
	{
		int minX = int.MaxValue, maxX = int.MinValue;
		int minY = int.MaxValue, maxY = int.MinValue;
		
		// Şeklin sınırlarını buluyoruz
		foreach (Vector2I coord in shape.LocalCoordinates)
		{
			if (coord.X < minX) minX = coord.X;
			if (coord.X > maxX) maxX = coord.X;
			if (coord.Y < minY) minY = coord.Y;
			if (coord.Y > maxY) maxY = coord.Y;
		}

		// Piksel cinsinden şeklin kapladığı alanın sınırları
		float leftEdge = minX * Step;
		float rightEdge = (maxX * Step) + CellSize;
		float topEdge = minY * Step;
		float bottomEdge = (maxY * Step) + CellSize;

		// Bloklar yuvada %50 (0.5f) küçültüleceği için merkez hesabını da yarıya indiriyoruz
		float scaleFactor = 0.5f; 
		float centerX = ((leftEdge + rightEdge) / 2f) * scaleFactor;
		float centerY = ((topEdge + bottomEdge) / 2f) * scaleFactor;

		// Slotumuzun konumu
		float targetX = _slotXPositions[slotIndex];
		float targetY = SlotYPosition;

		// Şeklin merkezini, slotun merkezine oturt!
		return new Vector2(targetX - centerX, targetY - centerY);
	}

	private BlockShape GetDynamicRandomShape()
	{
		float fullness = GetBoardFullnessRatio();
		
		if (_consecutiveHardShapes >= PITY_THRESHOLD)
		{
			_consecutiveHardShapes = 0;
			var easyShapes = _shapeDatabase
				.Where(s => s.Category == ShapeCategory.Small)
				.ToList();
			return easyShapes[Random.Shared.Next(easyShapes.Count)];
		}
		
		int totalDynamicWeight = 0;

		Dictionary<BlockShape, int> dynamicWeights = new();

		foreach (BlockShape shape in _shapeDatabase)
		{
			int currentWeight = shape.Weight;

			if (fullness < 0.25f)
			{
				// ── OYUN BAŞI: Kombo yağmuru ────────────────────────
				currentWeight = shape.Category switch
				{
					ShapeCategory.ComboMaker => currentWeight * 3,   // Büyükleri 3 kat artır
					ShapeCategory.Medium => currentWeight / 2,   // Ortaları yarıya indir
					ShapeCategory.Small => 0,                   // Mini şekil yok
					ShapeCategory.Nasty => 0,                   // Pis şekil yok
					_ => currentWeight
				};
			}
			else if (fullness <= 0.65f)
			{
				// ── ORTA OYUN: Dengeli ──────────────────────────────
				currentWeight = shape.Category switch
				{
					ShapeCategory.ComboMaker => currentWeight,
					ShapeCategory.Medium => (int)(currentWeight * 1.3f),
					ShapeCategory.Small => currentWeight,
					ShapeCategory.Nasty => currentWeight / 3,   // Pisleri bastır
					_ => currentWeight
				};
			}
			else
			{
				// ── TAHTA DOLU: Hayat kurtar ─────────────────────────
				int cellCount = shape.LocalCoordinates.Count;
				currentWeight = shape.Category switch
				{
					ShapeCategory.ComboMaker => cellCount >= 6 ? 1 : currentWeight / 3,
					ShapeCategory.Medium => (int)(currentWeight * 1.5f),
					ShapeCategory.Small => currentWeight * 5,   // Küçükleri şelale gibi akıt
					ShapeCategory.Nasty => 0,                   // Doluyken pis şekil = game over
					_ => currentWeight
				};
			}

			if (currentWeight > 0)
			{
				dynamicWeights[shape] = currentWeight;
				totalDynamicWeight += currentWeight;
			}
		}

		if (totalDynamicWeight == 0) return _shapeDatabase[0];

		int randomValue = Random.Shared.Next(0, totalDynamicWeight);
		int cumulativeWeight = 0;
		BlockShape selected = _shapeDatabase[0];
	
		foreach (var kvp in dynamicWeights)
		{
			cumulativeWeight += kvp.Value;
			if (randomValue < cumulativeWeight)
			{
				selected = kvp.Key; // Şekli bulduk, kaydettik
				break; 
			}
		}

		bool isHard = selected.Category == ShapeCategory.Nasty ||
					  selected.LocalCoordinates.Count >= 6;
		_consecutiveHardShapes = isHard ? _consecutiveHardShapes + 1 : 0;

		return selected;
	}

	private float GetBoardFullnessRatio()
	{
		int filledCount = 0;
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 8; y++)
			{
				if (_grid[x,y] != 0)
				{
					filledCount++;
				}
			}
		}

		return (float)filledCount / 64f;
	}

	private bool IsBoardEmpty()
	{
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 8; y++)
			{
				if (_grid[x, y] != 0)
				{
					return false;
				}
			}
		}

		return true;
	}
	
	private void ShowFloatingText(Vector2 startPosition, string text, Color textColor, float scale = 1.0f)
	{
		Label floatingLabel = new Label();
		floatingLabel.Text = text;
		
		// Yazı için Jilet gibi bir tasarım (Style) ayarı
		LabelSettings settings = new LabelSettings();
		settings.FontSize = (int)(32 * scale); // Font boyutunu kombo büyüklüğüne göre büyüt!
		settings.FontColor = textColor;
		settings.OutlineSize = 6;
		settings.OutlineColor = new Color(0, 0, 0, 0.8f); // Yazı okunsun diye kalın siyah kenarlık
		floatingLabel.LabelSettings = settings;

		floatingLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

		floatingLabel.ZIndex = 200; // Tüm blokların ve tahtanın üstünde çıksın
		AddChild(floatingLabel);

		// Yazıyı tam verilen koordinata ortalamak için ufak bir matematik
		// (Geçici bir font boyutu tahmini ile ortalıyoruz)
		floatingLabel.Position = startPosition - new Vector2(text.Length * (settings.FontSize / 4f), settings.FontSize / 2f);

		// GODOT TWEEN (ANİMASYON) MOTORU 
		Tween tween = CreateTween();
		tween.SetParallel(true); // Pozisyon ve Saydamlık animasyonları AYNI ANDA çalışsın

		// 1 saniye içinde 120 piksel yukarı süzül
		tween.TweenProperty(floatingLabel, "position", floatingLabel.Position + new Vector2(0, -120), 1.0f)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		
		// 1 saniye içinde yavaşça saydamlaşarak (Alpha = 0) kaybol
		tween.TweenProperty(floatingLabel, "modulate:a", 0.0f, 1.0f)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);

		// Animasyon bittiği milisaniye objeyi sahnede yok et 
		tween.Chain().TweenCallback(Callable.From(floatingLabel.QueueFree));
	}

	private bool CheckForGameOver()
	{
		if (_activeBlocks.Count == 0)
		{
			return false;
		}

		// Her bir blok icin kontrol
		foreach (DraggableBlock block in _activeBlocks)
		{
			// 64 karenin her birine sigiyormu kontrolu
			for (int x = 0; x < 8; x++)
			{
				for (int y = 0; y < 8; y++)
				{
					if (CanPlaceBlock(block.ShapeData, x, y))
					{
						return false; // En az 1 tane sigacak yer bulundu, oyun devam eder
					}
				}
			}
		}

		return true; // Bloklarin hicbiri sigmadi
	}
	private void ShowGameOverScreen()
	{
		// 1. Arayüz Katmanını Oluştur (Her şeyin en üstünde duracak)
		CanvasLayer gameOverLayer = new CanvasLayer();
		gameOverLayer.Layer = 100;

		// 2. Arka Planı Karart (Sinematik etki)
		ColorRect bgDimmer = new ColorRect();
		bgDimmer.Color = new Color(0f, 0f, 0f, 0.85f); // %85 saydam siyah
		bgDimmer.Size = GetViewportRect().Size; // Tüm ekranı kapla
		bgDimmer.MouseFilter = Control.MouseFilterEnum.Stop; // Tıklamaları YUT, alttaki bloklara geçmesin!
		gameOverLayer.AddChild(bgDimmer);

		// 3. Yazıları ve Butonu Ortalayacak Taşıyıcı Kutu (VBoxContainer)
		VBoxContainer vbox = new VBoxContainer();
		vbox.Size = GetViewportRect().Size;
		vbox.Alignment = BoxContainer.AlignmentMode.Center; // İçindekileri dikeyde ortala
		gameOverLayer.AddChild(vbox);

		// 4. GAME OVER Başlığı
		Label title = new Label();
		title.Text = "HİÇBİR ŞEKİL SIĞMIYOR. \n GAME OVER USTA!";
		title.AddThemeFontSizeOverride("font_size", 100);
		title.AddThemeColorOverride("font_color", new Color(0.9f, 0.2f, 0.2f)); // Kan Kırmızı
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		// 5. Final Skoru
		Label scoreText = new Label();
		scoreText.Text = $"TOPLAM PUAN\n{_score}";
		scoreText.AddThemeFontSizeOverride("font_size", 60);
		scoreText.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(scoreText);

		// 6. Araya Boşluk Bırak
		Control spacer = new Control();
		spacer.CustomMinimumSize = new Vector2(0, 100);
		vbox.AddChild(spacer);

		// 7. Yeniden Başlat Butonu
		Button restartBtn = new Button();
		restartBtn.Text = "TEKRAR OYNA";
		restartBtn.AddThemeFontSizeOverride("font_size", 50);
		restartBtn.CustomMinimumSize = new Vector2(450, 120); // Kocaman buton
		restartBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; // Yatayda ortala
		
		// Butona tıklandığında çalışacak olayı (Event) bağla
		restartBtn.Pressed += RestartGame; 
		
		vbox.AddChild(restartBtn);

		// Arayüzü sahneye ekle
		AddChild(gameOverLayer);
	}
	
	private void RestartGame()
	{
		// Sahneyi komple yeniden yükle (Her şeyi sıfırlar)
		GetTree().ReloadCurrentScene();
	}
	
	// Telegraphing
	public void UpdateHoverHighlight(Vector2I gridTopLeft, List<Vector2I> shapeCoords, Color blockColor)
	{
		_previewGridPos = gridTopLeft;
		_previewShapeCoords = shapeCoords;
		_previewColor = blockColor;
		_isHovering = true;
	
		// 1. Önce bu pozisyon GEÇERLİ Mİ? (Taşıyor mu veya altı dolu mu diye bakıyoruz)
		_isPreviewValid = true;
		foreach (Vector2I offset in shapeCoords)
		{
			int x = gridTopLeft.X + offset.X;
			int y = gridTopLeft.Y + offset.Y;
		
			// Sınır dışıysa veya tahtadaki o hücre zaten doluysa geçersizdir!
			if (x < 0 || x >= 8 || y < 0 || y >= 8 || _grid[x, y] != 0)
			{
				_isPreviewValid = false;
				break;
			}
		}

		_hoverClearRows.Clear();
		_hoverClearCols.Clear();

		// Eğer geçersiz bir yere (dolu bir yere) tutuyorsa patlama hesabı yapma, çık!
		if (!_isPreviewValid)
		{
			_highlightOverlay?.QueueRedraw();
			return;
		}

		// 2. GEÇERLİYSE GEÇİCİ TAHTA HESABI (Patlayacakları buluyoruz)
		int[,] tempGrid = new int[8, 8];
		Array.Copy(_grid, tempGrid, _grid.Length);

		foreach (Vector2I offset in shapeCoords)
		{
			tempGrid[gridTopLeft.X + offset.X, gridTopLeft.Y + offset.Y] = 1; 
		}

		for (int y = 0; y < 8; y++)
		{
			bool isRowFull = true;
			for (int x = 0; x < 8; x++)
			{
				if (tempGrid[x, y] == 0) { isRowFull = false; break; }
			}
			if (isRowFull) _hoverClearRows.Add(y);
		}

		for (int x = 0; x < 8; x++)
		{
			bool isColFull = true;
			for (int y = 0; y < 8; y++)
			{
				if (tempGrid[x, y] == 0) { isColFull = false; break; }
			}
			if (isColFull) _hoverClearCols.Add(x);
		}

		_highlightOverlay?.QueueRedraw(); 
	}

	// Blok tahtanın dışına çıkarsa veya sürükleme bırakılırsa ışıkları kapat
	public void ClearHoverHighlight()
	{
		_isHovering = false;
		_hoverClearRows.Clear();
		_hoverClearCols.Clear();
		_highlightOverlay?.QueueRedraw();
	}
	
	private void DrawHighlights()
	{
		if (!_isHovering || _previewShapeCoords == null) return;

		float gap = 6f; // Tahtadaki aralıkla birebir aynı olmalı
		float actualSize = Step - gap;

		// ── 1. ÖNCE HAYALET BLOĞU (GHOST PREVIEW) ÇİZ ──
		// Geçerliyse kendi renginin %50 saydamı, geçersizse Kırmızı rengin %40 saydamı
		Color ghostColor = _isPreviewValid 
			? new Color(_previewColor.R, _previewColor.G, _previewColor.B, 0.5f) 
			: new Color(1f, 0f, 0f, 0.4f); 

		foreach (Vector2I offset in _previewShapeCoords)
		{
			int gx = _previewGridPos.X + offset.X;
			int gy = _previewGridPos.Y + offset.Y;

			// Grid sınırları içindeyse ekrana çiz
			if (gx >= 0 && gx < 8 && gy >= 0 && gy < 8)
			{
				float px = GridStartX + (gx * Step) + (gap / 2f);
				float py = GridStartY + (gy * Step) + (gap / 2f);
				
				Rect2 rect = new Rect2(px, py, actualSize, actualSize);
				
				// Bloğun cam gibi saydam iç dolgusunu çiziyoruz
				_highlightOverlay.DrawRect(rect, ghostColor, true);
				
				// Bloğa şık, ince, parlak bir çerçeve atıyoruz ki 3D oyun alanında belli olsun
				_highlightOverlay.DrawRect(rect, new Color(1, 1, 1, 0.3f), false, 2f);
			}
		}

		// Eğer geçersiz yere konuyorsa veya patlayacak satır yoksa gökkuşağı çizmeden çık
		if (!_isPreviewValid || (_hoverClearRows.Count == 0 && _hoverClearCols.Count == 0)) return;

		// ── 2. PATLAYACAK YERLERİN GÖKKUŞAĞI NEON ÇİZİMİ ──
		float time = (float)Time.GetTicksMsec() / 1000f;
		Color rainbowColor = Color.FromHsv((time * 2.0f) % 1f, 0.85f, 1f, 0.8f);
		float padding = 4f;

		foreach (int y in _hoverClearRows)
		{
			Rect2 rect = new Rect2(GridStartX - padding, GridStartY + (y * Step) - padding, (8 * Step) + (padding * 2), Step + (padding * 2));
			_highlightOverlay.DrawRect(rect, rainbowColor, false, 4f);
			_highlightOverlay.DrawRect(rect, new Color(rainbowColor.R, rainbowColor.G, rainbowColor.B, 0.15f), true);
		}

		foreach (int x in _hoverClearCols)
		{
			Rect2 rect = new Rect2(GridStartX + (x * Step) - padding, GridStartY - padding, Step + (padding * 2), (8 * Step) + (padding * 2));
			_highlightOverlay.DrawRect(rect, rainbowColor, false, 4f);
			_highlightOverlay.DrawRect(rect, new Color(rainbowColor.R, rainbowColor.G, rainbowColor.B, 0.15f), true);
		}
	}	
	
	
}
