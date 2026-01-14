using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopWindow : MonoBehaviour
{
    [SerializeField]
    private Button closeButton;

    [Header("Left Preview")]
    [SerializeField] private TMP_Text selectedNameText;
    //[SerializeField] private Sprite selectedItemSprite;

    [Header("Right List")]
    [SerializeField] private Transform contentRoot;     
    [SerializeField] private ShopItemSlot slotPrefab;  

    private UIManager ui;


    public void Init(UIManager uimanger)
    {
        ui = uimanger;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => ui.CloseShop());

        var list = CreateItem(20);


        SettingSlot(list);
        if (list.Count > 0)
        {
            OnSelectItem(list[0]);
        }

    }

    private void SettingSlot(List<ShopItemData> items)
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject); //일단 기존슬롯 제거 
        }

        foreach (var item in items)
        {
            var slot = Instantiate(slotPrefab, contentRoot);
            slot.Bind(item,OnSelectItem);
        }


    }


    private void OnSelectItem(ShopItemData data)
    {
        selectedNameText.text = data.name;
        //스프라이트는 아직 미구현 
    }


    private List<ShopItemData> CreateItem(int count) // 임시로 만듬
    {
        var list = new List<ShopItemData>();

        for (int i = 0; i <= count; i++)
        {
            list.Add(new ShopItemData
            {
                id = i,
                name = $"이아템 이름{i}",
                price = 100*i

            });
        }
        return list;


    }
}
