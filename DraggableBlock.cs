using Godot;
using System;

public partial class DraggableBlock : Area2D
{

	private bool _isDragging = false;
	public int SlotIndex { get; set; }
	
	public Color ActiveThemeColor { get; set; }
	
	// Farenin blogun neresinden tuttugunu hesaplamak icin
	private Vector2 _dragOffset;

	//Tıklamanın başladığı ilk piksel
	private Vector2 _touchStartPos;
	
	// Blok birakildiginda BlockMaster'a haber verecek
	public Action<DraggableBlock, Vector2> OnBlockDropped;
	public Action<DraggableBlock, Vector2> OnBlockDragging;

	public Action<DraggableBlock> OnBlockRotated;

	private const int CellSize = 115;
	private const int CellPadding = 5;
	private const int Step = CellSize + CellPadding;
	
	// Alandaki fiziksel karede hangi mantiksal seklin tutuldugu
	// Akilli property, Veri atandigi an DrawShapeVisuals metodunu tetikler
	private BlockShape _shapeData;

	public BlockShape ShapeData
	{
		get => _shapeData;
		set
		{
			_shapeData = value;
			DrawShapeVisuals(); // Sekli erkana cizer
		}
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Şekil verisi yüklendiğinde görseli oluştur
		if (ShapeData != null) DrawShapeVisuals();
		
		// Önemli: Input algılaması için Area2D ayarı
		InputPickable = true;
	}
	
	private void DrawShapeVisuals()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

	    if (_shapeData == null) return;
	    
	    float gap = 6f; 
	    float actualSize = CellSize - gap;

