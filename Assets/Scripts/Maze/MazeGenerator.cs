using UnityEngine;

public class MazeGenerator
{
    private int size;
    private Vector3Int entrance;
    private float loopChance;

    /// <param name="entrancePos">예: (1,1,0) - 외벽 위의 구멍 1개</param>
    /// <param name="loopChance01">0.02~0.08 정도 추천</param>
    public bool[,,] Generate(int mazeSize, int seed, Vector3Int entrancePos, float loopChance01 = 0.04f)
    {
        if (mazeSize < 5 || mazeSize % 2 == 0)
        {
            Debug.LogError($"mazeSize는 5 이상 홀수여야 합니다. 현재: {mazeSize}");
            mazeSize = Mathf.Max(5, mazeSize | 1);
        }

        size = mazeSize;
        entrance = entrancePos;
        loopChance = Mathf.Clamp01(loopChance01);

        Random.InitState(seed);

        bool[,,] maze = new bool[size, size, size];

        // 1) 전부 벽으로 초기화 (외벽 포함)
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                    maze[x, y, z] = true;

        // 2) 내부 미로 시작점: 입구 바로 안쪽으로 고정 (entrance=(1,1,0) => start=(1,1,1))
        Vector3Int innerStart = new Vector3Int(entrance.x, entrance.y, entrance.z + 1);

        if (!IsInsideInner(innerStart))
        {
            Debug.LogError($"입구 {entrance} 기준 innerStart {innerStart}가 내부가 아닙니다. entrance를 (1,1,0)처럼 외벽에 두세요.");
            return maze;
        }

        // 시작점은 길
        maze[innerStart.x, innerStart.y, innerStart.z] = false;

        // 3) 내부만 carving (외벽은 절대 건드리지 않음)
        CarvePath(maze, innerStart.x, innerStart.y, innerStart.z);

        // 4) 루프 소량 추가(브레이딩) - 내부 벽 일부 제거
        AddLoops(maze);

        // 5) 외벽에 구멍 1개(입구) 뚫기 + 연결 보장
        maze[entrance.x, entrance.y, entrance.z] = false;
        maze[innerStart.x, innerStart.y, innerStart.z] = false;

        return maze;
    }

    public bool[,,] Generate(int size, int seed)
    {
        // 기본값: 입구 (1,1,0), 루프 4%
        return Generate(size, seed, new Vector3Int(1, 1, 0), 0.04f);
    }

    public bool[,,] Generate(int size, int seed, Vector3Int entrancePos)
    {
        return Generate(size, seed, entrancePos, 0.04f);
    }

    void CarvePath(bool[,,] maze, int x, int y, int z)
    {
        maze[x, y, z] = false;

        Vector3Int[] dirs =
        {
            new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
            new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1),
        };

        // 셔플
        for (int i = 0; i < dirs.Length; i++)
        {
            int r = Random.Range(i, dirs.Length);
            (dirs[i], dirs[r]) = (dirs[r], dirs[i]);
        }

        foreach (var d in dirs)
        {
            int nx = x + d.x * 2;
            int ny = y + d.y * 2;
            int nz = z + d.z * 2;

            // "내부"만 허용 (외벽: 0 또는 size-1은 금지)
            if (!IsInsideInner(nx, ny, nz)) continue;

            // 아직 방문 안한 내부(벽)만 진행
            if (maze[nx, ny, nz])
            {
                // 중간 벽 제거
                int mx = x + d.x;
                int my = y + d.y;
                int mz = z + d.z;

                // 중간칸도 내부 보장
                if (IsInsideInner(mx, my, mz))
                    maze[mx, my, mz] = false;

                CarvePath(maze, nx, ny, nz);
            }
        }
    }

    void AddLoops(bool[,,] maze)
    {
        // 내부에서만 벽을 일부 제거해서 루프 생성
        // 조건:
        // - 해당 칸이 벽(true)
        // - 양쪽(반대 방향) 두 칸이 모두 길(false)일 때만 뚫기 => “통로-벽-통로” 연결로 루프가 생김
        for (int x = 1; x < size - 1; x++)
            for (int y = 1; y < size - 1; y++)
                for (int z = 1; z < size - 1; z++)
                {
                    if (!maze[x, y, z]) continue;                 // 이미 길이면 패스
                    if (Random.value > loopChance) continue;      // 확률

                    // 외벽 바로 안쪽이라도 괜찮지만, 원하면 더 깊게 제한할 수 있음(예: x,y,z >=2..)
                    // 여기서는 내부(1..size-2)만 보장.

                    bool connectX = !maze[x - 1, y, z] && !maze[x + 1, y, z];
                    bool connectY = !maze[x, y - 1, z] && !maze[x, y + 1, z];
                    bool connectZ = !maze[x, y, z - 1] && !maze[x, y, z + 1];

                    // “반대 방향 두 칸이 길”인 경우만 뚫어서 과도한 방(공간) 생성 방지
                    if (connectX || connectY || connectZ)
                    {
                        // 추가로 "열린 이웃 수"가 너무 많으면 방처럼 뻥 뚫릴 수 있으니 제한
                        int openNeighbors = 0;
                        if (!maze[x - 1, y, z]) openNeighbors++;
                        if (!maze[x + 1, y, z]) openNeighbors++;
                        if (!maze[x, y - 1, z]) openNeighbors++;
                        if (!maze[x, y + 1, z]) openNeighbors++;
                        if (!maze[x, y, z - 1]) openNeighbors++;
                        if (!maze[x, y, z + 1]) openNeighbors++;

                        if (openNeighbors <= 2)
                            maze[x, y, z] = false;
                    }
                }
    }

    bool IsInsideInner(Vector3Int p) => IsInsideInner(p.x, p.y, p.z);

    bool IsInsideInner(int x, int y, int z)
        => x > 0 && x < size - 1 && y > 0 && y < size - 1 && z > 0 && z < size - 1;
}
