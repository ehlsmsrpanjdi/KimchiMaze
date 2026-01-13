using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameStarter : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] private int mazeSize = 11;          // 홀수
    [SerializeField] private float loopChance = 0.04f;   // 0.03~0.06 추천
    [SerializeField] private int goalMargin = 1;         // 1이면 goal이 0/size-1 좌표 가지면 실패(끝부분/외벽 금지)

    [Header("Prefabs")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Entrance/Start")]
    [SerializeField] private Vector3Int entrance = new Vector3Int(1, 1, 0); // 외벽 구멍 1개

    // EditorWindow에서 참조하기 좋게 공개
    public int MazeSize => mazeSize;
    public int GoalMargin => goalMargin;
    public float LoopChance => loopChance;
    public Vector3Int Entrance => entrance;

    private bool[,,] maze;
    private Vector3Int goalCell;

    // 퍼즐 오브젝트를 한 곳에 모아 삭제/재생성을 빠르게
    private Transform puzzleRoot;

    void Awake()
    {
        EnsurePuzzleRoot();
    }

    void Start()
    {
        // 자동 생성 원치 않으면 주석 처리
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

    // ====== EditorWindow용: 유지되어야 하는 API ======

    public void ClearPuzzle()
    {
        EnsurePuzzleRoot();

        for (int i = puzzleRoot.childCount - 1; i >= 0; i--)
        {
            var child = puzzleRoot.GetChild(i).gameObject;
            DestroySmart(child);
        }
        maze = null;
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

        // 생성
        var gen = new MazeGenerator();
        maze = gen.Generate(mazeSize, seed, entrance, loopChance);

        // 목표 선택(입구에서 최장거리 + goalMargin 내부)
        if (!TryPickGoalAndPath(maze, mazeSize, entrance, goalMargin, out goalCell, out var path))
        {
            Debug.LogError($"[MAZE] Goal pick failed. seed={seed} entrance={entrance}");
            return;
        }

        // 비주얼 빌드
        BuildVisual();

        Debug.Log($"[MAZE BUILT] seed={seed} entrance={entrance} goal={goalCell} dist={path.Count - 1}");
    }

    /// <summary>
    /// 현재 생성된 미로가 유효한지 확인(도달 가능/시작점/goal 경계 금지)
    /// </summary>
    public (bool ok, string msg, Vector3Int goal, int dist) ValidateCurrentMaze()
    {
        if (maze == null)
            return (false, "maze is null (not generated)", goalCell, -1);

        // 요구: 시작지점이 entrance와 일치하는지
        if (entrance != new Vector3Int(1, 1, 0))
        {
            // 입구를 고정하겠다 하셨으니, 정책상 체크를 넣어둡니다(원하면 제거 가능).
        }

        // 시작점(입구) 검사
        if (maze[entrance.x, entrance.y, entrance.z])
            return (false, $"entrance is wall: {entrance}", goalCell, -1);

        // goal 검사(길인지)
        if (maze[goalCell.x, goalCell.y, goalCell.z])
            return (false, $"goal is wall: {goalCell}", goalCell, -1);

        // goal 경계 금지 검사
        if (!IsInsideWithMargin(goalCell, mazeSize, goalMargin))
            return (false, $"goal violates boundary rule (margin={goalMargin}) goal={goalCell}", goalCell, -1);

        // 도달 가능 검사 + 거리
        int dist = BfsDistance(maze, mazeSize, entrance, goalCell);
        if (dist < 0)
            return (false, $"goal not reachable entrance={entrance} goal={goalCell}", goalCell, -1);

        return (true, $"OK dist={dist}", goalCell, dist);
    }

    // ====== 내부 구현 ======

    void BuildVisual()
    {
        EnsurePuzzleRoot();

        for (int x = 0; x < mazeSize; x++)
            for (int y = 0; y < mazeSize; y++)
                for (int z = 0; z < mazeSize; z++)
                {
                    Vector3 pos = new Vector3(x, y, z);

                    if (maze[x, y, z]) // wall
                    {
                        var cube = InstantiateSmart(cubePrefab, pos, Quaternion.identity, puzzleRoot);
                        cube.isStatic = true;
                    }
                    else if (x == goalCell.x && y == goalCell.y && z == goalCell.z)
                    {
                        InstantiateSmart(goalPrefab, pos, Quaternion.identity, puzzleRoot);
                    }
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

        // margin 내부에서 최장거리
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

        // 경로 복원
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
                dist[nxt.x, nxt.y, nxt.z] = cd + 1;
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
