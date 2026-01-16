using UnityEngine;
using System;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private PopupUI popUpPrefab;
    [SerializeField]
    private Transform windowRoot;
    [SerializeField]
    private ShopWindow shopWindow;

    private Dictionary<string, GameObject> opened = new();

    private const string ShopKey = "ShopWindow";


    public void OpenShop()
    {
        if (opened.ContainsKey(ShopKey))
        { 
            return;
        }

        List<ShopItemData> LoadItems = ShopDataLoader.GetItems();

        var instance = Instantiate(shopWindow, windowRoot);

        instance.Init(this,LoadItems);
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

    private void TestPurchaseClick(ShopItemData item)
    {
        ShowPopup("구매 확인", $"{item.name}을(를) {item.price}골드에 구매하시겠습니까?",
            ("구매", () => PurchaseRequest(item)), // '구매' 클릭 시 실행
            ("취소", null) // '취소' 클릭 시 그냥 닫힘
        );
    }

    private void PurchaseRequest(ShopItemData item)
    { 
        //여기에 통신 로직 ㄱㄱ
    }

}
