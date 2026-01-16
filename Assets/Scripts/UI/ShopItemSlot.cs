using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


public enum ItemType
{ 
    Block,
    Character
}

[Serializable]

public class ShopItemData
{
    public int id;
    public string name;
    public int price;
    public ItemType type;
}

[Serializable]
public class ShopItemWrapper
{
    public List<ShopItemData> items;
}

public class ShopItemSlot : MonoBehaviour
{

    [SerializeField]
    private Button SlotClickButton;
    [SerializeField]
    private TMP_Text nameTxt;

    private ShopItemData data;
    private Action<ShopItemData> onClick;

    public void Bind(ShopItemData item, Action<ShopItemData> onClickCallBack)
    {
        data = item;
        onClick = onClickCallBack;

        nameTxt.text = item.name;


        SlotClickButton.onClick.RemoveAllListeners();
        SlotClickButton.onClick.AddListener(() => onClick?.Invoke(data));
        
    }
}
