using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TNTGameMain : MonoBehaviour
{
    public GameObject cellTemplate;
    public GameObject blockTemplate;
    public Transform cellRoot;
    public BlockController blockController;

    private SampleCellPool<TNTCell> cellPool;
    private bool intractable;

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

        intractable = true;
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        intractable = false;

        cellBlocks.Clear();

        var index = Random.Range(0, offsets.Count);
        blockController.CreateBlock(cellPool.Take(4), offsets[index]);

        intractable = true;

        yield return null;
    }

    public void DropBlock(TNTCellBlock cellBlock)
    {
        cellBlock.transform.SetParent(cellRoot);
        cellBlocks.Add(cellBlock);

        var index = Random.Range(0, offsets.Count);
        blockController.CreateBlock(cellPool.Take(4), offsets[index]);

        StartCoroutine(UpdateCells());
    }

    private IEnumerator UpdateCells()
    {
        while (!cellBlocks.TrueForAll(cellBlock => cellBlock.rigidbody.IsSleeping()))
        {
            yield return null;
        }

        var connected = Bfs();
        foreach (var c in connected)
        {
            c.Destroy();
        }

        var oldList = new List<TNTCellBlock>(cellBlocks); 

        foreach (var cellBlock in oldList)
        {
            cellBlock.UpdateBlock();
        }
    }

    private List<TNTCellObject> GetContacts(TNTCellObject cellObject)
    {
        var collider = cellObject.transform.GetComponent<Collider2D>();

        var contactPoints = new ContactPoint2D[10];
        var count = collider.GetContacts(contactPoints);
        var contacts = new List<TNTCellObject>();

        foreach (Transform tf in cellObject.transform.parent)
        {
            var distance = Vector3.Distance(tf.position, cellObject.transform.position);
            if (Mathf.Abs(distance - 32) < 1.0f)
            {
                contacts.Add(tf.GetComponent<TNTCellObject>());
            }
        }

        for (var i = 0; i < count; i++)
        {
            if (contactPoints[i].rigidbody.bodyType != RigidbodyType2D.Static)
            {
                if (contactPoints[i].collider.transform.GetComponent<TNTCellObject>() != null)
                {
                    contacts.Add(contactPoints[i].collider.transform.GetComponent<TNTCellObject>());
                }
            }
        }

        return contacts;
    }

    private List<TNTCellObject> Bfs(TNTCellObject cellObject)
    {
        var visited = new HashSet<TNTCellObject> { cellObject };

        var queue = new Queue<TNTCellObject>(GetContacts(cellObject).Where(c => c.cell.type == cellObject.cell.type));
        var connected = new List<TNTCellObject>{ cellObject };

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

        return connected;
    }

    private List<TNTCellObject> Bfs()
    {
        var visited = new HashSet<TNTCellObject>();
        var connected = new List<TNTCellObject>();

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
                    connected.AddRange(res);
                }
            }
        }

        return connected.Distinct().ToList();
    }
}