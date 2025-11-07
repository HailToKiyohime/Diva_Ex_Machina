using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 單一合成插槽資料
[System.Serializable]
public class CraftingSlot
{
    // 在預覽武器上實際組裝出的零件實體 (例如 Receiver / Scope / Barrel 的 GameObject)
    public GameObject assembledPart;
    public Transform attachmentPointTransform;
    // 此插槽允許的武器零件類型
    public WeaponPartType equipmentType;
    // 這個插槽目前使用中的背包物品資料
    public ItemInstance item;
}

public class CraftingManager : MonoBehaviour
{
    // 左側「合成零件插槽按鈕」們的父物件 (Receiver / Scope / Barrel)
    public Transform craftingPartsButtonParent;
    // 右側「背包物品按鈕」們的父物件
    public Transform itemsButtonParent;
    // 左側插槽的 ToggleGroup，用來判斷目前有沒有選中插槽
    public ToggleGroup craftingPartsToggleGroup;
    // 武器預覽的父節點
    public Transform weaponPreviewTransform;
    // 目前場景中的武器預覽實體
    public GameObject weaponPreview;
    // 目前正在預覽 / 組裝中的武器資料 (ScriptableObject)
    public RangeWeapon rangeWeapon;
    // 記錄每一個合成插槽的狀態
    [SerializeField] public List<CraftingSlot> craftingSlots = new();

    [Header("UI Button Prefab")]
    // 右側背包物品按鈕的預置物
    public GameObject buttonPrefab;

