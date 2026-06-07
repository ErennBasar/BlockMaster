using System.Collections.Generic;
using Godot;

public record ColorPalette(
    Color ComboColor, 
    Color MediumColor, 
    Color SmallColor, 
    Color NastyColor,
    Color BgColor,
    Color EmptyGridColor
    );

public enum ShapeCategory
{
    ComboMaker,
    Medium,
    Small,
    Nasty
}

public class BlockShape
{
    //BlockId artik islevsiz bir ara silinmesi lazim
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