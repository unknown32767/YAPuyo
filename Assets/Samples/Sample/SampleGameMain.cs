using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SampleGameMain : MonoBehaviour, IPointerClickHandler
{
    public GameObject cellTemplate;
    public Transform cellRoot;

    private Board<SampleCell> board;

    private bool intractable;

    private void Start()
    {
        SampleCell.cellTemplate = cellTemplate;
        var simpleCellConfig = new Dictionary<SampleCell, int>()
        {
            { new SampleCell { type = 1, color = Color.red }, 10 },
            { new SampleCell { type = 2, color = Color.green }, 10 },
            { new SampleCell { type = 3, color = Color.blue }, 10 },
            { new SampleCell { type = 4, color = Color.yellow }, 10 },
        };
        var cellPool = new SampleCellPool<SampleCell>()
        {
            cellCounts = simpleCellConfig
        };

        board = new Board<SampleCell>(8, 8)
        {
            cellSize = new Vector2Int(50, 50),
            gap = new Vector2Int(10, 10),
            cellRoot = cellRoot,
            cellPool = cellPool,
        };
        board.SetVonNeumannNeighbour();

        intractable = true;
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        intractable = false;

        var waitTime = board.FillCellInPlace();

        yield return new WaitForSeconds(waitTime);

        intractable = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!intractable)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), eventData.position,
            eventData.pressEventCamera, out var localPos);

        var clickIndex = board.LocalToCell(localPos);

        StartCoroutine(DoRemove(clickIndex));
    }

    private IEnumerator DoRemove(Vector2Int pos)
    {
        intractable = false;

        var connected = board.FindConnected(pos);

        yield return new WaitForSeconds(board.RemoveCells(connected));
        yield return new WaitForSeconds(board.Collapse());
        yield return new WaitForSeconds(board.FillCellDropDown());

        intractable = true;
    }
}