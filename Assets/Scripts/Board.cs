using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Object;

public class Board<T> where T : class, ICell
{
    //棋盘相关
    public int width;
    public int height;

    public Vector2Int cellSize;
    public Vector2Int gap;

    public T[,] cells { get; }
    public RectTransform[,] cellTransforms { get; }

    private Vector2Int totalSize => cellSize + gap;

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
            LeanTween.move(rectTransform, toPos, 0.5f);
            return 0.5f;
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
            LeanTween.scale(rectTransform, Vector3.zero, 0.5f).setOnComplete(() =>
            {
                Destroy(rectTransform);
            });
            return 0.5f;
        };

        neighbourOffsets = vonNeumannNeighbour;
    }

    public Vector2 CellToLocal(Vector2Int pos)
    {
        return Vector2.Scale(totalSize, pos);
    }

    public Vector2 CellToLocal(int x, int y)
    {
        return new Vector2(x * totalSize.x, y * totalSize.y);
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
        Destroy(cellTransforms[to.x, to.y]);
        cellTransforms[to.x, to.y] = fromTransform;
        cellTransforms[from.x, from.y] = null;
        cells[to.x, to.y] = cells[from.x, from.y];
        cells[from.x, from.y] = null;

        return moveAnim.Invoke(fromTransform, to);
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
                    var animTime = fillAnim(rectTransform, CellToLocal(i, j));
                    maxAnimTime = Mathf.Max(maxAnimTime, animTime);
                }
            }
        }

        return maxAnimTime;
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
        if (cellChecked[pos.x, pos.y] || (pos.x == -1 && pos.y==-1))
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
                    if (!cellChecked[nextPos.x, nextPos.y])
                        queue.Enqueue(nextPos);
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

            if (start.x == -1 && start.y == -1)
            {
                break;
            }

            var connected = Bfs(start, ref cellChecked, extraConnectRequirement);

            if (connected.Count > threshold)
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