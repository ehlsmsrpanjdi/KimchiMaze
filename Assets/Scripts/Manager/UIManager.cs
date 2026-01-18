using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private PopupUI popUpPrefab;
    [SerializeField]
    private Transform windowRoot;
    [SerializeField]
    private ShopWindow shopWindow;

    [SerializeField]
    private TMP_Text curGold;

    [SerializeField]
    private TMP_Text curDia;

    [SerializeField]
    private Button AdButton;

    private Dictionary<string, GameObject> opened = new();

    private const string ShopKey = "ShopWindow";

    private void Reset()
    {
      
        shopWindow = this.TryFindChild("ShopWindow").GetComponent<ShopWindow>();
        curGold = this.TryFindChild("GoldText").GetComponent<TMP_Text>();
    }

    private void Start()
    {
        AdButton.onClick.RemoveAllListeners();
        AdButton.onClick.AddListener(OnClickAdButton);

    }

    private void RefreshAdButtonState()
    {
        bool isAlreadyWatched = DateManager.Instance.IsAdBonusUsed();

        AdButton.interactable = !isAlreadyWatched; //버튼 비활성화
    }

    public void OpenShop()
    {
        if (opened.ContainsKey(ShopKey))
        {
            return;
        }

        List<ShopItemData> LoadItems = ShopDataLoader.GetItems();

        var instance = Instantiate(shopWindow, windowRoot);

        instance.Init(this, LoadItems);

        instance.ItemsBuy += ShowPurchaseSelected;
        opened[ShopKey] = instance.gameObject;
    }

    public void CloseShop()
    {
        if (!opened.TryGetValue(ShopKey, out var go))
            return;

        Destroy(go);
        opened.Remove(ShopKey);
    }

    public void ShowPopup(string title, string message, params (string btnText, Action onClick)[] options)
    {
        var popup = Instantiate(popUpPrefab, windowRoot);
        popup.Setting(title, message, options);

    }

    public void OnCliCkStartButton()
    {
        var result = SceneManager.Instance.TryToGameScene();

        if (result == DateManager.EnterResult.NeedAd)
        {
            ShowAdConfirm(() =>
            {
                DateManager.Instance.ApplyAdBonus();
                RefreshAdButtonState();
                SceneManager.Instance.TryToGameScene();


            });
        }
        else if (result == DateManager.EnterResult.NeedPurchase)
        {
            ShowSystemAlert("오늘 입장 횟수 초과");
        }
        else if (result == DateManager.EnterResult.NoMore)
        {
            ShowSystemAlert("내일 다시 와주세요");
        }

    }

    private void OnClickAdButton()
    {
        ShowAdConfirm(() =>
        {
            DateManager.Instance.ApplyAdBonus();
            RefreshAdButtonState();
            ShowSystemAlert("광고 보너스가 제공됨");


        });
    }




    #region 팝업 텍스트

    private void ShowPurchaseSelected(ShopItemData item)
    {
        ShowPopup(
            "Sell Option",
            $"Would you like to purchase {item.name}?",
            ($"{item.price} G", () => PurchaseWithGold(item)),
            ("Dia", () => PurchaseWithDia(item)),
            ("Cancel", null)
            );

    }

    private void ShowAdConfirm(Action onAdComplete = null)
    {
        ShowPopup(
            "View AD",
            "Would you like to see the ad????",
            ("Yes", () => RequestAdWatch()),
            ("Cancel", null)


            );
    }

    public void ShowSystemAlert(string msg)
    {
        ShowPopup("알림", msg, ("확인", null));
    }

    #endregion


    public void UpdateGoldText(int amount)
    {
        curGold.text = amount.ToString();
    }

    public void UpdateDiaText(int amount)
    {
        curDia.text = amount.ToString();
    }

//이벤트 정리

private void PurchaseWithGold(ShopItemData item)
{
    Debug.Log($"[시스템] {item.price} 골드로 구매");
}

private void PurchaseWithDia(ShopItemData item)
{
    Debug.Log($"[시스템] {item.price} 다이아로 구매");
}

private void RequestAdWatch()
{
    Debug.Log($"[시스템]광고 재생");
    //여기 광고 
}
private void PurchaseRequest(ShopItemData item)
{
    //여기에 통신 로직 ㄱㄱ
}

}
