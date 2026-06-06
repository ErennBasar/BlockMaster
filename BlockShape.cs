using System.Collections.Generic;
using Godot;

public enum ShapeCategory
{
    ComboMaker,
    Medium,
    Small,
    Nasty
}

public class BlockShape
{
    public int BlockId { get; private set; } // Sekil rengi
    public int Weight { get; set; }
    public ShapeCategory Category { get; private set; }
    public List<Vector2I> LocalCoordinates { get; private set; }

    
    public BlockShape(int id, int weight, ShapeCategory category, List<Vector2I> coordinates )
    {
        BlockId = id;
        Weight = weight;
        Category = category;
        LocalCoordinates = coordinates;
    }
}