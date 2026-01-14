using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameStarter : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] private int mazeSize = 11;
    [SerializeField] private float loopChance = 0.04f;
    [SerializeField] private int goalMargin = 1;

    [Header("Prefabs")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject playerPrefab;

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;

    [Header("Entrance/Start")]
    [SerializeField] private Vector3Int entrance = new Vector3Int(1, 1, 0);

    public int MazeSize => mazeSize;
    public int GoalMargin => goalMargin;
    public float LoopChance => loopChance;
    public Vector3Int Entrance => entrance;

    private bool[,,] maze;
    private Vector3Int goalCell;
    private Transform puzzleRoot;
    private PlayerController player;

    void Awake()
    {
        EnsurePuzzleRoot();
    }

    void Start()
    {
        GenerateTodayAndBuild();
    }

    void EnsurePuzzleRoot()
    {
        if (puzzleRoot != null) return;

        var existing = transform.Find("PuzzleRoot");
        if (existing != null) puzzleRoot = existing;
        else
        {
            var go = new GameObject("PuzzleRoot");
            go.transform.SetParent(transform, false);
            puzzleRoot = go.transform;
        }
    }

    public void ClearPuzzle()
    {
        EnsurePuzzleRoot();

        for (int i = puzzleRoot.childCount - 1; i >= 0; i--)
        {
            var child = puzzleRoot.GetChild(i).gameObject;
            DestroySmart(child);
        }

        if (player != null)
        {
            DestroySmart(player.gameObject);
            player = null;
        }

        maze = null;
        CubeVisibilityManager.Instance.Clear();
    }

    public void GenerateTodayAndBuild()
    {
        int seed = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        GenerateWithSeedAndBuild(seed);
    }

    public void GenerateRandomAndBuild(int seed)
    {
        GenerateWithSeedAndBuild(seed);
    }

    public void GenerateWithSeedAndBuild(int seed)
    {
        EnsurePuzzleRoot();
        ClearPuzzle();

        var gen = new MazeGenerator();
        maze = gen.Generate(mazeSize, seed, entrance, loopChance);

        if (!TryPickGoalAndPath(maze, mazeSize, entrance, goalMargin, out goalCell, out var path))
        {
            Debug.LogError($"[MAZE] Goal pick failed. seed={seed} entrance={entrance}");
            return;
        }

        CubeVisibilityManager.Instance.Initialize();

        BuildVisual();
        SpawnPlayer();

        Debug.Log($"[MAZE BUILT] seed={seed} entrance={entrance} goal={goalCell} dist={path.Count - 1}");
    }

    public (bool ok, string msg, Vector3Int goal, int dist) ValidateCurrentMaze()
    {
        if (maze == null)
            return (false, "maze is null (not generated)", goalCell, -1);

        if (maze[entrance.x, entrance.y, entrance.z])
            return (false, $"entrance is wall: {entrance}", goalCell, -1);

        if (maze[goalCell.x, goalCell.y, goalCell.z])
            return (false, $"goal is wall: {goalCell}", goalCell, -1);

        if (!IsInsideWithMargin(goalCell, mazeSize, goalMargin))
            return (false, $"goal violates boundary rule (margin={goalMargin}) goal={goalCell}", goalCell, -1);

        int dist = BfsDistance(maze, mazeSize, entrance, goalCell);
        if (dist < 0)
            return (false, $"goal not reachable entrance={entrance} goal={goalCell}", goalCell, -1);

        return (true, $"OK dist={dist}", goalCell, dist);
    }

    void BuildVisual()
    {
        EnsurePuzzleRoot();

        for (int x = 0; x < mazeSize; x++)
            for (int y = 0; y < mazeSize; y++)
                for (int z = 0; z < mazeSize; z++)
                {
                    Vector3 pos = new Vector3(x, y, z);

                    if (maze[x, y, z])
                    {
                        var cube = InstantiateSmart(cubePrefab, pos, Quaternion.identity, puzzleRoot);
                        cube.isStatic = false;
                        CubeVisibilityManager.Instance.RegisterCube(new Vector3Int(x, y, z), cube);
                    }
                    else if (x == goalCell.x && y == goalCell.y && z == goalCell.z)
                    {
                        InstantiateSmart(goalPrefab, pos, Quaternion.identity, puzzleRoot);
                    }
                }
    }

    void SpawnPlayer()
    {
        if (player != null)
        {
            DestroySmart(player.gameObject);
        }

        var playerObj = InstantiateSmart(playerPrefab, Vector3.zero, Quaternion.identity, transform);
        player = playerObj.GetComponent<PlayerController>();

        if (player != null)
        {
            player.Initialize(entrance, maze, mazeSize);

            if (cameraController != null)
            {
                cameraController.SetTarget(player.transform);
            }

            CubeVisibilityManager.Instance.SetPlayer(player);
        }
    }

    static bool TryPickGoalAndPath(bool[,,] maze, int size, Vector3Int start, int margin,
        out Vector3Int goal, out List<Vector3Int> path)
    {
        goal = start;
        path = null;

        if (maze[start.x, start.y, start.z]) return false;

        int[,,] dist = new int[size, size, size];
        Vector3Int[,,] parent = new Vector3Int[size, size, size];

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    dist[x, y, z] = -1;
                    parent[x, y, z] = new Vector3Int(-1, -1, -1);
                }

        Queue<Vector3Int> q = new Queue<Vector3Int>();
        q.Enqueue(start);
        dist[start.x, start.y, start.z] = 0;

        Vector3Int[] dirs =
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1),
        };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int cd = dist[cur.x, cur.y, cur.z];

            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if ((uint)nxt.x >= (uint)size || (uint)nxt.y >= (uint)size || (uint)nxt.z >= (uint)size) continue;
                if (maze[nxt.x, nxt.y, nxt.z]) continue;
                if (dist[nxt.x, nxt.y, nxt.z] != -1) continue;

                dist[nxt.x, nxt.y, nxt.z] = cd + 1;
                parent[nxt.x, nxt.y, nxt.z] = cur;
                q.Enqueue(nxt);
            }
        }

        int bestD = -1;
        Vector3Int best = start;

        int min = margin;
        int max = size - 1 - margin;

        for (int x = min; x <= max; x++)
            for (int y = min; y <= max; y++)
                for (int z = min; z <= max; z++)
                {
                    int d = dist[x, y, z];
                    if (d > bestD)
                    {
                        bestD = d;
                        best = new Vector3Int(x, y, z);
                    }
                }

        if (bestD < 0) return false;
        goal = best;

        List<Vector3Int> rev = new List<Vector3Int>(256);
        var p = goal;
        while (p != new Vector3Int(-1, -1, -1))
        {
            rev.Add(p);
            if (p == start) break;
            p = parent[p.x, p.y, p.z];
        }
        if (rev.Count == 0 || rev[rev.Count - 1] != start) return false;
        rev.Reverse();
        path = rev;
        return true;
    }

    static bool IsInsideWithMargin(Vector3Int p, int size, int margin)
        => p.x >= margin && p.x <= size - 1 - margin &&
           p.y >= margin && p.y <= size - 1 - margin &&
           p.z >= margin && p.z <= size - 1 - margin;

    static int BfsDistance(bool[,,] maze, int size, Vector3Int start, Vector3Int goal)
    {
        int[,,] dist = new int[size, size, size];
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                    dist[x, y, z] = -1;

        Queue<Vector3Int> q = new Queue<Vector3Int>();
        q.Enqueue(start);
        dist[start.x, start.y, start.z] = 0;

        Vector3Int[] dirs =
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1),
        };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == goal) return dist[cur.x, cur.y, cur.z];

            int cd = dist[cur.x, cur.y, cur.z];
            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if ((uint)nxt.x >= (uint)size || (uint)nxt.y >= (uint)size || (uint)nxt.z >= (uint)size) continue;
                if (maze[nxt.x, nxt.y, nxt.z]) continue;
                if (dist[nxt.x, nxt.y, nxt.z] != -1) continue;

                dist[nxt.x, nxt.y, nxt.z] = cd + 1;
                q.Enqueue(nxt);
            }
        }
        return -1;
    }

    static GameObject InstantiateSmart(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            return go;
        }
#endif
        return Instantiate(prefab, pos, rot, parent);
    }

    static void DestroySmart(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(go);
        else Destroy(go);
#else
        Destroy(go);
#endif
    }
}