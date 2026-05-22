using Godot;
using System;
using System.Collections.Generic;

public partial class BlockMaster : Node2D
{

	// 9x9 tahtayi temsil edecek 2 bouytlu matris
	private int[,] _grid = new int[8, 8]; 
	
	// Renkli kutucuklari tutacak matris
	private Panel[,] _visualGrid = new Panel[8, 8];

	private const int CellSize = 120; //Her bir karenin piksel boyutu
	private const int CellPadding = 5; //Kareler arasi bosluk
	private const int Step = CellSize + CellPadding;

	private const float GridStartX = 60f;
	private const float GridStartY = 250f;

	private readonly float[] _slotXPositions = { 200f, 580f, 960f };
	private const float SlotYPosition = 1800f;

	private int _score = 0;
	private int _comboStreak = 0;

	private Label _scoreLabel;

	private int _highScore = 0;
	private Label _highScoreLabel;
	private const string SaveFilePath = "user://highscore.cfg";

	private List<BlockShape> _shapeDatabase = new List<BlockShape>();
	private List<DraggableBlock> _activeBlocks = new List<DraggableBlock>();
	
	// Sahne Referansı 
	private PackedScene _blockScene = GD.Load<PackedScene>("res://draggable_block.tscn");
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InitializeGrid();
		DrawGridVisuals(); // Calisir calismaz pikselleri erkana basar
		LoadHighScore();
		SetupScoreUI();
		LoadShapeDatabase();
		SpawnInitialBlocks();

		//TestBlockPlacements();

		// DraggableBlock myBlock = GetNodeOrNull<DraggableBlock>("DraggableBlock");
		//
		// if (myBlock != null)
		// {
		// 	List<Vector2I> lShapeCoords = new List<Vector2I>
		// 	{
		// 		new Vector2I(0, 0),
		// 		new Vector2I(1, 0),
		// 		new Vector2I(0, -1)
		// 	};
		//
		// 	myBlock.ShapeData = new BlockShape(1, lShapeCoords);
		// 	
		// 	// Event baglandi
		// 	myBlock.OnBlockDropped += HandleBlockDropped;
		// }
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

			_grid[finalX, finalY] = block.BlockId; 
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
		
		//3x3 bolge kontrolu
		// for (int regionX = 0; regionX < 3; regionX++)
		// {
		// 	for (int regionY = 0; regionY < 3; regionY++)
		// 	{
		// 		bool isRegionFull = true;
		// 		
		// 		// O andaki 3x3'luk alanin icindeki 9 kucuk hucreyi tarama
		// 		for (int i = 0; i < 3; i++)
		// 		{
		// 			for (int j = 0; j < 3; j++)
		// 			{
		// 				int gridX = (regionX * 3) + i;
		// 				int gridY = (regionY * 3) + j;
		//
		// 				if (_grid[gridX,gridY] == 0)
		// 				{
		// 					isRegionFull = false;
		// 					break;
		// 				}
		// 			}
		//
		// 			if (!isRegionFull)
		// 			{
		// 				break;
		// 			}
		// 		}
		//
		// 		if (isRegionFull)
		// 		{
		// 			linesCleared++;
		// 			for (int i = 0; i < 3; i++)
		// 			{
		// 				for (int j = 0; j < 3; j++)
		// 				{
		// 					cellsToClear.Add(new Vector2I((regionX * 3) + i, (regionY * 3) + j));
		// 				}
		// 			}
		// 		}
		// 	}
		// }
		
