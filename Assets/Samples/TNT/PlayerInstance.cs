using UnityEngine;

public class PlayerInstance
{
    private float hp;

    private Player playerData;

    public void Init(Player player)
    {
        playerData = player;

        hp = player.hpMax;
    }
}