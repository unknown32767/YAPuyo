using System.Collections.Generic;
using UnityEngine;

public class SampleCell : ICell
{
    public int type;
    public Color color;

    public static GameObject cellTemplate;

    public bool IsSameType(ICell other)
    {
        return ((SampleCell) other).type == type;
    }

    public RectTransform CreateInstance()
    {
        return GameObject.Instantiate(cellTemplate).GetComponent<RectTransform>();
    }

    public void OnMatch(Board<ICell> board, List<Vector2Int> matched, int combo = 0)
    {
    }
}