		//Temizleme ve kombo erkani
		if (cellsToClear.Count > 0)
		{
			_comboStreak++;
			
			foreach (Vector2I cell in cellsToClear)
			{
				_grid[cell.X, cell.Y] = 0;
			}
			
			GD.Print($"BOOM! {linesCleared} adet çizgi/bölge patlatıldı! Toplam silinen hücre: {cellsToClear.Count}");

			int baseScorePerLine = 100;
			
			int comboMultiplier = linesCleared + (_comboStreak - 1);

			int clearScore = (linesCleared * baseScorePerLine) * comboMultiplier;
			
			// Eger birden fazla bolge ayni anda patlatildiysa
			if (linesCleared > 1 || _comboStreak > 1)
			{
				GD.Print($"BOOM! {linesCleared} ÇİZGİ PATLADI! | SERİ: x{_comboStreak} | ÇARPAN: X{comboMultiplier}");
				AddScore(clearScore, $"X{comboMultiplier} KOMBO PATLATMASI");
			}
			else
			{
				AddScore(clearScore, "Tekli Patlatma");
			}
		}
		else
		{
			_comboStreak = 0;
		}
		
		SyncVisuals();
	}

	private void DrawGridVisuals()
	{
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 8; y++)
			{
				Panel rect = new Panel();
				rect.Size = new Vector2(CellSize, CellSize);
				rect.Position = new Vector2(GridStartX + (x * Step), GridStartY + (y * Step));

				StyleBoxFlat style = new StyleBoxFlat();
				style.BgColor = new Color(0.2f, 0.2f, 0.2f);
             
				// Tahtadaki boş yuvaların da havalı siyah kenarlıkları ve oval köşeleri olsun
				style.BorderWidthTop = 4;
				style.BorderWidthBottom = 4;
				style.BorderWidthLeft = 4;
				style.BorderWidthRight = 4;
				style.BorderColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); 
             
				style.CornerRadiusTopLeft = 12;
				style.CornerRadiusTopRight = 12;
				style.CornerRadiusBottomLeft = 12;
				style.CornerRadiusBottomRight = 12;

				rect.AddThemeStyleboxOverride("panel", style);
             
				AddChild(rect);
				_visualGrid[x, y] = rect;
			}
		}
	}
	
	private void LoadShapeDatabase()
	{
		// KOMBOCU BABLAR (Ağırlık: 50-60)
		_shapeDatabase.Add(new BlockShape(10, 60, new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0,1), new Vector2I(1,1), new Vector2I(-1,1), new Vector2I(0,2), new Vector2I(1,2), new Vector2I(-1,2) })); // 3x3 kare
		_shapeDatabase.Add(new BlockShape(6, 50, new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0) })); // 4 birim cubuk
		_shapeDatabase.Add(new BlockShape(6, 50, new List<Vector2I> { new Vector2I(-2, 0), new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0) })); // 5 birim cubuk
		_shapeDatabase.Add(new BlockShape(2, 50, new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0) })); // 3 birim cubuk
		_shapeDatabase.Add(new BlockShape(3, 55, new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0, 1), new Vector2I(1, 1) })); // 2x2 Kare
		_shapeDatabase.Add(new BlockShape(7, 60, new List<Vector2I> { new Vector2I(-1, 0), new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0,1), new Vector2I(1,1), new Vector2I(-1,1) })); // 3x2 dikdortgen
		
		// ORTA ŞEKİLLER (Ağırlık: 20-30)
		_shapeDatabase.Add(new BlockShape(4, 35, new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1) })); // 2x1 Dikdortgen
		_shapeDatabase.Add(new BlockShape(8, 30, new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(1,0), new Vector2I(-1,0) })); // T
		_shapeDatabase.Add(new BlockShape(5, 25, new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0), new Vector2I(0, -1), new Vector2I(0, -2) })); // Uzun L
		_shapeDatabase.Add(new BlockShape(1, 25, new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0), new Vector2I(0, -1) })); // Kisa L
		
		// PİS ŞEKİLLER (Ağırlık: 5-10)
		
		// O.Ç ŞEKİL (Ağırlık: 5)
		_shapeDatabase.Add(new BlockShape(9, 5,new List<Vector2I> { new Vector2I(1, -1), new Vector2I(0, 0), new Vector2I(-1, 1) })); // o.ç 
	}
	
	// --- ÜRETİM SİSTEMİ ---
	private void SpawnInitialBlocks()
	{
		for (int i = 0; i < 3; i++) 
			SpawnSingleBlock(i);
	}

	public void SyncVisuals()
	{
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 8; y++)
			{
				// Panel'in içindeki tasarımı al
				StyleBoxFlat style = (StyleBoxFlat)_visualGrid[x, y].GetThemeStylebox("panel");
             
				// Sadece arka plan rengini değiştir, kenarlıklar ve oval köşeler sabit kalsın
				style.BgColor = _grid[x, y] switch
				{
					0 => new Color(0.2f, 0.2f, 0.2f),       // Boş tahta rengi (Koyu Gri)
					1 => new Color(1f, 0.839f, 0.647f),     // Acik turuncu
					2 => new Color(0.91f, 0.612f, 0.506f),  // Acik kirmizi
					3 => new Color(0.82f, 0.384f, 0.361f),  // Kirmizi
					4 => new Color(0.918f, 0.769f, 0.835f), // Acik pembe
					5 => new Color(0.839f, 0.918f, 0.875f), // Acik yesil
					6 => new Color(0.722f, 0.878f, 0.831f), // Su yesili
					7 => new Color(0.584f, 0.722f, 0.82f),  // Soluk mavi
					8 => new Color(0.502f, 0.608f, 0.808f), // Mavi
					9 => new Color(0.243f, 0.71f, 0.616f),  // BMW yesili
					10 => new Color(0.243f, 0.71f, 0.616f), // BMW yesili
					_ => new Color(0.2f, 0.2f, 0.2f)        // Güvenlik: Bilinmeyen ID gelirse boş kalsın
				};
			}
		}
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
		GD.Print($"Hedef indeks hesaplandi: X:{targetIndex.X}, Y:{targetIndex.Y}");
		
		// Blok hedefe sigiyormu ?
		if (CanPlaceBlock(block.ShapeData, targetIndex.X, targetIndex.Y))
		{
			PlaceBlock(block.ShapeData, targetIndex.X, targetIndex.Y);
			_activeBlocks.Remove(block);
			
			CheckAndClearlines();
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
		
		BlockShape templateShape = GetDynamicRandomShape();
		
		// Şeklin orijinalini bozmamak için koordinat listesinin bir kopyasını alıyoruz!
		List<Vector2I> clonedCoords = new List<Vector2I>(templateShape.LocalCoordinates);
	   
		newBlock.ShapeData = new BlockShape(templateShape.BlockId,templateShape.Weight, clonedCoords);
		newBlock.SlotIndex = slotIndex;
		
		// Blok doğar doğmaz boyutunu %50 küçültüyoruz ki ekrana sığsın
		newBlock.Scale = new Vector2(0.5f, 0.5f);
		
		//newBlock.Position = new Vector2(_slotXPositions[slotIndex], SlotYPosition);
		newBlock.Position = GetCenteredSlotPosition(newBlock.ShapeData, slotIndex);
		
		newBlock.OnBlockDropped += HandleBlockDropped;
		newBlock.OnBlockRotated += HandleBlockRotated;
		
		AddChild(newBlock);
		_activeBlocks.Add(newBlock);
	}

	private void AddScore(int points, string reason)
	{
		_score += points;
		GD.Print($"+{points} PUAN ({reason}) ---> TOPLAM PUAN: {_score}");

		if (_scoreLabel != null)
		{
			_scoreLabel.Text = $"PUAN: {_score}";
		}

		if (_score > _highScore)
		{
			_highScore = _score;
			if (_highScoreLabel != null)
			{
				_highScoreLabel.Text = $"REKOR: {_highScore}";
			}
			SaveHighScore();
		}
	}

	private void SetupScoreUI()
	{
		// Puan
		_scoreLabel = new Label();
		_scoreLabel.Position = new Vector2(GridStartX, GridStartY - 100);
		_scoreLabel.Text = "PUAN: 0";
		_scoreLabel.AddThemeFontSizeOverride("font_size", 40);
		_scoreLabel.AddThemeColorOverride("font_color",new Color(0.9f, 0.9f, 0.8f));
		AddChild(_scoreLabel);
		
		// Rekor
		_highScoreLabel = new Label();
		_highScoreLabel.Position = new Vector2(GridStartX, GridStartY - 50);
		_highScoreLabel.Text = $"REKOR: {_highScore}";
		_highScoreLabel.AddThemeFontSizeOverride("font_size", 28);
		_highScoreLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.7f, 0.2f));
		AddChild(_highScoreLabel);
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
		float fullnes = GetBoardFullnessRatio();
		int totalDynamicWeight = 0;

		Dictionary<BlockShape, int> dynamicWeights = new Dictionary<BlockShape, int>();

		foreach (BlockShape shape in _shapeDatabase)
		{
			int currentWeight = shape.Weight;

			if (fullnes < 0.35f)
			{
				if (currentWeight >= 50)
				{
					currentWeight *= 2;
				}
			}
			else if (fullnes > 0.65f)
			{
				// Tahta dolu. Oyuncuyu kurtar.
				if (currentWeight >= 50) 
				{
					currentWeight = 1;
				}

				if (currentWeight <= 15) 
				{
					currentWeight *= 4;
				}
			}

			dynamicWeights[shape] = currentWeight;
			totalDynamicWeight += currentWeight;
		}
		
		// Yeni manipule edilmis agiliklara gore duzenleme
		Random random = new Random();
		int randomValue = random.Next(0, totalDynamicWeight);

		int cumulativeWeight = 0;
		foreach (var kvp in dynamicWeights)
		{
			cumulativeWeight += kvp.Value;
			if (randomValue < cumulativeWeight)
			{
				return kvp.Key;
			}
		}

		return _shapeDatabase[0];

		// Tum sekillerin agirliklari toplami
		// int totalWeight = 0;
		// foreach (BlockShape shape in _shapeDatabase)
		// {
		// 	totalWeight += shape.Weight;
		// }
		//
		// // 0 ile toplam agirlik arasinda rastgele bir zar atilir
		// Random random = new Random();
		// int randomValue = random.Next(0, totalWeight);
		//
		// // 3. Pastadan dilimleri çıkararak hangi şekle denk geldiğimizi bul
		// int currentWeight = 0;
		// foreach (BlockShape shape in _shapeDatabase)
		// {
		// 	currentWeight += shape.Weight;
		// 	if (randomValue < currentWeight)
		// 	{
		// 		return shape; // Zar bu şeklin dilimine denk geldi!
		// 	}
		// }
		//
		// // Teorik olarak buraya hiç düşmemesi lazım ama kod hata vermesin diye ilk şekli dönüyoruz
		// return _shapeDatabase[0];
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
	
	// private void TestBlockPlacements()
	// {
	// 	// 1. Klasik L Şekli (BlockId = 1)
	// 	List<Vector2I> lShapeCoords = new List<Vector2I>()
	// 	{
	// 		new Vector2I(0, 0),
	// 		new Vector2I(1, 0),
	// 		new Vector2I(0, -1)
	// 	};
	// 	BlockShape lShape = new BlockShape(1, lShapeCoords);
	// 	
	// 	// 2. Yatay 3'lü Çubuk (BlockId = 2)
	// 	List<Vector2I> lineCoords = new List<Vector2I>()
	// 	{
	// 		new Vector2I(-1, 0),
	// 		new Vector2I(0, 0),
	// 		new Vector2I(1, 0)
	// 	};
	// 	BlockShape horizontalLine = new BlockShape(2, lineCoords);
	//
	// 	GD.Print("--- 1. L Şekli Testi ---");
	// 	// Tahtanın ortasına yerleştir
	// 	PlaceBlock(lShape, 4, 4);
	// 	
	// 	GD.Print("--- 2. Çubuk Testi ---");
	// 	// Çizgiyi tahtanın sağ altına (güvenli bölgeye) yerleştir
	// 	// Koordinatları 6, 7 olarak ayarladık
	// 	PlaceBlock(horizontalLine, 6, 7);
	// 	
	// 	// Renkleri senkronize et
	// 	SyncVisuals();
	// }
	
	
}
