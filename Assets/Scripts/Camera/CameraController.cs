using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float heightOffset = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float followSpeed = 5f;

    public enum ViewDirection { Front, Right, Back, Left, Top, Bottom }

    // 수평 4방향 기준 (0=Front,1=Right,2=Back,3=Left)
    private int baseYaw = 0;

    // 수직 4단계 (Side -> Top -> OppositeSide -> Bottom -> Side)
    private enum VerticalState { Side = 0, Top = 1, OppositeSide = 2, Bottom = 3 }

    private VerticalState currentState = VerticalState.Side;
    private VerticalState targetState = VerticalState.Side;

    private ViewDirection currentDirection = ViewDirection.Front;
    private ViewDirection targetDirection = ViewDirection.Front;

    private float rotationProgress = 1f; // 0~1, 1이면 회전 완료

    public ViewDirection CurrentDirection => currentDirection;

    void Update()
    {
        HandleRotationInput();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 회전 진행
        if (rotationProgress < 1f)
        {
            rotationProgress += rotationSpeed * Time.deltaTime;
            if (rotationProgress >= 1f)
            {
                rotationProgress = 1f;
                currentState = targetState;
                currentDirection = targetDirection;
                NotifyCameraRotation();
            }
        }

        Vector3 currentOffset = GetOffsetByDirection(currentDirection, baseYaw);
        Vector3 targetOffset = GetOffsetByDirection(targetDirection, baseYaw);

        Vector3 smoothOffset = Vector3.Lerp(currentOffset, targetOffset, rotationProgress);

        Vector3 targetPos = target.position + smoothOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        // Up vector도 부드럽게 전환
        Vector3 currentUp = GetStableUpVector(currentDirection, baseYaw);
        Vector3 targetUp = GetStableUpVector(targetDirection, baseYaw);
        Vector3 smoothUp = Vector3.Slerp(currentUp, targetUp, rotationProgress).normalized;

        Vector3 forward = (target.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(forward, smoothUp);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
    }

    void HandleRotationInput()
    {
        if (rotationProgress < 1f) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) RotateHorizontal(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) RotateHorizontal(1);
        else if (Input.GetKeyDown(KeyCode.UpArrow)) RotateVertical(+1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) RotateVertical(-1);
    }

    // 좌/우: 항상 yaw를 누적 (Top/Bottom 상태에서도 yaw를 유지해두면 복귀 시 일관됨)
    void RotateHorizontal(int dir)
    {
        baseYaw = (baseYaw + dir + 4) % 4;

        // 현재(또는 목표) 수직 상태는 유지한 채, 방향만 재계산
        targetDirection = DirectionFromState(targetState, baseYaw);
        rotationProgress = 0f;
    }

    // 상/하: 4단계 수직 사이클
    // Up: Side->Top->Opposite->Bottom->Side
    // Down: 반대
    void RotateVertical(int dir)
    {
        int s = (int)currentState;
        if (dir > 0) s = (s + 1) % 4;
        else s = (s + 3) % 4; // -1을 모듈로 처리

        targetState = (VerticalState)s;
        targetDirection = DirectionFromState(targetState, baseYaw);
        rotationProgress = 0f;
    }

    ViewDirection DirectionFromState(VerticalState state, int yaw)
    {
        if (state == VerticalState.Top) return ViewDirection.Top;
        if (state == VerticalState.Bottom) return ViewDirection.Bottom;

        int y = yaw;
        if (state == VerticalState.OppositeSide) y = (yaw + 2) % 4;

        return y switch
        {
            0 => ViewDirection.Front,
            1 => ViewDirection.Right,
            2 => ViewDirection.Back,
            _ => ViewDirection.Left,
        };
    }

    Vector3 GetOffsetByDirection(ViewDirection dir, int yaw)
    {
        // Side(Front/Right/Back/Left)는 기존 방식 유지
        // Top/Bottom은 위/아래에 고정. (yaw는 up 벡터 안정화에만 사용)
        switch (dir)
        {
            case ViewDirection.Front: return new Vector3(0, heightOffset, -distance);
            case ViewDirection.Back: return new Vector3(0, heightOffset, distance);
            case ViewDirection.Left: return new Vector3(-distance, heightOffset, 0);
            case ViewDirection.Right: return new Vector3(distance, heightOffset, 0);
            case ViewDirection.Top: return new Vector3(0, distance, 0);
            case ViewDirection.Bottom: return new Vector3(0, -distance, 0);
            default: return new Vector3(0, heightOffset, -distance);
        }
    }

    Vector3 GetStableUpVector(ViewDirection dir, int yaw)
    {
        // Top/Bottom에서 LookAt만 쓰면 카메라가 롤로 뒤집히기 쉬움.
        // yaw 기준으로 "화면 위"를 고정해줌.
        if (dir == ViewDirection.Top || dir == ViewDirection.Bottom)
        {
            return yaw switch
            {
                0 => Vector3.forward,
                1 => Vector3.left,
                2 => Vector3.back,
                _ => Vector3.right,
            };
        }

        return Vector3.up;
    }

    void NotifyCameraRotation()
    {
        var player = target?.GetComponent<PlayerController>();
        if (player != null)
        {
            // CubeVisibilityManager 쪽 시그니처에 맞춰 구현되어 있어야 합니다.
            CubeVisibilityManager.Instance.OnCameraRotated(currentDirection, player.GridPosition);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            transform.position = target.position + GetOffsetByDirection(currentDirection, baseYaw);
            transform.LookAt(target.position);
        }
    }
}
