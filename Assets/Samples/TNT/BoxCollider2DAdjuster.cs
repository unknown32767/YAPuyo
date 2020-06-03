using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BoxCollider2DAdjuster : MonoBehaviour
{
    private BoxCollider2D boxCollider2D;
    private RectTransform rectTransform;

    private void Start()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        var rect = rectTransform.rect;
        boxCollider2D.offset = rect.center;
        boxCollider2D.size = rect.size;
    }
}