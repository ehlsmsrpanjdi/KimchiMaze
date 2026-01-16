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
    private Button buyBtn;
    [SerializeField]
    private TMP_Text nameTxt;
    [SerializeField]
    private TMP_Text priceTxt;

    private ShopItemData data;
    private Action<ShopItemData> onClick;

    public void Bind(ShopItemData item, Action<ShopItemData> onClickCallBack)
    {
        data = item;
        onClick = onClickCallBack;

        nameTxt.text = item.name;
        priceTxt.text = item.price.ToString();

        buyBtn.onClick.RemoveAllListeners();
        buyBtn.onClick.AddListener(() => onClick?.Invoke(data)); //바인드 해서 사용 아이템 뭔지 모르니까 일단
    }
}
