using UnityEngine;
using UnityEngine.UI;

public class TNTCell : ICell<TNTCell>
{
    public int type;
    public Color color;

    public static GameObject cellTemplate { get; private set; }
    public static Vector2 size;

    public static void SetTemplate(GameObject template)
    {
        cellTemplate = template;
        size = template.GetComponent<RectTransform>().rect.size;
    }

    public bool IsSameType(TNTCell other)
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