	    foreach (Vector2I offset in _shapeData.LocalCoordinates)
	    {
	        float posX = (offset.X * Step) - (CellSize / 2f);
	        float posY = (offset.Y * Step) - (CellSize / 2f);
	        
	        BevelCell cell    = new BevelCell();
	        cell.MainColor    = ActiveThemeColor;
	        cell.IsEmpty     = false;
	        cell.Size         = new Vector2(actualSize, actualSize);
	        cell.Position     = new Vector2(posX + (gap / 2f), posY + (gap / 2f));
	        cell.MouseFilter  = Control.MouseFilterEnum.Ignore;
	        AddChild(cell);

	        // Çarpışma alanı (değişmedi)
	        CollisionShape2D col   = new CollisionShape2D();
	        RectangleShape2D shape = new RectangleShape2D();
	        shape.Size             = new Vector2(CellSize, CellSize);
	        col.Shape              = shape;
	        col.Position           = new Vector2(posX + CellSize / 2f, posY + CellSize / 2f);
	        AddChild(col);
	    }
		// // Çizim yapmadan önce eski kutuları ve fiziksel zırhları temizle
		// foreach (Node child in GetChildren())
		// {
		// 	child.QueueFree();
		// }
		//
		// foreach (Vector2I offset in _shapeData.LocalCoordinates)
		// {
		// 	Panel blockPanel = new Panel();
		// 	blockPanel.Size = new Vector2(CellSize, CellSize);
		//   
		// 	StyleBoxFlat style = new StyleBoxFlat();
		// 	
		// 	style.BgColor = ActiveThemeColor;
		// 	
		// 	style.BorderWidthTop = 4;
		// 	style.BorderWidthBottom = 4;
		// 	style.BorderWidthLeft = 4;
		// 	style.BorderWidthRight = 4;
		// 	style.BorderColor = new Color(0f, 0f, 0f, 0.4f); // Yarı saydam siyah kenarlık
		//   
		// 	style.CornerRadiusTopLeft = 12;
		// 	style.CornerRadiusTopRight = 12;
		// 	style.CornerRadiusBottomLeft = 12;
		// 	style.CornerRadiusBottomRight = 12;
		// 	
		// 	blockPanel.AddThemeStyleboxOverride("panel", style);
		// 	
		// 	// KOORDİNAT HESABI: 
		// 	// offset değerini adım ile çarpıyoruz. 
		// 	// - (CellSize / 2f) yapmamızın sebebi: Godot'da kutunun konumu sol üst köşesidir.
		// 	// Fare tam ortadan tutsun diye kutuyu yarım boy kadar geriye çekiyoruz.
		// 	float posX = (offset.X * Step) - (CellSize / 2f);
		// 	float posY = (offset.Y * Step) - (CellSize / 2f);
		//
		// 	blockPanel.Position = new Vector2(posX, posY);
		// 	blockPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		// 	AddChild(blockPanel);
		//
		// 	// FİZİKSEL TIKLAMA ALANI 
		// 	CollisionShape2D collisionBox = new CollisionShape2D();
		// 	RectangleShape2D physicsShape = new RectangleShape2D();
		// 	physicsShape.Size = new Vector2(CellSize, CellSize);
		// 	collisionBox.Shape = physicsShape;
		// 	collisionBox.Position = blockPanel.Position + new Vector2(CellSize / 2f, CellSize / 2f);
		// 	AddChild(collisionBox);
		// }
	}
	
	// Area2D'nin icinde gomulu olan fiziksel tiklama algilayici
	public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
	{
		GD.Print("Bir input algılandı!"); // Tıkladığında bu yazı gelmiyorsa sorun fiziksel ayarlardadır.
		
		// Gelen input fare tiklamasi ve sol tik ise
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (mouseEvent.Pressed)
			{
				_isDragging = true;

				Scale = new Vector2(1.0f, 1.0f);
			 
				// Farenin/Parmağın ekrana ilk dokunduğu global koordinatı kaydet
				_touchStartPos = mouseEvent.GlobalPosition;
			 
				// Fare blogun neresine tikladiysa orayi offset olarak alsin diye
				_dragOffset = GlobalPosition - mouseEvent.GlobalPosition;

				// Blogu havaya kaldirma hissiyati, Z ekseninde en one aliyoruz
				ZIndex = 100;
			}
			// else
			// {
			// 	if (!_isDragging)
			// 	{
			// 		return;
			// 	}
			// 	// Tiklama bittiginde
			// 	_isDragging = false;
			// 	ZIndex = 0; // Eski katmanina geri doner
			//  
			// 	// Parmağın basıldığı yer ile çekildiği yer arasındaki piksel mesafesini ölçüyoruz
			// 	float dragDistance = mouseEvent.GlobalPosition.DistanceTo(_touchStartPos);
			//
			// 	// Blok dondurmeyi iptal ettik. Daha sonra puan karsiligi aktif olacak bir ozellik olacak
			// 	
			// 	// Eğer oyuncu bloğu 15 pikselden daha az hareket ettirdiyse bu bir SÜRÜKLEME değil, TIKLAMADIR!
			// 	// if (dragDistance < 15f)
			// 	// {
			// 	// 	GD.Print("Blok üzerinde kısa tık algılandı! Döndürülüyor...");
			// 	// 	Rotate90Degrees();
			// 	// 	
			// 	// 	// BlockMaster'a "Ben döndüm, merkezim kaydı, beni düzelt!" diye haber veriyoruz.
			// 	// 	OnBlockRotated?.Invoke(this);
			// 	// 	
			// 	// 	return; // Metottan erken çıkıyoruz ki tahtaya bırakma (Drop) kodu tetiklenmesin!
			// 	// }
			//
			// 	// Eğer 15 pikselden fazla hareket ettirdiyse bu normal sürüklemedir, tahtaya yerleştirmeyi dene:
			// 	OnBlockDropped?.Invoke(this, GlobalPosition);
			// }
		}
	}
	
	// Release event
	public override void _Input(InputEvent @event)
	{
		// Eğer blok zaten sürükleniyorsa ve ekrandan parmak çekildiyse (Pressed == false)
		if (_isDragging && @event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
		{
			_isDragging = false;
			ZIndex = 0;
        
			// Bloğu tahtaya bırakmayı dene
			OnBlockDropped?.Invoke(this, GlobalPosition);
		}
	}
	
	// public override void _UnhandledInput(InputEvent @event)
	// {
	// 	if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
	// 	{
	// 		GD.Print("Ekranın bir yerine tıklandı: " + mouseEvent.GlobalPosition);
	// 	}
	// }
	
	// Process metodunu sadece sürükleme esnasında pozisyon güncellemek için kullanacağız
	public override void _Process(double delta)
	{
		if (_isDragging)
		{
			// Bloğun pozisyonunu, farenin ekrandaki pozisyonu + ilk tıkladığımız yerin ofseti yap
			GlobalPosition = GetGlobalMousePosition() + _dragOffset;
			
			OnBlockDragging?.Invoke(this, GlobalPosition);
		}
	}

	private void Rotate90Degrees()
	{
		for (int i = 0; i < ShapeData.LocalCoordinates.Count; i++)
		{
			Vector2I oldCoord = ShapeData.LocalCoordinates[i];
			ShapeData.LocalCoordinates[i] = new Vector2I(-oldCoord.Y, oldCoord.X);
		}

		foreach (Node child in GetChildren())
		{
			if (child is ColorRect)
			{
				child.QueueFree();
			}
		}
		DrawShapeVisuals();
	}
}
