using System;
using UnityEngine;

[Serializable]
public abstract class Character
{
    public int hpMax;
    public int atk;

    public abstract void Init();
}

[Serializable]
public class Enemy : Character
{
    public float friction;
    public float bounciness;
    
    [NonSerialized] public PhysicsMaterial2D physicMaterial;

    public override void Init()
    {
        physicMaterial = new PhysicsMaterial2D
        {
            bounciness = bounciness,
            friction = friction
        };
    }
}

[Serializable]
public class Player : Character
{
    public override void Init()
    {
    }
}