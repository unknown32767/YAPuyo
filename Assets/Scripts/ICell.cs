using System.Collections.Generic;
using UnityEngine;

public interface ICell
{
    bool IsSameType(ICell other);

    RectTransform CreateInstance();

    void OnMatch(Board<ICell> board, List<Vector2Int> matched, int combo = 0);
}