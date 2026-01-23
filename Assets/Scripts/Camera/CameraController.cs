using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float focusHeight = 1.0f;
    [SerializeField] private float rotationSpeed = 6f;

    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f; // 최소 스와이프 거리(픽셀)

    public enum ViewDirection { Front, Right, Back, Left, Top, Bottom }

    private int yawIndex = 0;
    private int targetYawIndex = 0;
    private int pitchIndex = 0;
    private int targetPitchIndex = 0;

    private float t = 1f;
    private Quaternion startRot;
    private Quaternion endRot;
    private Vector3 startOffset;
    private Vector3 endOffset;

    public bool IsRotating => t < 1f;
    public ViewDirection CurrentDirection { get; private set; } = ViewDirection.Front;

    // Input System
    private Vector2 touchStartPos;
    private bool isTouching = false;

    void Start()
    {
        ApplyInstant();
    }

    void Update()
    {
        HandleSwipeInput();

#if UNITY_EDITOR
        HandleKeyboardInput(); // 에디터에서 테스트용
#endif
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (t < 1f)
        {
            t += rotationSpeed * Time.deltaTime;
            if (t >= 1f)
            {
                t = 1f;
                yawIndex = targetYawIndex;
                pitchIndex = targetPitchIndex;
                UpdateCurrentDirection();
                NotifyCameraRotation();
            }
        }

        Vector3 focus = target.position + Vector3.up * focusHeight;
        Quaternion rot = Quaternion.Slerp(startRot, endRot, t);
        Vector3 offset = Vector3.Lerp(startOffset, endOffset, t);

        transform.position = focus + offset;
        transform.rotation = rot;
    }

    void HandleSwipeInput()
    {
        if (IsRotating) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        // 터치/마우스 시작
        if (pointer.press.wasPressedThisFrame)
        {
            touchStartPos = pointer.position.ReadValue();
            isTouching = true;
        }
        // 터치/마우스 끝
        else if (pointer.press.wasReleasedThisFrame && isTouching)
        {
            Vector2 touchEndPos = pointer.position.ReadValue();
            DetectSwipe(touchStartPos, touchEndPos);
            isTouching = false;
        }
    }

    void DetectSwipe(Vector2 startPos, Vector2 endPos)
    {
        Vector2 swipeDelta = endPos - startPos;

        if (swipeDelta.magnitude < minSwipeDistance) return;

        bool changed = false;

        // 가로/세로 중 더 큰 방향으로 판단
        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            // 좌우 스와이프
            if (swipeDelta.x > 0) // 오른쪽으로 스와이프 → 왼쪽 키 효과
            {
                targetYawIndex = (yawIndex + 1) % 4;
                targetPitchIndex = pitchIndex;
                changed = true;
            }
            else // 왼쪽으로 스와이프 → 오른쪽 키 효과
            {
                targetYawIndex = (yawIndex + 3) % 4;
                targetPitchIndex = pitchIndex;
                changed = true;
            }
        }
        else
        {
            // 상하 스와이프 (방향 반대로 변경)
            if (swipeDelta.y > 0) // 위로 스와이프 → 아래 키 효과
            {
                targetYawIndex = yawIndex;
                targetPitchIndex = (pitchIndex + 3) % 4; // 변경됨
                changed = true;
            }
            else // 아래로 스와이프 → 위 키 효과
            {
                targetYawIndex = yawIndex;
                targetPitchIndex = (pitchIndex + 1) % 4; // 변경됨
                changed = true;
            }
        }

        if (changed)
        {
            BeginTransition();
        }
    }

#if UNITY_EDITOR
    void HandleKeyboardInput()
    {
        if (IsRotating) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool changed = false;

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            targetYawIndex = (yawIndex + 3) % 4;
            targetPitchIndex = pitchIndex;
            changed = true;
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            targetYawIndex = (yawIndex + 1) % 4;
            targetPitchIndex = pitchIndex;
            changed = true;
        }
        else if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            targetYawIndex = yawIndex;
            targetPitchIndex = (pitchIndex + 1) % 4;
            changed = true;
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            targetYawIndex = yawIndex;
            targetPitchIndex = (pitchIndex + 3) % 4;
            changed = true;
        }

        if (changed) BeginTransition();
    }
#endif

    void BeginTransition()
    {
        startRot = ComputeOrbitRotation(yawIndex, pitchIndex);
        startOffset = startRot * new Vector3(0f, 0f, -distance);

        endRot = ComputeOrbitRotation(targetYawIndex, targetPitchIndex);
        endOffset = endRot * new Vector3(0f, 0f, -distance);

        t = 0f;
    }

    Quaternion ComputeOrbitRotation(int yawI, int pitchI)
    {
        float yaw = yawI * 90f;
        float pitch = pitchI * 90f;

        Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);
        Quaternion pitchRot = Quaternion.AngleAxis(pitch, Vector3.right);

        return yawRot * pitchRot;
    }

    void UpdateCurrentDirection()
    {
        if (pitchIndex == 1) { CurrentDirection = ViewDirection.Top; return; }
        if (pitchIndex == 3) { CurrentDirection = ViewDirection.Bottom; return; }

        int y = yawIndex;
        if (pitchIndex == 2) y = (yawIndex + 2) % 4;

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