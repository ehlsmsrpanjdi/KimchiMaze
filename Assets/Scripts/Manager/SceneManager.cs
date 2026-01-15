using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager
{
    
    private static SceneManager instance;

    public static SceneManager Instance
    {
        get 
        {
            if (instance == null)
            {
                instance = new SceneManager();
            }
            return instance;
        }
    }
    
    [SerializeField]
    private string GameSceneName = "GameScene";
    [SerializeField]
    private string LobbySceneName = "LobbyScene";





    [SerializeField]
    private bool isPurchasing;

    private int CountToday;
    private bool ADBonus; //±¤°í 

 
    public void GiveAdBonusToday()
    {
        if (ADBonus)
        {
            return;
        }
    }

    public void ToLobbyScene() => UnityEngine.SceneManagement.SceneManager.LoadScene(LobbySceneName);
    

    
    
    public DateManager.EnterResult TryToGameScene()
    {
        var result = DateManager.Instance.TryConsumeGameEntry();

        if (result == DateManager.EnterResult.Success)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameSceneName);
        }

        return result;
        
    }


    private void Reset()
    { 
        
    }




}
