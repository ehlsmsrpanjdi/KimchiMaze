using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : MonoBehaviour
{
    [SerializeField]
    private string GameSceneName = "GameScene";
    [SerializeField]
    private string LobbySceneName = "LobbyScene";





    [SerializeField]
    private bool isPurchasing;

    private int CountToday;
    private bool ADBonus; //±¤°í 

  

    private void Awake()
    {
        
    }

    public void GiveAdBonusToday()
    {
        if (ADBonus)
        {
            return;
        }
    }

    public void ToLobbyScene() => UnityEngine.SceneManagement.SceneManager.LoadScene(LobbySceneName);
    

    
    /*
    public EnterResult TryToGameScene()
    { 
        
    }
     */


    private void Reset()
    { 
        
    }




}
