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
    public Transform weaponPreviewTransform;
    public GameObject weaponPreview;
    public RangeWeapon rangeWeapon;
    [SerializeField] public List<CraftingSlot> craftingSlots = new();

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;

    public void OpenReceiverInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Receiver);
    public void OpenScopeInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Scope);
    public void OpenBarrelInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Barrel);

    public void OpenRangeWeaponInventory() => OpenWeaponInventory(ItemType.RangeWeapon);
    public void OpenWeaponInventory(ItemType itemType)
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
            if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
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
    public void OpenRangeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
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
            if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
            {
                slotHasEquipment = craftingSlots[slotIndex].item != null;
            }
            // 開始生成符合這個類型的物品按鈕
            foreach (var inv in InventoryManager.Instance.inventory)
            {
                // 沒物品或型別不對就跳過
                if (inv == null || inv.item == null || inv.item.type != itemType)
                    continue;

                // 如果是武器零件，還要檢查子類型
                if (itemType == ItemType.WeaponPart)
                {
                    if (inv.item is RangeWeaponPart rangeWeaponPart)
                    {
                        // 不同類型就略過
                        if (rangeWeaponPart.partType != weaponPartType)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // 不是 RangeWeaponPart 也略過
                        continue;
                    }
                }
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
        if (item.item is RangeWeapon rw)
        {
            // 先清掉舊的預覽比較安全
            if (weaponPreview != null)
            {
                Destroy(weaponPreview);
            }
            GameObject weapon = Instantiate(rw.weaponPrefab, weaponPreviewTransform);
            weaponPreview = weapon;
            rangeWeapon = rw;
        }
        else if (item.item is RangeWeaponPart rwp)
        {
            if (rangeWeapon == null || weaponPreview == null)
                return;

            foreach (var attachmentPoint in rangeWeapon.attachmentPoints)
            {
                if (rwp.partType != attachmentPoint.allowPart || attachmentPoint.pointTransform == null)
                    continue;

                // 用 ScriptableObject 裡存的 Transform 名字，去場景的 weaponPreview 上找
                string slotName = attachmentPoint.pointTransform.name;
                Transform parentInScene = FindChildRecursive(weaponPreview.transform, slotName);

                if (parentInScene == null)
                {
                    Debug.LogWarning($"找不到掛點 {slotName}，請確認 weaponPrefab 內有同名子物件。");
                    continue;
                }

                Instantiate(rwp.rangeWeaponPartPrefab, parentInScene);
            }
        }
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

    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
