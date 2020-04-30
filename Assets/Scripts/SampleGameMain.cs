using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SampleGameMain : MonoBehaviour, IPointerClickHandler
{
    private Board<SampleCell> board;

    private bool intractable;

    private void Start()
    {
        board = new Board<SampleCell>(8, 8)
        {
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

        var waitTime = board.RemoveCells(connected);

        yield return new WaitForSeconds(waitTime);

        waitTime = board.FillCellInPlace();

        yield return new WaitForSeconds(waitTime);

        intractable = true;
    }
}
