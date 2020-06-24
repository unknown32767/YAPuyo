using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyInstance
{
    public float hp;

    public Enemy enemyData;

    public void Init(Enemy enemy)
    {
        enemyData = enemy;

        hp = enemy.hpMax;
    }
}