using UnityEngine;

public class Cell
{
    public int type;
    public Color color;

    public Cell((int type, Color color) tuple)
    {
        type = tuple.type;
        color = tuple.color;
    }
}
