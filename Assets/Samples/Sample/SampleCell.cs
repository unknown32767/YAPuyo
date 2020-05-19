using UnityEngine;
using UnityEngine.UI;

public class SampleCell : ICell<SampleCell>
{
    public int type;
    public Color color;

    public static GameObject cellTemplate;

    public bool IsSameType(SampleCell other)
    {
        return other.type == type;
    }

    public RectTransform CreateInstance()
    {
        var instance = GameObject.Instantiate(cellTemplate);
        var image = instance.GetComponent<Image>();
        image.color = color;
        return instance.GetComponent<RectTransform>();
    }
}