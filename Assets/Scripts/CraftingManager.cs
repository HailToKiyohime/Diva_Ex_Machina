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
    // 左側合成插槽按鈕的預置物
    public GameObject craftingSlotPrefab;
    public Sprite barrelIcon;
    public Sprite scopeIcon;

    public UIPageSwitch uiPageSwitch;

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
        AssignRemovePartButtonListener();

        if (craftingPartsToggleGroup.AnyTogglesOn())
        {
            ClearInventoryButton();

            int slotIndex = GetSelectedSlotIndex();

            // 先把所有合成插槽上的「Remove Equipment Button」關掉
            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
            {
                var removeBtnGo = InventoryManager.Instance.FindChild(
                    craftingPartsButtonParent.GetChild(i).gameObject,
                    "Remove Equipment Button"
                );
                if (removeBtnGo != null)
                    removeBtnGo.SetActive(false);
            }

            // 檢查現在選到的插槽是否已經有裝東西
            bool slotHasEquipment = false;
            if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
            {
                var slot = craftingSlots[slotIndex];
                if (slot != null && slot.item != null && slot.item.item != null)
                    slotHasEquipment = true;
            }

            // 掃描背包
            foreach (var inv in InventoryManager.Instance.inventory)
            {
                if (inv == null || inv.item == null || inv.item.type != itemType)
                    continue;

                var button = Instantiate(buttonPrefab, itemsButtonParent);

                var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = inv.item.icon;

                var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                if (label != null)
                    label.text = inv.item.itemName;

                var btn = button.GetComponent<Toggle>();
                if (btn != null)
                {
                    // 插槽已有裝備 → 顯示對應的移除按鈕
                    if (slotHasEquipment &&
                        slotIndex >= 0 &&
                        slotIndex < craftingPartsButtonParent.childCount)
                    {
                        var removeBtnGo = InventoryManager.Instance.FindChild(
                            craftingPartsButtonParent.GetChild(slotIndex).gameObject,
                            "Remove Equipment Button"
                        );
                        if (removeBtnGo != null)
                            removeBtnGo.SetActive(true);
                    }

                    ItemInstance capturedItem = inv;
                    btn.onValueChanged.AddListener(isOn =>
                    {
                        if (!isOn) return;
                        OnClickInventoryItem(capturedItem, btn);
                    });

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
        else
        {
            // 沒選插槽就關掉所有 Remove 按鈕
            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
            {
                var removeBtnGo = InventoryManager.Instance.FindChild(
                    craftingPartsButtonParent.GetChild(i).gameObject,
                    "Remove Equipment Button"
                );
                if (removeBtnGo != null)
                    removeBtnGo.SetActive(false);
            }
        }
    }

    // 打開「武器零件」的背包清單，會再用 weaponPartType 過濾
    public void OpenRangeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        if (!craftingPartsToggleGroup.AnyTogglesOn())
            return;

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();

        // 先關閉所有插槽上的移除按鈕
        for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
        {
            var removeBtnGo = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(i).gameObject,
                "Remove Equipment Button"
            );
            if (removeBtnGo != null)
                removeBtnGo.SetActive(false);
        }

        // 檢查現在選中的插槽是否已經有裝備
        bool slotHasEquipment = false;
        if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
        {
            var slot = craftingSlots[slotIndex];
            if (slot != null && slot.item != null && slot.item.item != null)
                slotHasEquipment = true;
        }

        // 掃描背包
        foreach (var inv in InventoryManager.Instance.inventory)
        {
            if (inv == null || inv.item == null || inv.item.type != itemType)
                continue;

            if (itemType == ItemType.WeaponPart)
            {
                if (inv.item is RangeWeaponPart rangeWeaponPart)
                {
                    if (rangeWeaponPart.partType != weaponPartType)
                        continue;
                }
                else
                {
                    continue;
                }
            }

            var button = Instantiate(buttonPrefab, itemsButtonParent);

            var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
            if (icon != null)
                icon.sprite = inv.item.icon;

            var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
            if (label != null)
                label.text = inv.item.itemName;

            var btn = button.GetComponent<Toggle>();
            if (btn != null)
            {
                // 插槽已有裝備 → 顯示 Remove 按鈕（listener 在 AssignRemovePartButtonListener 統一處理）
                if (slotHasEquipment &&
                    slotIndex >= 0 &&
                    slotIndex < craftingPartsButtonParent.childCount)
                {
                    var removeBtnGo = InventoryManager.Instance.FindChild(
                        craftingPartsButtonParent.GetChild(slotIndex).gameObject,
                        "Remove Equipment Button"
                    );
                    if (removeBtnGo != null)
                        removeBtnGo.SetActive(true);
                }

                ItemInstance capturedItem = inv;
                btn.onValueChanged.AddListener(isOn =>
                {
                    if (!isOn) return;
                    OnClickInventoryItem(capturedItem, btn);
                });

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

    // 右側「背包物品按鈕」被勾選時的處理
    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        // 整把武器
        if (item.item is RangeWeapon rw)
        {
            if (weaponPreview != null)
                Destroy(weaponPreview);

            // 換新武器時先清掉舊的 craftingSlots
            craftingSlots.Clear();

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
            CreateCraftingSlots();
        }
        // 武器零件
        else if (item.item is RangeWeaponPart rwp)
        {
            if (rangeWeapon == null || weaponPreview == null)
                return;

            foreach (var attachmentPoint in craftingSlots)
            {
                if (rwp.partType != attachmentPoint.equipmentType ||
                    attachmentPoint.attachmentPointTransform == null)
                    continue;

                GameObject part = Instantiate(rwp.rangeWeaponPartPrefab, attachmentPoint.attachmentPointTransform);
                attachmentPoint.assembledPart = part;
                attachmentPoint.item = item;
            }
        }

        // 更新 Remove 按鈕顯示
        int slotIndex = GetSelectedSlotIndex();
        if (slotIndex >= 0 &&
            slotIndex < craftingSlots.Count &&
            slotIndex < craftingPartsButtonParent.childCount)
        {
            var slot = craftingSlots[slotIndex];
            bool slotHasEquipment = (slot != null && slot.item != null && slot.item.item != null);

            if (slotHasEquipment)
            {
                var removeBtnGo = InventoryManager.Instance.FindChild(
                    craftingPartsButtonParent.GetChild(slotIndex).gameObject,
                    "Remove Equipment Button"
                );
                if (removeBtnGo != null)
                    removeBtnGo.SetActive(true);
            }
        }

        // 這個物品已被使用，不允許再點
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
        if (index < 0 || index >= craftingSlots.Count)
            return;

        if (index == 0) // 移除整把武器
        {
            foreach (var slot in craftingSlots)
            {
                if (slot.assembledPart != null)
                    Destroy(slot.assembledPart);
            }
            craftingSlots.Clear();
            weaponPreview = null;
            rangeWeapon = null;
            OpenRangeWeaponInventory();
        }
        else
        {
            var slot = craftingSlots[index];
            if (slot.assembledPart != null)
                Destroy(slot.assembledPart);
            slot.assembledPart = null;
            slot.item = null;
            OpenRangeWeaponPartsInventory(ItemType.WeaponPart, slot.equipmentType);
        }
    }
    public void AssignRemovePartButtonListener()
    {
        foreach (Transform slotButton in craftingPartsButtonParent)
        {
            var removeBtnGo = InventoryManager.Instance.FindChild(
                slotButton.gameObject,
                "Remove Equipment Button"
            );
            if (removeBtnGo != null)
            {
                var button = removeBtnGo.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(removePart);
                }
            }
        }
    }
    public void CreateCraftingSlots()
    {
        if (craftingSlots.Count > 1)
        {
            for (int i = 1; i < craftingSlots.Count; i++)
            {
                var slot = craftingSlots[i];
                var slotButton = Instantiate(craftingSlotPrefab, craftingPartsButtonParent);
                Image icon = FindChildRecursive(slotButton.transform, "Slot Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    Debug.Log("Setting icon for slot: " + slot.equipmentType);
                    if (slot.equipmentType == WeaponPartType.Barrel)
                        icon.sprite = barrelIcon;
                    else if (slot.equipmentType == WeaponPartType.Scope)
                        icon.sprite = scopeIcon;
                }
                var btn = slotButton.GetComponent<Toggle>();
                if (btn != null)
                {
                    btn.group = craftingPartsToggleGroup;
                    uiPageSwitch.toggles.Add(btn);
                    btn.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn)
                        {
                            // 當這個插槽被選中時，打開對應的背包清單
                            OpenRangeWeaponPartsInventory(ItemType.WeaponPart, slot.equipmentType);
                        }
                    });
                }
            }
            uiPageSwitch.UpdateToggles();
        }
    }

}
