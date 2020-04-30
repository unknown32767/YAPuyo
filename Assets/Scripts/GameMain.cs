using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameMain : MonoBehaviour
{
    public float waitTime = 0.25f;

    private List<(RectTransform tf, Cell cell)> currentDropDown;

    public Transform cellsRoot;

    public GameObject cellTemplate;

    public Toggle shield;
    public Toggle claymore;
    public Toggle dagger;
    public Toggle leather;
    public Toggle light;
    public Toggle heavy;
    public Toggle potion;
    public Toggle shuriken;

    public Text hp;
    public Text block;
    public Text turn;
    public Text hp1;

    private Cell[,] cells;
    private GameObject[,] cellObjects;
    private CellPool cellPool;

    private const float Width = 96;
    private const float Height = 96;
    private const float OffsetX = 336;
    private const float OffsetY = 720;

    private int selfHP;
    private int selfAtk;
    private int selfDef;
    private int selfBaseBlock;
    private int selfBlock;
    private int selfAtkComboBonus;
    private int selfBlockComboBonus;

    private int enemyHP;

    private int turns;
    private int phase;

    private List<(int, Action)> enemyLoop;

    private List<Cell> settings = new List<Cell>
    {
        //攻击
        new Cell((1, Color.red)),
        //格挡
        new Cell((2, Color.blue)),
        //药剂
        new Cell((3, Color.green)),
        //投掷
        new Cell((4, Color.gray)),
    };

    private bool intractable;

    private void DealDamageToPlayer(int dmg)
    {
        dmg -= selfDef + selfBlock;
        selfBlock = 0;
        if(dmg > 0)
            selfHP -= dmg;
    }

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
    }

    public void StartGame()
    {
        var cellSet = new Dictionary<Cell, int>();
        foreach (var cell in settings)
        {
            cellSet[cell] = 0;
        }

        selfHP = 30;
        enemyHP = 100;
        selfBlock = 0;

        if (shield.isOn)
        {
            selfAtk = 7;
            selfAtkComboBonus = 2;
            selfBaseBlock = 5;
            selfBlockComboBonus = 3;

            cellSet[settings[0]] += 20;
            cellSet[settings[1]] += 20;
        }
        else if (claymore.isOn)
        {
            selfAtk = 14;
            selfAtkComboBonus = 1;
            selfBaseBlock = 3;
            selfBlockComboBonus = 2;

            cellSet[settings[0]] += 30;
            cellSet[settings[1]] += 10;
        }
        else if (dagger.isOn)
        {
            selfAtk = 5;
            selfAtkComboBonus = 3;
            selfBaseBlock = 1;
            selfBlockComboBonus = 1;

            cellSet[settings[0]] += 20;
            cellSet[settings[1]] += 20;
        }

        if (leather.isOn)
        {
            selfDef = 1;
        }
        else if (light.isOn)
        {
            selfDef = 2;
        }
        else if (heavy.isOn)
        {
            selfDef = 3;
        }

        if (potion.isOn)
        {
            cellSet[settings[2]] += 4;
        }

        if (shuriken.isOn)
        {
            cellSet[settings[3]] += 8;
        }

        cells = new Cell[8, 16];
        cellObjects = new GameObject[8, 16];
        currentDropDown = new List<(RectTransform, Cell)>();

        enemyLoop = new List<(int, Action)>
        {
            (4, () => DealDamageToPlayer(5)),
            (4, () => DealDamageToPlayer(5)),
            (10, () => DealDamageToPlayer(20)),
        };

        cellPool = new CellPool
        {
            cellCounts = cellSet,
            cellPool = new List<Cell>(),
        };

        turns = 0;
        phase = 0;

        intractable = true;
        DropCell(cellPool.Take(), cellPool.Take());
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

            if (index == cells.GetLength(1))
            {
                continue;
            }

            cells[cellPos.x, index] = cell;
            cellObjects[cellPos.x, index] = tf.gameObject;
            tf.anchoredPosition = CellToLocal(cellPos.x, index);
        }

        currentDropDown.Clear();

        DropCell(cellPool.Take(), cellPool.Take());
    }

    private void Update()
    {
        hp.text = selfHP.ToString();
        block.text = selfBlock.ToString();
        hp1.text = enemyHP.ToString();
        turn.text = turns.ToString();

        if (intractable)
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
                StartCoroutine(UpdateCells());
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                var tf0 = currentDropDown[0].tf;
                var tf1 = currentDropDown[1].tf;

                tf1.RotateAround(tf0.position, Vector3.forward, -90);
            }
        }
    }

    private bool AllChecked(bool[,] cellChecked)
    {
        foreach (var b in cellChecked)
        {
            if (!b)
                return false;
        }

        return true;
    }

    private Vector2Int FirstUnchecked(bool[,] cellChecked)
    {
        for (int i = 0; i < 8; i++)
        for (int j = 0; j < 16; j++)
        {
            if (!cellChecked[i, j])
            {
                if (cells[i, j] != null)
                    return new Vector2Int(i, j);
                else
                {
                    cellChecked[i, j] = true;
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    private void BFS(Vector2Int start, bool[,] cellChecked, List<Vector2Int> willBeRemoved)
    {
        if (start.x == -1 && start.y == -1)
        {
            return;
        }

        var cell = cells[start.x, start.y];

        if (cells == null)
        {
            return;
        }

        var type = cell.type;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> connected = new List<Vector2Int>();

        queue.Enqueue(start);

        while (queue.Count != 0)
        {
            var next = queue.Dequeue();

            if (cell.type == cells[next.x, next.y]?.type && !cellChecked[next.x, next.y])
            {
                cellChecked[next.x, next.y] = true;
                connected.Add(next);

                if (next.x > 0)
                    queue.Enqueue(new Vector2Int(next.x - 1, next.y));
                if (next.x < 8 - 1)
                    queue.Enqueue(new Vector2Int(next.x + 1, next.y));
                if (next.y > 0)
                    queue.Enqueue(new Vector2Int(next.x, next.y - 1));
                if (next.y < 16 - 1)
                    queue.Enqueue(new Vector2Int(next.x, next.y + 1));
            }
        }

        if (connected.Count >= 4)
        {
            willBeRemoved.AddRange(connected);
        }
    }

    private void RemoveCell(int x, int y)
    {
        cells[x, y] = null;
        Destroy(cellObjects[x, y]);
        cellObjects[x, y] = null;
    }

    private void RemoveConnected(int combo)
    {
        var cellChecked = new bool[8, 16];
        var willBeRemoved = new List<Vector2Int>();

        while (!AllChecked(cellChecked))
        {
            Vector2Int next = FirstUnchecked(cellChecked);
            BFS(next, cellChecked, willBeRemoved);
        }

        var cellCount = new List<int>(Enumerable.Repeat(0, settings.Count));
        foreach (var pos in willBeRemoved)
        {
            cellCount[settings.IndexOf(cells[pos.x, pos.y])]++;
        }

        if (cellCount[0] > 0)
        {
            enemyHP -= selfAtk + selfAtkComboBonus * (cellCount[0] + combo - 4);
        }
        if (cellCount[1] > 0)
        {
            selfBlock = Mathf.Max(selfBlock, selfBaseBlock + selfBlockComboBonus * (cellCount[1] + combo - 4));
        }
        if (cellCount[2] > 0)
        {
            selfHP += 10;
        }
        if (cellCount[3] > 0)
        {
            enemyHP -= 10;
        }

        foreach (var i in willBeRemoved)
        {
            RemoveCell(i.x, i.y);
        }
    }

    private void MoveCell(Vector2Int from, Vector2Int to)
    {
        var fromObject = cellObjects[from.x, from.y];
        Destroy(cellObjects[to.x, to.y]);
        cellObjects[to.x, to.y] = fromObject;
        cellObjects[from.x, from.y] = null;
        cells[to.x, to.y] = cells[from.x, from.y];
        cells[from.x, from.y] = null;

        fromObject.GetComponent<RectTransform>().anchoredPosition = CellToLocal(to.x, to.y);
    }

    private int Collapse()
    {
        int count = 0;
        for (int i = 0; i < 8; i++)
        {
            var j = 0;
            var k = 0;
            while (j < 16 && k < 16)
            {
                if (cells[i, j] != null)
                    j++;
                else
                {
                    if (k < j)
                        k = j;
                    while (k < 16 && cells[i, k] == null)
                    {
                        k++;
                    }

                    if (k < 16)
                    {
                        MoveCell(new Vector2Int(i, k), new Vector2Int(i, j));
                        count++;
                        j++;
                    }
                }
            }
        }

        return count;
    }

    private IEnumerator UpdateCells()
    {
        int newCellCount;
        int combo = 0;
        intractable = false;
        do
        {
            RemoveConnected(combo);
            newCellCount = Collapse();
            combo++;

            yield return new WaitForSeconds(waitTime);
        } while (newCellCount > 0);

        turns++;
        if (turns == enemyLoop[phase].Item1)
        {
            enemyLoop[phase].Item2();

            turns = 0;
            phase = (phase + 1) % enemyLoop.Count;
        }

        intractable = true;
    }
}