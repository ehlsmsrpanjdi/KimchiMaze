using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeButton : MonoBehaviour
{
    public void ClickToLobby()
    {
        // 내부적으로 싱글톤 인스턴스의 함수를 대신 호출해줍니다.
        SceneManager.Instance.ToLobbyScene();
    }

    public void ClickToGame()
    {
        SceneManager.Instance.ToLobbyScene();
    }
}
