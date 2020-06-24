using UnityEngine;
using UnityEngine.UI;

public class EnemyComponent : MonoBehaviour
{
    private EnemyInstance enemyInstance;

    private Text hpText;
    private Text atkText;

    public void Start()
    {
        hpText = transform.Find("hp").GetComponent<Text>();
        atkText = transform.Find("atk").GetComponent<Text>();
    }

    public void Update()
    {
        hpText.text = $"{Mathf.FloorToInt(enemyInstance.hp)}";
        atkText.text = $"{enemyInstance.enemyData.atk}";
    }

    public void Init(Enemy enemy)
    {
        enemyInstance = new EnemyInstance();
        enemyInstance.Init(enemy);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        TNTGameMain.instance.OnEnemyCollision(enemyInstance, other);
    }
}