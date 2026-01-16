using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class ShopWindow : MonoBehaviour
{
    public event Action<ShopItemData> ItemsBuy;

    [SerializeField]
    private Button closeButton;

    [Header("Category Tabs")]
    [SerializeField] private Button blockTabBtn;
    [SerializeField] private Button characterTabBtn;

    [Header("Left Preview")]
    [SerializeField] private TMP_Text selectedNameText;
    //[SerializeField] private Sprite selectedItemSprite;

    [Header("Right List")]
    [SerializeField] private Transform contentRoot;     
    [SerializeField] private ShopItemSlot slotPrefab;


    private UIManager ui;
    private Dictionary<ItemType, List<ShopItemData>> allItemDic = new();


    public void Init(UIManager uimanger , List<ShopItemData> loadItems)
    {
        ui = uimanger;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => ui.CloseShop());

        blockTabBtn.onClick.AddListener(() =>
        {
            OnTabSelected(ItemType.Block);
            UpdateButtonStates(ItemType.Block);
            
        });
        characterTabBtn.onClick.AddListener(() =>
        {

            OnTabSelected(ItemType.Character);
            UpdateButtonStates(ItemType.Character);
        });

        CreateItem(loadItems);




             

        /*
         보여주기
        var list = CreateItem(20);


        SettingSlot(list);
        if (list.Count > 0)
        {
            OnSelectItem(list[0]);
        }
         */

    }

    private void CreateItem(List<ShopItemData> items)
    {
        allItemDic.Clear();

        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            allItemDic[type] = new List<ShopItemData>();
        
        }

        foreach (var item in items)
        {
            if (allItemDic.ContainsKey(item.type))
            {
                allItemDic[item.type].Add(item);
            }
        }
        
    }



    private void OnTabSelected(ItemType type) 
    {
        if (allItemDic.TryGetValue(type, out var shopItemDatas))
        {
            SettingSlot(shopItemDatas);

            if (shopItemDatas.Count > 0)
            { 
                OnSelectItem(shopItemDatas[0]);
            }
        }
    }

    private void SettingSlot(List<ShopItemData> items)
    {
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
            //최적화는 나중에
        }

        foreach (var item in items)
        {
            var slot = Instantiate(slotPrefab, contentRoot);
            slot.Bind(item, (data) => {
                                OnSelectItem(data);
                ItemsBuy?.Invoke(data); //구매했을때

                });
        }


    }


    private void OnSelectItem(ShopItemData data)
    {
        selectedNameText.text = data.name;
        //스프라이트는 아직 미구현 
    }

    private void UpdateButtonStates(ItemType type)
    {
        blockTabBtn.interactable = (type != ItemType.Block);
        characterTabBtn.interactable = (type != ItemType.Character);
    }

   
}
