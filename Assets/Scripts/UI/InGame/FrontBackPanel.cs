using UnityEngine;
using UnityEngine.UI;

public class FrontBackPanel : MonoBehaviour
{
    [SerializeField] Button frontButton;
    [SerializeField] Button backButton;

    private void Reset()
    {
        frontButton = this.TryFindChild("Btn_Front").GetComponent<Button>();
        backButton = this.TryFindChild("Btn_Back").GetComponent<Button>();
    }

    private void Start()
    {
        frontButton.onClick.AddListener(MoveFront);
        backButton.onClick.AddListener(MoveBack);
    }

    void MoveFront()
    {
        Transform cam = PlayerController.Instance.cameraController.transform;

        Vector3 Dir = cam.forward;

        PlayerController.Instance.HandleInput(Dir);
    }

    void MoveBack()
    {
        Transform cam = PlayerController.Instance.cameraController.transform;

        Vector3 Dir = -cam.forward;

        PlayerController.Instance.HandleInput(Dir);
    }
}
