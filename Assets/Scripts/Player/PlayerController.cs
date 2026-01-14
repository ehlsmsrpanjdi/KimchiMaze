using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector3Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;

    private bool[,,] maze;
    private int mazeSize;

    private CameraController cameraController; // 추가

    public Vector3Int GridPosition => gridPosition;
    public event Action<Vector3Int> OnPositionChanged;

    public void Initialize(Vector3Int startPos, bool[,,] mazeData, int size)
    {
        gridPosition = startPos;
        maze = mazeData;
        mazeSize = size;

        targetWorldPosition = GridToWorld(gridPosition);
        transform.position = targetWorldPosition;

        // 카메라 컨트롤러 찾기
        cameraController = Camera.main.GetComponent<CameraController>();

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
        }
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
}