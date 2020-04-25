using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameMain : MonoBehaviour
{
    public float acceleration;
    public float baseVelocity;

    private List<(RectTransform tf, Cell cell)> currentDropDown;
    private float velocity;
    private float realPos;

    public Transform cellsRoot;

    public GameObject cellTemplate;

    private Cell[,] cells;

    private const float Width = 96;
    private const float Height = 96;
    private const float OffsetX = 336;
    private const float OffsetY = 720;

    private List<(int type, Color color)> settings = new List<(int type, Color color)>
    {
        (1, Color.red),
        (2, Color.green),
        (3, Color.blue),
    };

    private Vector2 CellToLocal(int x, int y)
    {
        return new Vector2(Width * x - OffsetX, Height * y - OffsetY);
    }

    private Vector2 CellToLocal(Vector2Int pos)
    {
        return CellToLocal(pos.x, pos.y);
    }

    private Vector2Int LocalToCell(Vector2 pos)
    {
        return new Vector2Int(Mathf.FloorToInt((pos.x + OffsetX) / Width), Mathf.FloorToInt((pos.y + OffsetY) / Height));
    }

    private int Lowest(int columnNum)
    {
        for (int i = 0; i < cells.GetLength(0); i++)
        {
            if (cells[i, columnNum] != null)
            {
                return i;
            }
        }

        return cells.GetLength(0);
    }

    private int LockDownHeight()
    {
        var height = 0;
        foreach (var (tf, cell) in currentDropDown)
        {
            var cellPos = LocalToCell(tf.anchoredPosition);

            for (int i = cells.GetLength(1); i >0; i--)
            {
                if (cells[cellPos.x, i-1] != null)
                {
                    height = height > i ? height : i;
                }
            }
        }

        return height;
    }

    private RectTransform CreateCell(Cell cell, Vector2Int pos)
    {
        var cellObject = Instantiate(cellTemplate, cellsRoot);
        cellObject.transform.localPosition = CellToLocal(pos);
        cellObject.transform.GetComponent<Image>().color = cell.color;

        return cellObject.GetComponent<RectTransform>();
    }

    private void DropCell(Cell cell1, Cell cell2)
    {
        var tf1 = CreateCell(cell1, new Vector2Int(3, 16));
        var tf2 = CreateCell(cell2, new Vector2Int(3, 17));

        currentDropDown.Add((tf1, cell1));
        currentDropDown.Add((tf2, cell2));

        velocity = baseVelocity;
        realPos = 1392;
    }

    private void Start()
    {
        cells = new Cell[8, 16];
        currentDropDown = new List<(RectTransform, Cell)>();

        DropCell(new Cell(settings[Random.Range(0, settings.Count)]), new Cell(settings[Random.Range(0, settings.Count)]));
    }

    private void DropDownCell()
    {
        foreach (var (tf, cell) in currentDropDown.OrderBy(x => x.tf.anchoredPosition.y))
        {
            var cellPos = LocalToCell(tf.anchoredPosition);
            var index = cells.GetLength(1);
            for (; index > 0; index--)
            {
                if (cells[cellPos.x, index - 1] != null)
                {
                    break;
                }
            }

            cells[cellPos.x, index] = cell;
            tf.anchoredPosition = CellToLocal(cellPos.x, index);
        }

        currentDropDown.Clear();

        DropCell(new Cell(settings[Random.Range(0, settings.Count)]), new Cell(settings[Random.Range(0, settings.Count)]));
    }

    private void CellDrop()
    {
        realPos -= velocity;
        velocity += acceleration * Time.deltaTime;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            var delta = 96 * (Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 1);

            foreach (var (tf, _) in currentDropDown)
            {
                tf.anchoredPosition += new Vector2(delta, 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            DropDownCell();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            var tf0 = currentDropDown[0].tf;
            var tf1 = currentDropDown[1].tf;

            tf1.RotateAround(tf0.position, Vector3.forward, -90);
        }
    }
}