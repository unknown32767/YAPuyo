using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuyoGameMain : MonoBehaviour
{
    public GameObject cellTemplate;
    public Transform cellRoot;

    private Board<SampleCell> board;
    private SampleCellPool<SampleCell> cellPool;

    private bool intractable;
    private List<(SampleCell cell, RectTransform transform)> currentDropdown;

    private void Start()
    {
        SampleCell.cellTemplate = cellTemplate;
        var simpleCellConfig = new Dictionary<SampleCell, int>()
        {
            { new SampleCell { type = 1, color = Color.red }, 20 },
            { new SampleCell { type = 2, color = Color.green }, 20 },
            { new SampleCell { type = 3, color = Color.blue }, 20 },
            { new SampleCell { type = 4, color = Color.yellow }, 20 },
        };
        cellPool = new SampleCellPool<SampleCell>()
        {
            cellCounts = simpleCellConfig
        };

        board = new Board<SampleCell>(8, 16)
        {
            cellSize = new Vector2Int(32, 32),
            gap = new Vector2Int(0, 0),
            cellRoot = cellRoot,
            cellPool = cellPool,
        };
        board.SetVonNeumannNeighbour();

        intractable = true;

        currentDropdown = new List<(SampleCell cell, RectTransform transform)>();
    }

    public void StartGame()
    {
        board.Clear();
        CreateDropdown(cellPool.Take(), cellPool.Take());
    }

    private RectTransform CreateCell(SampleCell cell, Vector2Int pos)
    {
        var cellObject = cell.CreateInstance();
        cellObject.SetParent(cellRoot);
        cellObject.transform.localPosition = board.CellToLocal(pos);

        return cellObject.GetComponent<RectTransform>();
    }

    private void CreateDropdown(SampleCell cell1, SampleCell cell2)
    {
        var tf1 = CreateCell(cell1, new Vector2Int(3, 16));
        var tf2 = CreateCell(cell2, new Vector2Int(3, 17));

        currentDropdown.Add((cell1, tf1));
        currentDropdown.Add((cell2, tf2));
    }

    private void Update()
    {
        if (!intractable)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            var delta = 32 * (Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 1);

            foreach (var (_, tf) in currentDropdown)
            {
                tf.anchoredPosition += new Vector2(delta, 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            board.DropDownCells(currentDropdown);
            currentDropdown.Clear();
            CreateDropdown(cellPool.Take(), cellPool.Take());

            StartCoroutine(UpdateCells());
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            var tf0 = currentDropdown[0].transform;
            var tf1 = currentDropdown[1].transform;

            tf1.RotateAround(tf0.position, Vector3.forward, -90);
        }
    }

    private IEnumerator UpdateCells()
    {
        intractable = false;

        var connected = board.FindAllConnected(4);

        while (connected.Count != 0)
        {
            yield return new WaitForSeconds(board.RemoveCells(connected));
            yield return new WaitForSeconds(board.Collapse());

            connected = board.FindAllConnected(4);
        }

        intractable = true;
    }
}