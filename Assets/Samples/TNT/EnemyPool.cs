using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyPool
{
    public Dictionary<Enemy, int> ttk;
    public int floatMin;
    public int floatMax;

    private int countdown;

    public EnemyPool()
    {
        countdown = 0;
    }

    public Enemy Countdown()
    {
        countdown--;

        if (countdown <= 0)
        {
            var selected = ttk.ElementAt(Random.Range(0, ttk.Count));
            countdown = Mathf.Max(1, selected.Value + Random.Range(floatMin, floatMax));
            return selected.Key;
        }

        return null;
    }
}