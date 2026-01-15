using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector3Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;

    private bool[,,] maze;
    private int mazeSize;
    private Vector3Int goalPosition; // 추가

    private CameraController cameraController;

    public Vector3Int GridPosition => gridPosition;
    public event Action<Vector3Int> OnPositionChanged;
    public event Action<float> OnGoalReached; // 도착 이벤트

    public void Initialize(Vector3Int startPos, bool[,,] mazeData, int size, Vector3Int goal)
    {
        gridPosition = startPos;
        maze = mazeData;
        mazeSize = size;
        goalPosition = goal;

        targetWorldPosition = GridToWorld(gridPosition);
        transform.position = targetWorldPosition;

        cameraController = Camera.main.GetComponent<CameraController>();

        // 타이머 시작
        TimeManager.Instance.StartTimer();

        OnPositionChanged?.Invoke(gridPosition);
    }

    void Update()
    {
        if (isMoving)
        {
            MoveToTarget();
        }
        else
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        if (cameraController == null) return;

        Transform cam = cameraController.transform;
        Vector3 worldDir = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W)) worldDir = cam.forward;
        else if (Input.GetKeyDown(KeyCode.S)) worldDir = -cam.forward;
        else if (Input.GetKeyDown(KeyCode.A)) worldDir = -cam.right;
        else if (Input.GetKeyDown(KeyCode.D)) worldDir = cam.right;
        else if (Input.GetKeyDown(KeyCode.Q)) worldDir = cam.up;
        else if (Input.GetKeyDown(KeyCode.E)) worldDir = -cam.up;

        if (worldDir != Vector3.zero)
        {
            Vector3Int moveDir = WorldDirToGrid(worldDir);
            TryMove(moveDir);
        }
    }

    Vector3Int WorldDirToGrid(Vector3 dir)
    {
        dir.Normalize();

        float ax = Mathf.Abs(dir.x);
        float ay = Mathf.Abs(dir.y);
        float az = Mathf.Abs(dir.z);

        if (ax >= ay && ax >= az)
            return dir.x > 0 ? Vector3Int.right : Vector3Int.left;

        if (ay >= ax && ay >= az)
            return dir.y > 0 ? Vector3Int.up : Vector3Int.down;

        return dir.z > 0 ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
    }

    void TryMove(Vector3Int direction)
    {
        Vector3Int newPos = gridPosition + direction;

        if (newPos.x < -1 || newPos.x > mazeSize ||
            newPos.y < -1 || newPos.y > mazeSize ||
            newPos.z < -1 || newPos.z > mazeSize)
        {
            return;
        }

        if (IsInsideMaze(newPos))
        {
            if (maze[newPos.x, newPos.y, newPos.z])
            {
                return;
            }
        }

        gridPosition = newPos;
        targetWorldPosition = GridToWorld(gridPosition);
        isMoving = true;
    }

    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            transform.position = targetWorldPosition;
            isMoving = false;

            OnPositionChanged?.Invoke(gridPosition);

            // 도착 체크
            CheckGoalReached();
        }
    }

    void CheckGoalReached()
    {
        if (gridPosition == goalPosition)
        {
            float clearTime = TimeManager.Instance.EndTimer();
            Debug.Log($"Goal Reached! Time: {clearTime:F2}s");
            OnGoalReached?.Invoke(clearTime);
        }
    }

    public void DebugGoal()
    {
        float clearTime = TimeManager.Instance.EndTimer();
        OnGoalReached?.Invoke(clearTime);
    }

    bool IsInsideMaze(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < mazeSize &&
               pos.y >= 0 && pos.y < mazeSize &&
               pos.z >= 0 && pos.z < mazeSize;
    }

    Vector3 GridToWorld(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x, gridPos.y, gridPos.z);
    }

    // 디버그: 경로 표시
    public void ShowPathToGoal()
    {
        var path = FindPath(gridPosition, goalPosition);
        if (path == null || path.Count == 0)
        {
            Debug.Log("No path to goal!");
            return;
        }

        Debug.Log($"Path to goal ({path.Count} steps):");
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"  Step {i}: {path[i]}");
        }
    }

    System.Collections.Generic.List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        int[,,] dist = new int[mazeSize, mazeSize, mazeSize];
        Vector3Int[,,] parent = new Vector3Int[mazeSize, mazeSize, mazeSize];

        for (int x = 0; x < mazeSize; x++)
            for (int y = 0; y < mazeSize; y++)
                for (int z = 0; z < mazeSize; z++)
                {
                    dist[x, y, z] = -1;
                    parent[x, y, z] = new Vector3Int(-1, -1, -1);
                }

        System.Collections.Generic.Queue<Vector3Int> q = new System.Collections.Generic.Queue<Vector3Int>();
        q.Enqueue(start);
        dist[start.x, start.y, start.z] = 0;

        Vector3Int[] dirs =
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == goal) break;

            int cd = dist[cur.x, cur.y, cur.z];
            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if (!IsInsideMaze(nxt)) continue;
                if (maze[nxt.x, nxt.y, nxt.z]) continue;
                if (dist[nxt.x, nxt.y, nxt.z] != -1) continue;

                dist[nxt.x, nxt.y, nxt.z] = cd + 1;
                parent[nxt.x, nxt.y, nxt.z] = cur;
                q.Enqueue(nxt);
            }
        }

        if (dist[goal.x, goal.y, goal.z] < 0) return null;

        // 경로 복원
        var path = new System.Collections.Generic.List<Vector3Int>();
        var p = goal;
        while (p != new Vector3Int(-1, -1, -1))
        {
            path.Add(p);
            if (p == start) break;
            p = parent[p.x, p.y, p.z];
        }

        path.Reverse();
        return path;
    }
}