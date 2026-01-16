using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUI : MonoBehaviour
{


    [SerializeField]
    private TMP_Text titleTxt;
    [SerializeField]
    private TMP_Text messageTxt;
    [SerializeField]
    private TMP_Text[] buttonsTexts;
    [SerializeField]
    private Button[] buttons;


    [SerializeField]
    private Button buttonPrefab;
    [SerializeField]
    private Transform buttonParent;
    

    public void Setting(string title, string message, params (string btnText, Action onClick)[] options) //params 인자값 갯수 상관없이 받는다 자주안써본 문법이었음
    {
        titleTxt.text = title;
        messageTxt.text = message;

        foreach (Transform child in buttonParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 전달받은 옵션 개수만큼 버튼 생성
        for (int i = 0; i < options.Length; i++)
        {
            Button newBtn = Instantiate(buttonPrefab, buttonParent);

          
            TMP_Text btnTxt = newBtn.GetComponentInChildren<TMP_Text>();
            if (btnTxt != null) btnTxt.text = options[i].btnText;

           
            Action callback = options[i].onClick;
            newBtn.onClick.RemoveAllListeners();
            newBtn.onClick.AddListener(() =>
            {
                callback?.Invoke();
                Destroy(gameObject); 
            });
        }

    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
