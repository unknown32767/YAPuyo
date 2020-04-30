using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CellPool : ICellPool
{
    public Dictionary<Cell, int> cellCounts;

    public List<Cell> cellPool;

    public Cell Take()
    {
        if (cellPool.Count == 0)
        {
            MakePool();
        }

        var index = Random.Range(0, cellPool.Count);
        var res = cellPool[index];
        cellPool.RemoveAt(index);
        return res;
    }

    public List<Cell> Take(int count)
    {
        var res = new List<Cell>();
        for (int i = 0; i < count; i++)
        {
            var index = Random.Range(0, cellPool.Count);
            res.Add(cellPool[index]);
            cellPool.RemoveAt(index);
        }

        if (cellPool.Count == 0)
        {
            MakePool();
        }

        return res;
    }

    private void MakePool()
    {
        foreach (var kv in cellCounts)
        {
            cellPool.AddRange(Enumerable.Repeat(kv.Key, kv.Value));
        }
    }
}