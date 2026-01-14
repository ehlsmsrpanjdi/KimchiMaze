using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float focusHeight = 1.0f;   // 플레이어 바라보는 높이(머리쪽)
    [SerializeField] private float rotationSpeed = 6f;    // 0~1 진행 속도(클수록 빠름)

    public enum ViewDirection { Front, Right, Back, Left, Top, Bottom }

    // 수평 4방향 (0=Front,1=Right,2=Back,3=Left)
    private int yawIndex = 0;
    private int targetYawIndex = 0;

    // 수직 4단계 (0=Side,1=Top,2=Opposite(뒤집힘),3=Bottom)
    private int pitchIndex = 0;
    private int targetPitchIndex = 0;

    private float t = 1f;                 // 0~1
    private Quaternion startRot;
    private Quaternion endRot;

    private Vector3 startOffset;
    private Vector3 endOffset;

    public bool IsRotating => t < 1f;

    // 기존 코드 호환용
    public ViewDirection CurrentDirection { get; private set; } = ViewDirection.Front;

    void Start()
    {
        // 초기 상태를 현재 트랜스폼 기준으로 맞추고 싶으면 여기서 복원 로직을 추가할 수 있음.
        // 지금은 (Front, Side) 시작.
        ApplyInstant();
    }

    void Update()
    {
        HandleRotationInput();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 진행
        if (t < 1f)
        {
            t += rotationSpeed * Time.deltaTime;
            if (t >= 1f)
            {
                t = 1f;

                yawIndex = targetYawIndex;
                pitchIndex = targetPitchIndex;
                UpdateCurrentDirection();

                // 회전 완료 시점 이벤트가 필요하면 여기서 호출
                NotifyCameraRotation();
            }
        }

        Vector3 focus = target.position + Vector3.up * focusHeight;

        // 회전/위치 보간(직접 만든 회전만 사용, LookRotation 없음)
        Quaternion rot = Quaternion.Slerp(startRot, endRot, t);
        Vector3 offset = Vector3.Lerp(startOffset, endOffset, t);

        transform.position = focus + offset;
        transform.rotation = rot;
    }

    void HandleRotationInput()
    {
        if (IsRotating) return;

        bool changed = false;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            targetYawIndex = (yawIndex + 3) % 4;
            targetPitchIndex = pitchIndex;
            changed = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            targetYawIndex = (yawIndex + 1) % 4;
            targetPitchIndex = pitchIndex;
            changed = true;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            targetYawIndex = yawIndex;
            targetPitchIndex = (pitchIndex + 1) % 4; // Side->Top->Opposite->Bottom->Side
            changed = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            targetYawIndex = yawIndex;
            targetPitchIndex = (pitchIndex + 3) % 4; // 역방향
            changed = true;
        }

        if (!changed) return;

        BeginTransition();
    }

    void BeginTransition()
    {
        // 시작 상태(현재 인덱스) 회전/오프셋
        startRot = ComputeOrbitRotation(yawIndex, pitchIndex);
        startOffset = startRot * new Vector3(0f, 0f, -distance);

        // 목표 상태 회전/오프셋
        endRot = ComputeOrbitRotation(targetYawIndex, targetPitchIndex);
        endOffset = endRot * new Vector3(0f, 0f, -distance);

        // 진행 시작
        t = 0f;
    }

    // "진짜 공전 회전": yaw는 월드 Y축, pitch는 월드 X축 기준으로 먼저 적용 후 yaw
    // 이 회전 자체가 카메라의 forward/up/right를 결정하므로 플립이 생길 여지가 줄어듭니다.
    Quaternion ComputeOrbitRotation(int yawI, int pitchI)
    {
        float yaw = yawI * 90f;
        float pitch = pitchI * 90f;

        Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);
        Quaternion pitchRot = Quaternion.AngleAxis(pitch, Vector3.right);

        // pitch 먼저, 그 다음 yaw
        return yawRot * pitchRot;
    }

    void UpdateCurrentDirection()
    {
        if (pitchIndex == 1) { CurrentDirection = ViewDirection.Top; return; }
        if (pitchIndex == 3) { CurrentDirection = ViewDirection.Bottom; return; }

        // Side(0) 또는 Opposite(2)
        int y = yawIndex;
        if (pitchIndex == 2) y = (yawIndex + 2) % 4; // 반대면

        CurrentDirection = y switch
        {
            0 => ViewDirection.Front,
            1 => ViewDirection.Left,
            2 => ViewDirection.Back,
            _ => ViewDirection.Right,
        };
    }

    void ApplyInstant()
    {
        targetYawIndex = yawIndex;
        targetPitchIndex = pitchIndex;

        startRot = endRot = ComputeOrbitRotation(yawIndex, pitchIndex);
        startOffset = endOffset = startRot * new Vector3(0f, 0f, -distance);
        t = 1f;

        UpdateCurrentDirection();
    }

    void NotifyCameraRotation()
    {
        // 기존 연결 유지
        var player = target?.GetComponent<PlayerController>();
        if (player != null)
            CubeVisibilityManager.Instance.OnCameraRotated(CurrentDirection, player.GridPosition);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ApplyInstant();
    }
}
