using System.Collections.Generic;
using Godot;

public class BlockShape
{
    public int BlockId { get; private set; } // Sekil tipi
    public int Weight { get; set; }
    public List<Vector2I> LocalCoordinates { get; private set; }

    
    public BlockShape(int id, int weight, List<Vector2I> coordinates )
    {
        BlockId = id;
        Weight = weight;
        LocalCoordinates = coordinates;
    }
}