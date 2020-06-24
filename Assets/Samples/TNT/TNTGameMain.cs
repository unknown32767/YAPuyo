using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TNTGameMain : MonoBehaviour
{
    [Header("Template")]
    public GameObject cellTemplate;
    public GameObject blockTemplate;
    public GameObject enemyTemplate;

    [Header("Reference")]
    public Transform cellRoot;
    public Rigidbody2D cellRootRigidbody;
    public EdgeCollider2D cellRootCollider;
    public Text status;
    public Text statistic;
    public Text timer;
    public Button debug;
    public BlockController blockController;

    [NonSerialized] public bool intractable;

    private PlayerInstance playerInstance;
    private Transform currentDropdown;
    private SampleCellPool<TNTCell> cellPool;
    private EnemyPool enemyPool;

    private List<float> waitingTimePerStep;
    private List<float> waitingTimePerMove;
    private int stepCount => waitingTimePerStep.Count;
    private int moveCount => waitingTimePerMove.Count;
    private int matchMoveCount;
    private int matchCount;
    private int maxMatchCount;
    private int maxCombo;

    private readonly List<List<Vector2>> offsets = new List<List<Vector2>>
    {
        new List<Vector2>
        {
            new Vector2(0, 1.5f),
            new Vector2(0, 0.5f),
            new Vector2(0, -0.5f),
            new Vector2(0, -1.5f),
        },
        new List<Vector2>
        {
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(0, -1),
        },
        new List<Vector2>
        {
            new Vector2(-1, 1),
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(0, -1),
        },
        new List<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(0, -1),
            new Vector2(-1, 0),
        },
        new List<Vector2>
        {
            new Vector2(0.5f, 1),
            new Vector2(0.5f, 0),
            new Vector2(-0.5f, 0),
            new Vector2(-0.5f, -1),
        },
        new List<Vector2>
        {
            new Vector2(-0.5f, 1),
            new Vector2(-0.5f, 0),
            new Vector2(0.5f, 0),
            new Vector2(0.5f, -1),
        },
        new List<Vector2>
        {
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, -0.5f),
            new Vector2(-0.5f, -0.5f),
            new Vector2(-0.5f, 0.5f),
        },
    };

    public static TNTGameMain instance { get; private set; }

    private List<TNTCellBlock> cellBlocks;
    private List<EnemyInstance> enemies;

    public void CreateBlocK(List<TNTCellObject> cellObjects)
    {
        var rectTransform = Instantiate(blockTemplate, transform).GetComponent<RectTransform>();
        rectTransform.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
        var cellBlock = rectTransform.GetComponent<TNTCellBlock>();
        cellBlock.Init(cellObjects);
        cellBlocks.Add(cellBlock);
    }

    public void RemoveBlock(TNTCellBlock cellBlock)
    {
        cellBlocks.Remove(cellBlock);
        Destroy(cellBlock.gameObject);
    }

    public void UpsideDown()
    {
        cellRoot.Rotate(0, 0, -180);
        for (var i = 0; i < cellRootCollider.points.Length; i++)
        {
            cellRootCollider.points[i] *= new Vector2(1, -1);
        }

        StartCoroutine(UpdateCells());
    }

    public void Shake()
    {
        StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        var oldPosition = cellRoot.position;
        var oldRotation = cellRoot.rotation;
        cellRootRigidbody.constraints = RigidbodyConstraints2D.None;
        cellRootRigidbody.AddForce(Vector2.right * 10.0f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(1.0f);

        cellRootRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
        cellRoot.position = oldPosition;
        cellRoot.rotation = oldRotation;

        StartCoroutine(UpdateCells());
    }

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        TNTCell.SetTemplate(cellTemplate);
        var simpleCellConfig = new Dictionary<TNTCell, int>()
        {
            { new TNTCell { type = 1, color = Color.red }, 10 },
            { new TNTCell { type = 2, color = Color.green }, 10 },
            { new TNTCell { type = 3, color = Color.blue }, 10 },
            { new TNTCell { type = 4, color = Color.yellow }, 10 },
        };
        cellPool = new SampleCellPool<TNTCell>
        {
            cellCounts = simpleCellConfig
        };

        cellBlocks = new List<TNTCellBlock>();

        enemyPool = new EnemyPool
        {
            floatMin = -1,
            floatMax = 3,
            ttk = new Dictionary<Enemy, int>
            {
                {
                    new Enemy
                    {
                        hpMax = 100,
                        atk = 10,
                        friction = 1,
                        bounciness = 0,
                    },
                    10
                },
                {
                    new Enemy
                    {
                        hpMax = 150,
                        atk = 7,
                        friction = 1,
                        bounciness = 0,
                    },
                    15
                }
            }
        };

        debug.onClick.AddListener(() =>
            EditorGUIUtility.PingObject(cellBlocks.LastOrDefault(cellBlock =>
                !cellBlock.GetComponent<Rigidbody2D>().IsSleeping())));

        waitingTimePerStep = new List<float>();
        waitingTimePerMove = new List<float>();

        intractable = true;
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        intractable = false;

        currentDropdown = null;

        Time.timeScale = 10;

        if (cellBlocks.Count > 0)
        {
            cellBlocks.ForEach(cell => Destroy(cell.gameObject));
        }

        cellBlocks.Clear();

        var index = Random.Range(0, offsets.Count);
        blockController.CreateBlock(cellPool.Take(4), offsets[index]);

        waitingTimePerStep.Clear();
        waitingTimePerMove.Clear();

        matchMoveCount = 0;
        matchCount = 0;
        maxMatchCount = 0;
        maxCombo = 0;

        intractable = true;

        yield return null;
    }

    public void DropBlock(TNTCellBlock cellBlock)
    {
        currentDropdown = cellBlock.transform;

        cellBlock.transform.SetParent(cellRoot);
        cellBlocks.Add(cellBlock);

        StartCoroutine(UpdateCells());
    }

    private void DropEnemy(Enemy enemy)
    {
        var enemyTransform = Instantiate(enemyTemplate, cellRoot).GetComponent<RectTransform>();
        var enemyInstance = enemyTransform.GetComponent<EnemyComponent>();

        enemyTransform.anchoredPosition = new Vector2(Random.Range(-100.0f, 100.0f), 240);
        enemyInstance.Init(enemy);
    }

    private IEnumerator UpdateCells()
    {
        intractable = false;
        status.text = "Updating";

        var moveStartTime = Time.time;
        var combo = 0;

        List<List<TNTCellObject>> connects;
        do
        {
            var stepStartTime = Time.time;

            while (!cellBlocks.TrueForAll(cellBlock => cellBlock.GetComponent<Rigidbody2D>().IsSleeping()))
            {
                status.text = "Waiting physic stop";
                timer.text = $"{Time.time - stepStartTime:F}";
                yield return new WaitForFixedUpdate();
            }

            connects = Bfs();

            if (connects.Count > 0)
            {
                matchCount += connects.Count;
                maxMatchCount = Mathf.Max(connects.Count, maxMatchCount);
                combo++;
            }

            foreach (var cell in connects.SelectMany(connected => connected))
            {
                cell.transform.parent.GetComponent<TNTCellBlock>().RemoveCell(cell);
            }

            var oldList = new List<TNTCellBlock>(cellBlocks);

            foreach (var cellBlock in oldList)
            {
                cellBlock.UpdateBlock();
            }

            waitingTimePerStep.Add(Time.time - stepStartTime);
        } while (connects.Count != 0);

        if (combo > 0)
        {
            matchMoveCount++;
            maxCombo = Mathf.Max(combo, maxCombo);
        }

        waitingTimePerMove.Add(Time.time - moveStartTime);

        var index = Random.Range(0, offsets.Count);
        blockController.CreateBlock(cellPool.Take(4), offsets[index]);

        intractable = true;
        status.text = "Done";

        statistic.text = $"更新时间（每步）\t{waitingTimePerMove.Min()}\t{waitingTimePerMove.Max()}\t{waitingTimePerMove.Average()}\n" +
                         $"更新时间（每次消除）\t{waitingTimePerStep.Min()}\t{waitingTimePerStep.Max()}\t{waitingTimePerStep.Average()}\n" +
                         $"行动次数\t{moveCount}\n" +
                         $"消除行动次数\t{matchMoveCount}\t消除行动百分比\t{(float) matchMoveCount / moveCount:P}\n" +
                         $"总消除次数\t{matchCount}\t平均每次连消（次）\t{(float) matchCount / matchMoveCount:F}\n" +
                         $"最大同时消除（次）\t{maxMatchCount}\n" +
                         $"最大连消（次）\t{maxCombo}";

        var nextEnemy = enemyPool.Countdown();
        if (nextEnemy != null)
        {
            DropEnemy(nextEnemy);
        }
    }

    private List<TNTCellObject> GetContacts(TNTCellObject cellObject)
    {
        var collider2d = cellObject.transform.GetComponent<Collider2D>();

        var contactPoints = new ContactPoint2D[10];
        var contactCount = collider2d.GetContacts(contactPoints);
        var contacts = new List<TNTCellObject>();

        foreach (Transform tf in cellObject.transform.parent)
        {
            var distance = Vector3.Distance(tf.position, cellObject.transform.position);
            if (Mathf.Abs(distance - 32) < 1.0f && tf.GetComponent<TNTCellObject>() != null)
            {
                contacts.Add(tf.GetComponent<TNTCellObject>());
            }
        }

        for (var i = 0; i < contactCount; i++)
        {
            var self = contactPoints[i].collider.GetComponent<TNTCellObject>();
            var other = contactPoints[i].otherCollider.GetComponent<TNTCellObject>();

            if (other != cellObject)
            {
                Debug.Log("?");
            }

            if (self != null)
            {
                contacts.Add(self);
            }
        }

        return contacts;
    }

    private List<TNTCellObject> Bfs(TNTCellObject cellObject)
    {
        var visited = new HashSet<TNTCellObject> { cellObject };

        var queue = new Queue<TNTCellObject>(GetContacts(cellObject).Where(c => c.cell.type == cellObject.cell.type));
        var connected = new List<TNTCellObject> { cellObject };

        while (queue.Count != 0)
        {
            var next = queue.Dequeue();

            connected.Add(next);
            visited.Add(next);

            foreach (var tntCellObject in GetContacts(next))
            {
                if (tntCellObject.cell.type == cellObject.cell.type && !visited.Contains(tntCellObject))
                {
                    queue.Enqueue(tntCellObject);
                }
            }
        }

        return connected.Distinct().ToList();
    }

    private List<List<TNTCellObject>> Bfs()
    {
        var visited = new HashSet<TNTCellObject>();
        var connected = new List<List<TNTCellObject>>();

        if (cellBlocks.Count == 0)
        {
            return connected;
        }

        foreach (var cellObject in cellBlocks.SelectMany(cellBlock => cellBlock.cells).Where(cell => cell != null))
        {
            if (!visited.Contains(cellObject))
            {
                var res = Bfs(cellObject);

                res.ForEach(c => visited.Add(c));

                if (res.Count >= 3)
                {
                    connected.Add(res);
                }
            }
        }

        return connected;
    }

    public void OnEnemyCollision(EnemyInstance enemy, Collision2D collision)
    {
        if (collision.transform == currentDropdown)
        {

        }
    }
}