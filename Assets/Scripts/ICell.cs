using System.Collections.Generic;
using UnityEngine;

public interface ICell
{
    bool IsSameType(ICell other);

    RectTransform CreateInstance();
}