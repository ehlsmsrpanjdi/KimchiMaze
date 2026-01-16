using UnityEngine;

using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
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

    private void PurchaseRequest(ShopItemData item)
    { 
        //여기에 통신 로직 ㄱㄱ
    }

}
