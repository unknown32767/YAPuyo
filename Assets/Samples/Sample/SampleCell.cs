using UnityEngine;
using UnityEngine.UI;

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
        var instance = GameObject.Instantiate(cellTemplate);
        var image = instance.GetComponent<Image>();
        image.color = color;
        return instance.GetComponent<RectTransform>();
    }
}