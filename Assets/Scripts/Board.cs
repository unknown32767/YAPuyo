using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Object;

public class Board<T> where T : class, ICell
{
    //棋盘相关
    public int width;
    public int height;

    public Vector2 cellSize;
    public Vector2 gap;

    public T[,] cells { get; }
    public RectTransform[,] cellTransforms { get; }

    public Transform cellRoot;

    private Vector2 totalSize => cellSize + gap;

    //生成相关
    public ICellPool<T> cellPool;

    //动画相关
    public Func<RectTransform, Vector2, float> moveAnim;
    public Func<RectTransform, Vector2, float> fillAnim;
    public Func<RectTransform, float> matchAnim;

    //逻辑相关
    private List<Vector2Int> neighbourOffsets;

    private readonly List<Vector2Int> vonNeumannNeighbour = new List<Vector2Int>
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    private readonly List<Vector2Int> mooreNeighbour = new List<Vector2Int>
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
    };

    public Board(int width, int height)
    {
        this.width = width;
        this.height = height;

        cells = new T[width, height];
        cellTransforms = new RectTransform[width, height];

        moveAnim = (rectTransform, toPos) =>
        {
            var time = (rectTransform.anchoredPosition - toPos).magnitude / (totalSize.y * 4);
            LeanTween.move(rectTransform, toPos, time).setEase(LeanTweenType.linear);
            return time;
        };

        fillAnim = (rectTransform, pos) =>
        {
            rectTransform.anchoredPosition = pos;
            rectTransform.localScale = Vector3.zero;
            LeanTween.scale(rectTransform, Vector3.one, 0.5f);
            return 0.5f;
        };

        matchAnim = (rectTransform) =>
        {
            LeanTween.scale(rectTransform, Vector3.zero, 0.5f).setOnComplete(() => { Destroy(rectTransform.gameObject); });
            return 0.5f;
        };

        neighbourOffsets = vonNeumannNeighbour;
    }

    public void Clear()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                cells[i, j] = null;

                if (cellTransforms[i, j] != null)
                {
                    Destroy(cellTransforms[i, j].gameObject);
                }

                cellTransforms[i, j] = null;
            }
        }
    }

    public bool InBound(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public Vector2 CellToLocal(Vector2Int pos)
    {
        return Vector2.Scale(totalSize, pos) + cellSize * 0.5f;
    }

    public Vector2 CellToLocal(int x, int y)
    {
        return new Vector2(x * totalSize.x, y * totalSize.y) + cellSize * 0.5f;
    }

    public Vector2Int LocalToCell(Vector2 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / totalSize.x), Mathf.FloorToInt(pos.y / totalSize.y));
    }

    public void SetVonNeumannNeighbour()
    {
        neighbourOffsets = vonNeumannNeighbour;
    }

    public void SetMooreNeighbour()
    {
        neighbourOffsets = mooreNeighbour;
    }

    public float MoveCell(Vector2Int from, Vector2Int to)
    {
        var fromTransform = cellTransforms[from.x, from.y];
        var toTransform = cellTransforms[to.x, to.y];

        if (toTransform != null)
        {
            Destroy(toTransform.gameObject);
        }

        cellTransforms[to.x, to.y] = fromTransform;
        cellTransforms[from.x, from.y] = null;

        cells[to.x, to.y] = cells[from.x, from.y];
        cells[from.x, from.y] = null;

        return moveAnim(fromTransform, CellToLocal(to));
    }

    public float SwapCell(Vector2Int a, Vector2Int b)
    {
        var timeA = moveAnim(cellTransforms[a.x, a.y], CellToLocal(b));
        var timeB = moveAnim(cellTransforms[b.x, b.y], CellToLocal(a));

        (cells[a.x, a.y], cells[b.x, b.y]) = (cells[b.x, b.y], cells[a.x, a.y]);
        (cellTransforms[a.x, a.y], cellTransforms[b.x, b.y]) = (cellTransforms[b.x, b.y], cellTransforms[a.x, a.y]);

        return Mathf.Max(timeA, timeB);
    }

    public float RemoveCell(Vector2Int index)
    {
        var willBeRemoved = cellTransforms[index.x, index.y];

        cells[index.x, index.y] = null;
        cellTransforms[index.x, index.y] = null;

        return matchAnim(willBeRemoved);
    }

    public float RemoveCells(List<Vector2Int> indices)
    {
        var maxAnimTime = 0.0f;

        foreach (var index in indices)
        {
            var animTime = RemoveCell(index);
            maxAnimTime = Mathf.Max(maxAnimTime, animTime);
        }

        return maxAnimTime;
    }

    public float RemoveCells(List<List<Vector2Int>> indicess)
    {
        var maxAnimTime = 0.0f;

        foreach (var indices in indicess)
        {
            foreach (var index in indices)
            {
                var animTime = RemoveCell(index);
                maxAnimTime = Mathf.Max(maxAnimTime, animTime);
            }
        }

        return maxAnimTime;
    }

    public float FillCellInPlace()
    {
        var maxAnimTime = 0.0f;

        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (cells[i, j] == null)
                {
                    var newCell = cellPool.Take();
                    var rectTransform = newCell.CreateInstance();
                    rectTransform.SetParent(cellRoot);
                    var animTime = fillAnim(rectTransform, CellToLocal(i, j));

                    cells[i, j] = newCell;
                    cellTransforms[i, j] = rectTransform;

                    maxAnimTime = Mathf.Max(maxAnimTime, animTime);
                }
            }
        }

        return maxAnimTime;
    }

    public float FillCellDropDown(bool unifyDropHeight = true)
    {
        var maxAnimTime = 0.0f;

        var maxDropHeight = 0;

        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (cells[i, j] == null)
                {
                    maxDropHeight = Mathf.Max(maxDropHeight, height - j);
                    break;
                }
            }
        }

        for (var i = 0; i < width; i++)
        {
            var startHeight = height;
            for (var j = 0; j < height; j++)
            {
                if (cells[i, j] == null)
                {
                    var newCell = cellPool.Take();
                    var rectTransform = newCell.CreateInstance();
                    var dropHeight = unifyDropHeight ? maxDropHeight + j : startHeight;

                    rectTransform.SetParent(cellRoot);
                    rectTransform.anchoredPosition = CellToLocal(i, dropHeight);
                    startHeight++;

                    var animTime = moveAnim(rectTransform, CellToLocal(i, j));

                    cells[i, j] = newCell;
                    cellTransforms[i, j] = rectTransform;

                    maxAnimTime = Mathf.Max(maxAnimTime, animTime);
                }
            }
        }

        return maxAnimTime;
    }

    public float Collapse()
    {
        var maxAnimTime = 0.0f;

        for (int i = 0; i < width; i++)
        {
            var j = 0;
            var k = 0;
            while (j < height && k < height)
            {
                if (cells[i, j] != null)
                    j++;
                else
                {
                    if (k < j)
                        k = j;
                    while (k < height && cells[i, k] == null)
                    {
                        k++;
                    }

                    if (k < height)
                    {
                        var animTime = MoveCell(new Vector2Int(i, k), new Vector2Int(i, j));
                        maxAnimTime = Mathf.Max(maxAnimTime, animTime);
                        j++;
                    }
                }
            }
        }

        return maxAnimTime;
    }

    public void DropDownCells(List<(T cell, RectTransform tf)> dropdownCells)
    {
        foreach (var (cell, tf) in dropdownCells.OrderBy(x => x.tf.anchoredPosition.y))
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

            if (index == cells.GetLength(1))
            {
                continue;
            }

            cells[cellPos.x, index] = cell;
            cellTransforms[cellPos.x, index] = tf;
            tf.anchoredPosition = CellToLocal(cellPos.x, index);
        }
    }

    private Vector2Int FirstUnchecked(bool[,] cellChecked)
    {
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (!cellChecked[i, j])
                {
                    if (cells[i, j] != null)
                        return new Vector2Int(i, j);

                    cellChecked[i, j] = true;
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    private bool AllChecked(bool[,] cellChecked)
    {
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (!cellChecked[i, j] && cells[i, j] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private List<Vector2Int> Bfs(Vector2Int pos, ref bool[,] cellChecked, Func<Vector2Int, bool> extraConnectRequirement = null)
    {
        if (!InBound(pos) || cellChecked[pos.x, pos.y])
        {
            return null;
        }

        var startCell = cells[pos.x, pos.y];

        var queue = new Queue<Vector2Int>();
        var connected = new List<Vector2Int>();

        queue.Enqueue(pos);

        while (queue.Count != 0)
        {
            var currentPos = queue.Dequeue();

            if (startCell.IsSameType(cells[currentPos.x, currentPos.y]) &&
                (extraConnectRequirement == null || extraConnectRequirement(currentPos)))
            {
                connected.Add(currentPos);
                cellChecked[currentPos.x, currentPos.y] = true;

                foreach (var offset in neighbourOffsets)
                {
                    var nextPos = currentPos + offset;

                    if (!InBound(nextPos) || cellChecked[nextPos.x, nextPos.y])
                    {
                        continue;
                    }

                    if (cells[nextPos.x, nextPos.y] == null)
                    {
                        cellChecked[nextPos.x, nextPos.y] = true;
                    }
                    else if (!queue.Contains(nextPos))
                    {
                        queue.Enqueue(nextPos);
                    }
                }
            }
        }

        return connected;
    }

    public List<List<Vector2Int>> FindAllConnected(int threshold = 0, Func<Vector2Int, bool> extraConnectRequirement = null)
    {
        var result = new List<List<Vector2Int>>();
        var cellChecked = new bool[width, height];

        while (true)
        {
            var start = FirstUnchecked(cellChecked);

            if (!InBound(start))
            {
                break;
            }

            var connected = Bfs(start, ref cellChecked, extraConnectRequirement);

            if (connected.Count >= threshold)
            {
                result.Add(connected);
            }
        }

        return result;
    }

    public List<Vector2Int> FindConnected(Vector2Int pos, Func<Vector2Int, bool> extraConnectRequirement = null)
    {
        var cellChecked = new bool[width, height];

        return Bfs(pos, ref cellChecked, extraConnectRequirement);
    }
}