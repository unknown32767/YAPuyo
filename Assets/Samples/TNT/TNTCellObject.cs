using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TNTCellObject : MonoBehaviour
{
    public TNTCell cell;

    public void Destroy()
    {
        var cellBlock = transform.parent.GetComponent<TNTCellBlock>();
        cellBlock.RemoveCell(this);
    }
}