    // 給 UI 按鈕用的簡化呼叫：打開 Receiver 清單
    public void OpenReceiverInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Gun);
    // 打開 Scope 清單
    public void OpenScopeInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Scope);
    // 打開 Barrel 清單
    public void OpenBarrelInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Barrel);

    // 打開整把武器清單
    public void OpenRangeWeaponInventory() => OpenWeaponInventory(ItemType.RangeWeapon);

    // 打開「指定 ItemType」的背包清單 (目前主要給 RangeWeapon 用)
    public void OpenWeaponInventory(ItemType itemType)
    {
        // 必須先選中一個合成插槽 (ToggleGroup 有東西被選到)
        if (craftingPartsToggleGroup.AnyTogglesOn())
        {
            // 先清掉右側所有舊的物品按鈕
            ClearInventoryButton();

            // 找出目前選中的插槽 index
            int slotIndex = GetSelectedSlotIndex();

            // 先把所有合成插槽上的「Remove Equipment Button」關掉
            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
            {
                var removeBtn = InventoryManager.Instance.FindChild(
                    craftingPartsButtonParent.GetChild(i).gameObject,
                    "Remove Equipment Button"
                );
                if (removeBtn != null)
                    removeBtn.SetActive(false);
            }

            // 檢查現在選到的插槽是否已經有裝東西
            bool slotHasEquipment = false;
            if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
            {
                slotHasEquipment = craftingSlots[slotIndex].item.item != null;
            }

            // 掃描背包裡的所有物品
            foreach (var inv in InventoryManager.Instance.inventory)
            {
                // 空格、沒 item、或類型不符就略過
                if (inv == null || inv.item == null || inv.item.type != itemType)
                    continue;

                // 產生一個 UI 按鈕到右側清單
                var button = Instantiate(buttonPrefab, itemsButtonParent);

                // 設定圖示
                var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = inv.item.icon;

                // 設定名稱文字
                var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                if (label != null)
                    label.text = inv.item.itemName;

                // 取得 Toggle 組件
                var btn = button.GetComponent<Toggle>();
                if (btn != null)
                {
                    // 若目前插槽已經有裝備，打開該插槽的移除按鈕
                    if (slotHasEquipment && slotIndex >= 0)
                    {
                        var removeBtn = InventoryManager.Instance.FindChild(
                            craftingPartsButtonParent.GetChild(slotIndex).gameObject,
                            "Remove Equipment Button"
                        );
                        if (removeBtn != null)
                            removeBtn.SetActive(true);
                    }

                    // 用 local 變數捕捉，避免閉包都指向最後一個 inv
                    ItemInstance capturedItem = inv;
                    btn.onValueChanged.AddListener(isOn =>
                    {
                        // 只處理勾選為 true 的瞬間
                        if (!isOn) return;
                        OnClickInventoryItem(capturedItem, btn);
                    });

                    // 如果這個物品已經被放在某個 CraftingSlot，就讓這顆按鈕不能再按
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

    // 打開「武器零件」的背包清單，會再用 weaponPartType 過濾
    public void OpenRangeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        // 一樣必須先選中一個合成插槽
        if (craftingPartsToggleGroup.AnyTogglesOn())
        {
            // 清掉右側舊的按鈕
            ClearInventoryButton();

            // 找出目前選中的插槽 index
            int slotIndex = GetSelectedSlotIndex();

            // 先關閉所有插槽上的移除按鈕
            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
            {
                var removeBtn = InventoryManager.Instance.FindChild(
                    craftingPartsButtonParent.GetChild(i).gameObject,
                    "Remove Equipment Button"
                );
                if (removeBtn != null)
                    removeBtn.SetActive(false);
            }

            // 檢查現在選中的插槽是否已經有裝備
            bool slotHasEquipment = false;
            if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
            {
                Debug.Log(craftingSlots[slotIndex].item.item);
                slotHasEquipment = craftingSlots[slotIndex].item.item != null;
            }

            // 掃描背包
            foreach (var inv in InventoryManager.Instance.inventory)
            {
                // 基本過濾：空 / 無物品 / 類型不對
                if (inv == null || inv.item == null || inv.item.type != itemType)
                    continue;

                // 若要找的是 WeaponPart，再做進一步類型過濾
                if (itemType == ItemType.WeaponPart)
                {
                    if (inv.item is RangeWeaponPart rangeWeaponPart)
                    {
                        // 零件的 partType 不符合指定的 weaponPartType 就略過
                        if (rangeWeaponPart.partType != weaponPartType)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // 不是 RangeWeaponPart，直接略過
                        continue;
                    }
                }

                // 建立 UI 按鈕
                var button = Instantiate(buttonPrefab, itemsButtonParent);

                // 設定圖示
                var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = inv.item.icon;

                // 設定名稱
                var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                if (label != null)
                    label.text = inv.item.itemName;

                // Toggle 行為
                var btn = button.GetComponent<Toggle>();
                if (btn != null)
                {
                    // 插槽已經有裝備時，顯示該插槽的移除按鈕
                    if (slotHasEquipment && slotIndex >= 0)
                    {
                        var removeBtn = InventoryManager.Instance.FindChild(
                            craftingPartsButtonParent.GetChild(slotIndex).gameObject,
                            "Remove Equipment Button"
                        );
                        if (removeBtn != null)
                            removeBtn.SetActive(true);
                        removeBtn.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            removePart();
                            OpenRangeWeaponPartsInventory(itemType, weaponPartType);
                        });
                    }

                    // 捕捉當前 inv
                    ItemInstance capturedItem = inv;
                    btn.onValueChanged.AddListener(isOn =>
                    {
                        if (!isOn) return;
                        OnClickInventoryItem(capturedItem, btn);
                    });

                    // 物品已經在某個插槽使用中就鎖定按鈕
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

    // 右側「背包物品按鈕」被勾選時的處理
    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        // 若點的是整把 RangeWeapon
        if (item.item is RangeWeapon rw)
        {
            // 刪掉舊的武器預覽
            if (weaponPreview != null)
            {
                Destroy(weaponPreview);
            }

            // 產生新的武器預覽並掛在 weaponPreviewTransform 底下
            GameObject weapon = Instantiate(rw.weaponPrefab, weaponPreviewTransform);
            weaponPreview = weapon;
            rangeWeapon = rw;

            craftingSlots.Add(new CraftingSlot
            {
                assembledPart = weapon,
                attachmentPointTransform = null,
                equipmentType = WeaponPartType.Gun,
                item = item
            });

            foreach (var at in rw.attachmentPoints)
            {
                string slotName = at.pointTransform.name;
                craftingSlots.Add(new CraftingSlot
                {
                    assembledPart = null,
                    attachmentPointTransform = FindChildRecursive(weaponPreview.transform, slotName),
                    equipmentType = at.allowPart,
                    item = null
                });

            }

        }
        // 若點的是武器零件 RangeWeaponPart
        else if (item.item is RangeWeaponPart rwp)
        {
            // 沒有武器資料或沒有預覽實體就無法裝零件
            if (rangeWeapon == null || weaponPreview == null)
                return;

            // 逐一檢查這把武器定義好的所有掛點
            foreach (var attachmentPoint in craftingSlots)
            {
                // 零件類型不相符，或掛點沒指定 Transform，直接略過
                if (rwp.partType != attachmentPoint.equipmentType || attachmentPoint.attachmentPointTransform == null)
                    continue;

                // 在掛點底下實體化零件 prefab，產生組裝效果
                GameObject part = Instantiate(rwp.rangeWeaponPartPrefab, attachmentPoint.attachmentPointTransform);
                attachmentPoint.assembledPart = part;

                // 記錄這個插槽目前使用的物品
                attachmentPoint.item = item;

            }
        }
        btn.interactable = false;
    }

    // 清空右側背包按鈕列表
    public void ClearInventoryButton()
    {
        for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(itemsButtonParent.GetChild(i).gameObject);
    }

    // 從左側合成插槽中找出目前被勾選的插槽 index
    public int GetSelectedSlotIndex()
    {
        for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
        {
            var child = craftingPartsButtonParent.GetChild(i);
            // 用 GetComponentInChildren(true)，即使子物件預設是關閉也會找到 Toggle
            var t = child.GetComponentInChildren<Toggle>(true);
            if (t != null && t.isOn)
                return i;
        }
        // -1 代表沒有任何插槽被選中
        return -1;
    }

    // 遞迴尋找指定名稱的子物件
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

    public void removePart()
    {
        int index = GetSelectedSlotIndex();
        if (index >= 0 && index < craftingSlots.Count)
        {
            var slot = craftingSlots[index];
            if (slot.assembledPart != null)
            {
                Destroy(slot.assembledPart);
                slot.assembledPart = null;
                slot.item = null;
            }
        }

    }
}
