using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class TNTCellBlock : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler
{
    public Rigidbody2D blockRigidbody;

    [NonSerialized] public List<TNTCellObject> cells;

    private bool selected;
    private Vector3 originPos;

    public void Init(List<TNTCell> cellList, List<Vector2> offsetList)
    {
        cells = new List<TNTCellObject>();

        for (var i = 0; i < cellList.Count; i++)
        {
            var cell = cellList[i];
            var instance = cellList[i].CreateInstance();
            instance.SetParent(transform, false);
            instance.anchoredPosition = Vector2.Scale(TNTCell.size, offsetList[i]);
            var cellObject = instance.GetComponent<TNTCellObject>();
            cellObject.cell = cell;

            cells.Add(cellObject);
        }

        selected = false;
    }

    public void Init(List<TNTCellObject> cellObjects)
    {
        cells = new List<TNTCellObject>(cellObjects);

        var pos = cells.Aggregate(Vector3.zero, (sum, cell) => sum + cell.transform.position) / cells.Count;
        transform.position = pos;

        foreach (var cell in cells)
        {
            cell.transform.SetParent(transform, true);
        }
    }

    private void Rotate()
    {
        transform.Rotate(0,0,-45);
    }

    public void RemoveCell(TNTCellObject cellObject)
    {
        cells.Remove(cellObject);
        Destroy(cellObject.gameObject);

        if (cells.Count == 0)
        {
            TNTGameMain.instance.RemoveBlock(this);
        }
    }

    public void UpdateBlock()
    {
        var visited = new HashSet<TNTCellObject>();
        var connects = new List<List<TNTCellObject>>();

        while (cells.Exists(cell => !visited.Contains(cell)))
        {
            var head = cells.First(cell => !visited.Contains(cell));
            var queue = new Queue<TNTCellObject>();
            queue.Enqueue(head);

            var connected = new List<TNTCellObject>();

            while (queue.Count != 0)
            {
                var next = queue.Dequeue();

                connected.Add(next);
                visited.Add(next);

                foreach (var cell in cells)
                {
                    if (!visited.Contains(cell) &&
                        Mathf.Abs(Vector3.Distance(next.transform.position, cell.transform.position) - 32) < 0.01f)
                    {
                        queue.Enqueue(cell);
                    }
                }
            }

            connects.Add(connected);
        }

        if (connects.Count > 1)
        {
            foreach (var connected in connects)
            {
                TNTGameMain.instance.CreateBlocK(connected);
            }
            TNTGameMain.instance.RemoveBlock(this);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left && TNTGameMain.instance.intractable)
        {
            selected = true;
            originPos = transform.localPosition;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && selected)
        {
            selected = false;
            blockRigidbody.constraints = RigidbodyConstraints2D.None;
            blockRigidbody.velocity = Vector2.down * 10.0f;
            TNTGameMain.instance.DropBlock(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (selected)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent as RectTransform, eventData.position,
                eventData.pressEventCamera, out var localPos);
            transform.localPosition = localPos;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selected)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Rotate();
            }
        }
    }
}