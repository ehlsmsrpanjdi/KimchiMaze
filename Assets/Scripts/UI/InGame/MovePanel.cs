using UnityEngine;
using UnityEngine.UI;

public class MovePanel : MonoBehaviour
{
    [SerializeField] Button UpBtn;
    [SerializeField] Button DownBtn;
    [SerializeField] Button LeftBtn;
    [SerializeField] Button RightBtn;

    private void Reset()
    {
        UpBtn = this.TryFindChild("Btn_Up").GetComponent<Button>();
        DownBtn = this.TryFindChild("Btn_Down").GetComponent<Button>();
        LeftBtn = this.TryFindChild("Btn_Left").GetComponent<Button>();
        RightBtn = this.TryFindChild("Btn_Right").GetComponent<Button>();
    }

    private void Start()
    {
        UpBtn.onClick.AddListener(OnClickUP);
        DownBtn.onClick.AddListener(OnClickDown);
        LeftBtn.onClick.AddListener(OnClickLeft);
        RightBtn.onClick.AddListener(OnClickRight);
    }

    void OnClickUP()
    {
        Transform cam = PlayerController.Instance.cameraController.transform;

        Vector3 Dir = cam.up;

        PlayerController.Instance.HandleInput(Dir);
    }

    void OnClickDown()
    {
        Transform cam = PlayerController.Instance.cameraController.transform;

        Vector3 Dir = -cam.up;

        PlayerController.Instance.HandleInput(Dir);
    }

    void OnClickLeft()
    {
        Transform cam = PlayerController.Instance.cameraController.transform;

        Vector3 Dir = -cam.right;

        PlayerController.Instance.HandleInput(Dir);
    }

    void OnClickRight()
    {
        Transform cam = PlayerController.Instance.cameraController.transform;

        Vector3 Dir = cam.right;

        PlayerController.Instance.HandleInput(Dir);
    }

}
