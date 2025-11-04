using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingSlot {
    public GameObject assembledPart;
    public WeaponPartType equipmentType;
    public ItemInstance item;
}


public class CraftingManager : MonoBehaviour
{
    public Transform craftingPartsButtonParent;// 放按鈕的容器
    public Transform itemsButtonParent;// 放按鈕的容器
    public ToggleGroup craftingPartsToggleGroup;

    [SerializeField] public List<CraftingSlot> craftingSlots = new();

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;

    public void OpenWeaponPartInventory() => OpenPartsInventory(ItemType.WeaponPart);
    public void OpenPartsInventory(ItemType itemType)
    {
        if (craftingPartsToggleGroup.AnyTogglesOn())
        {
            // 先清空右邊的物品列表
            ClearInventoryButton();
            // 先記住目前選到哪個裝備槽
            int slotIndex = GetSelectedSlotIndex();

            // 把所有裝備槽上的「Remove Equipment Button」先關掉
            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
            {
                var removeBtn = InventoryManager.Instance.FindChild(craftingPartsButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
                if (removeBtn != null)
                    removeBtn.SetActive(false);
            }
            // 檢查這個槽現在是不是有裝東西
            bool slotHasEquipment = false;
            if (slotIndex >= 0 && slotIndex< craftingSlots.Count)
            {
                slotHasEquipment = craftingSlots[slotIndex].item != null;
            }
            // 開始生成符合這個類型的物品按鈕
            foreach (var inv in InventoryManager.Instance.inventory)
            {
                // 沒物品或型別不對就跳過
                if (inv == null || inv.item == null || inv.item.type != itemType)
                    continue;
                // 生成按鈕
                var button = Instantiate(buttonPrefab, itemsButtonParent);

                // 圖示
                var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = inv.item.icon;

                // 名稱
                var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                if (label != null)
                    label.text = inv.item.itemName;

                // Toggle
                var btn = button.GetComponent<Toggle>();
                if (btn != null)
                {
                    // 如果這個槽本來就有裝備，就把那個槽的 Remove Button 打開
                    if (slotHasEquipment && slotIndex >= 0)
                    {
                        var removeBtn = InventoryManager.Instance.FindChild(craftingPartsButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
                        if (removeBtn != null)
                            removeBtn.SetActive(true);
                    }
                    // 為了避免閉包問題，做一個本地變數
                    ItemInstance capturedItem = inv;
                    btn.onValueChanged.AddListener(isOn =>
                    {
                        if (!isOn) return;
                        OnClickInventoryItem(capturedItem, btn);
                    });
                    // 如果這個物品已經裝在任一個槽上，就把它鎖住
                    foreach (var slot in craftingSlots)
                    {
                        if (slot != null && capturedItem == slot.item)
                        {
                            btn.interactable = false;
                            break;
                        }
                    }
                }
            }
        }
    }

    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {

    }

    public void ClearInventoryButton()
    {
        for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(itemsButtonParent.GetChild(i).gameObject);
    }
    public int GetSelectedSlotIndex()
    {
        for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
        {
            var child = craftingPartsButtonParent.GetChild(i);
            var t = child.GetComponentInChildren<Toggle>(true); // 允許巢狀/隱藏
            if (t != null && t.isOn)
                return i;
        }
        return -1;
    }
}
