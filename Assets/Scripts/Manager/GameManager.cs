using UnityEngine;

public class GameManager : MonoBehaviour
{
    DateManager dateMangaer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        dateMangaer = DateManager.Instance;
        dateMangaer.init(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
