using System.Collections.Generic;
using UnityEngine;

public interface ICell<T>
{
    bool IsSameType(T other);

    RectTransform CreateInstance();
}