using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    public GameObject baseCellBlock;

    public void CreateBlock(List<TNTCell> cells, List<Vector2> offsets)
    {
        var instance = Instantiate(baseCellBlock, transform).GetComponent<RectTransform>();
        var cellBlock = instance.GetComponent<TNTCellBlock>();
        cellBlock.Init(cells, offsets);
    }
}