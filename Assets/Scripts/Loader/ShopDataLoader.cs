using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Build.Content;

public static class ShopDataLoader
{
    private const string JsonPath = "ItemList/ItemList.json";

    public static List<ShopItemData> GetItems()
    {
        TextAsset json = Resources.Load<TextAsset>(JsonPath);

        if(json == null)
        {
            return new List<ShopItemData>();
        }

        ShopItemWrapper wrapper = JsonUtility.FromJson<ShopItemWrapper>(json.text);
        if (wrapper != null && wrapper.items != null)
        {
            return wrapper.items;
        }
        else
        {
            Debug.Log("[ShopDataLoader] 后 府胶飘 积己");
            return new List<ShopItemData>();
        }
    }


}