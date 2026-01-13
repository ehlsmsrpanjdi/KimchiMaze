using System;
using System.Collections.Generic;
using UnityEngine;

public static class MazeDebugUtility
{
    public struct ValidationResult
    {
        public bool ok;
        public string message;
        public int distance;               // start->goal 최단거리
        public int reachableOpenCount;      // start에서 도달 가능한 길 칸 수
        public Vector3Int goal;
    }

    /// <summary>
    /// goalMargin=1이면 (1..size-2)만 허용(경계면 전부 금지). size=13이면 goal 좌표에 0 또는 12가 나오면 실패.
    /// </summary>
    public static bool TrySelectGoalAndPath(
        bool[,,] maze,
        int size,
        Vector3Int start,
        int goalMargin,
        out Vector3Int goal,
        out List<Vector3Int> path,
        out int reachableOpenCount,
        out string failReason)
    {
        goal = start;
        path = null;
        reachableOpenCount = 0;
        failReason = null;

        if (!IsInBounds(start, size))
        {
            failReason = $"Start out of bounds: {start}";
            return false;
        }
        if (maze[start.x, start.y, start.z])
        {
            failReason = $"Start is wall: {start}";
            return false;
        }

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
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            reachableOpenCount++;

            int cd = dist[cur.x, cur.y, cur.z];
            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if (!IsInBounds(nxt, size)) continue;
                if (maze[nxt.x, nxt.y, nxt.z]) continue;          // wall
                if (dist[nxt.x, nxt.y, nxt.z] != -1) continue;     // visited

                dist[nxt.x, nxt.y, nxt.z] = cd + 1;
                parent[nxt.x, nxt.y, nxt.z] = cur;
                q.Enqueue(nxt);
            }
        }

        // 1) 경계면 전부 금지(goalMargin 기반) 중 최장거리
        if (!TryPickFarthest(dist, size, (x, y, z, s) => IsInsideWithMargin(x, y, z, s, goalMargin), out goal))
        {
            // 2) fallback: 코너만 금지(끝부분이 너무 빡세서 내부가 없을 때 대비)
            if (!TryPickFarthest(dist, size, NotCorner, out goal))
            {
                // 3) fallback: 그냥 최장거리
                if (!TryPickFarthest(dist, size, (_, __, ___, ____) => true, out goal))
                {
                    failReason = "No reachable open cell found (unexpected).";
                    return false;
                }
            }
        }

        if (dist[goal.x, goal.y, goal.z] < 0)
        {
            failReason = $"Picked goal unreachable (unexpected): {goal}";
            return false;
        }

        path = ReconstructPath(parent, start, goal);
        if (path == null || path.Count == 0)
        {
            failReason = "Path reconstruct failed.";
            return false;
        }

        return true;
    }

    public static ValidationResult Validate(
        bool[,,] maze,
        int size,
        Vector3Int start,
        Vector3Int goal,
        int goalMargin)
    {
        ValidationResult r = new ValidationResult
        {
            ok = false,
            message = "",
            distance = -1,
            reachableOpenCount = 0,
            goal = goal
        };

        if (!IsInBounds(start, size) || start != Vector3Int.zero)
        {
            r.message = $"Start invalid. Expected (0,0,0), got {start}";
            return r;
        }

        if (!IsInBounds(goal, size))
        {
            r.message = $"Goal out of bounds: {goal}";
            return r;
        }

        if (maze[start.x, start.y, start.z])
        {
            r.message = $"Start is wall: {start}";
            return r;
        }

        if (maze[goal.x, goal.y, goal.z])
        {
            r.message = $"Goal is wall: {goal}";
            return r;
        }

        // goal 경계 금지: goalMargin=1이면 0/size-1 포함하면 무조건 실패
        if (!IsInsideWithMargin(goal.x, goal.y, goal.z, size, goalMargin))
        {
            r.message = $"Goal violates boundary rule (margin={goalMargin}). goal={goal}, size={size}";
            return r;
        }

        // BFS로 도달 가능/거리 계산
        var bfs = BfsDistance(maze, size, start, goal, out int reachableOpen);
        r.reachableOpenCount = reachableOpen;
        r.distance = bfs;

        if (bfs < 0)
        {
            r.message = $"Goal not reachable from start. start={start}, goal={goal}, reachableOpen={reachableOpen}";
            return r;
        }

        r.ok = true;
        r.message = $"OK (dist={bfs}, reachableOpen={reachableOpen})";
        return r;
    }

    public static List<Vector3Int> ReconstructPath(Vector3Int[,,] parent, Vector3Int start, Vector3Int goal)
    {
        List<Vector3Int> rev = new List<Vector3Int>(256);
        Vector3Int cur = goal;

        while (cur != new Vector3Int(-1, -1, -1))
        {
            rev.Add(cur);
            if (cur == start) break;
            cur = parent[cur.x, cur.y, cur.z];
        }

        if (rev.Count == 0 || rev[rev.Count - 1] != start)
            return null;

        rev.Reverse();
        return rev;
    }

    public static int BfsDistance(bool[,,] maze, int size, Vector3Int start, Vector3Int goal, out int reachableOpenCount)
    {
        reachableOpenCount = 0;

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
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            reachableOpenCount++;

            if (cur == goal)
                return dist[cur.x, cur.y, cur.z];

            int cd = dist[cur.x, cur.y, cur.z];
            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if (!IsInBounds(nxt, size)) continue;
                if (maze[nxt.x, nxt.y, nxt.z]) continue;
                if (dist[nxt.x, nxt.y, nxt.z] != -1) continue;

                dist[nxt.x, nxt.y, nxt.z] = cd + 1;
                q.Enqueue(nxt);
            }
        }

        return -1;
    }

    static bool TryPickFarthest(int[,,] dist, int size, Func<int, int, int, int, bool> predicate, out Vector3Int best)
    {
        best = Vector3Int.zero;
        int bestD = -1;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    int d = dist[x, y, z];
                    if (d < 0) continue;
                    if (!predicate(x, y, z, size)) continue;

                    if (d > bestD)
                    {
                        bestD = d;
                        best = new Vector3Int(x, y, z);
                    }
                }
        return bestD >= 0;
    }

    static bool IsInBounds(Vector3Int p, int size)
        => (uint)p.x < (uint)size && (uint)p.y < (uint)size && (uint)p.z < (uint)size;

    static bool IsInsideWithMargin(int x, int y, int z, int size, int margin)
        => x >= margin && x <= size - 1 - margin &&
           y >= margin && y <= size - 1 - margin &&
           z >= margin && z <= size - 1 - margin;

    static bool NotCorner(int x, int y, int z, int size)
    {
        bool bx = (x == 0 || x == size - 1);
        bool by = (y == 0 || y == size - 1);
        bool bz = (z == 0 || z == size - 1);
        return !(bx && by && bz);
    }
}
