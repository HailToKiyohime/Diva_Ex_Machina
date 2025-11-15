using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ItemInstance
{
    public ItemObject item;
    public int amount;
}

[System.Serializable]
public class RangeWeaponInstance : ItemInstance
{
    public string newWeaponName; // 用來存鍛造後的新名稱
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<PartInstance> attachment;
    public Transform muzzlePoint;

    [SerializeField] public List<Color> colors = new List<Color>();
    [SerializeField] public string shaderName;
}

[System.Serializable]
public class ArmorInstance : ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();

    [SerializeField] public List<Color> colors = new List<Color>();
    [SerializeField] public string shaderName;
}

[System.Serializable]
public class PartInstance : ItemInstance
{
    public WeaponPartType partType;
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();

    [SerializeField] public List<Color> colors = new List<Color>();
    [SerializeField] public string shaderName;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Slot")]
    [SerializeReference] public List<ItemInstance> inventory = new();

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;
    public Transform itemsButtonParent;     // 放物品按鈕
    public Transform inventoryButtonParent; // 裝備欄按鈕

    [Header("Color Block/ Inventory Block")]
    public GameObject inventoryBlock;
    public GameObject characterColorBlock;
    public GameObject statBlock;

    [Header("Color Panel")]
    public ColorPicker characterEquipmentColorPicker;

    [Header("Button Toggle Group")]
    public ToggleGroup inventoryToggleGroup;

    private GameObject currentPage;
    [SerializeField] private UIPageSwitch pageSwitch;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ===== 小工具：顏色、名稱、UI 共用 =====

    // 根據 shader 讀顏色資料
    private void FillColorFromRenderer(Renderer renderer, out string shaderName, List<Color> colors)
    {
        shaderName = null;
        if (colors == null) return;
        colors.Clear();

        if (!renderer || !renderer.sharedMaterial) return;

        var mat = renderer.sharedMaterial;
        var shader = mat.shader;
        if (shader == null) return;

        shaderName = shader.name;

        if (shader.name.Contains("Mix 3"))
        {
            colors.Add(mat.GetColor("_BaseColor"));
            colors.Add(mat.GetColor("_Layer1Color"));
            colors.Add(mat.GetColor("_Layer2Color"));
        }
        else if (shader.name.Contains("Mix 4"))
        {
            colors.Add(mat.GetColor("_BaseColor"));
            colors.Add(mat.GetColor("_Layer1Color"));
            colors.Add(mat.GetColor("_Layer2Color"));
            colors.Add(mat.GetColor("_Layer3Color"));
        }
        else if (shader.name.Contains("Mix 5"))
        {
            colors.Add(mat.GetColor("_BaseColor"));
            for (int i = 1; i < 5; i++)
                colors.Add(mat.GetColor($"_Layer{i}Color"));
        }
    }

    // 顯示名稱（有 newWeaponName 就優先）
    private string GetDisplayName(ItemInstance inv)
    {
        if (inv is RangeWeaponInstance rwi && !string.IsNullOrEmpty(rwi.newWeaponName))
            return rwi.newWeaponName;
        return inv.item.itemName;
    }

    // 某裝備槽是否已有裝備
    private bool EquipmentSlotHasItem(int slotIndex)
    {
        if (slotIndex < 0 ||
            EquipmentManager.Instance == null ||
            slotIndex >= EquipmentManager.Instance.equipmentSlots.Count)
            return false;

        return EquipmentManager.Instance.equipmentSlots[slotIndex].equipedItem != null;
    }

    // 把所有裝備槽的 Remove Button 全部開/關
    private void ToggleAllRemoveButtonsOnInventorySlots(bool active)
    {
        for (int i = 0; i < inventoryButtonParent.childCount; i++)
        {
            var removeBtn = FindChild(inventoryButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
            if (removeBtn != null)
                removeBtn.SetActive(active);
        }
    }

    // 單一槽的 Remove Button 顯示
    private void ShowRemoveButtonForSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventoryButtonParent.childCount) return;
        var removeBtn = FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
        if (removeBtn != null)
            removeBtn.SetActive(true);
    }

    // 單一槽的 Remove Button 關閉
    private void HideRemoveButtonForSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventoryButtonParent.childCount) return;
        var removeBtn = FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
        if (removeBtn != null)
            removeBtn.SetActive(false);
    }

    // 更新裝備槽 icon（sprite = null 表示清除）
    private void UpdateSlotIcon(int slotIndex, Sprite sprite)
    {
        if (slotIndex < 0 || slotIndex >= inventoryButtonParent.childCount) return;

        var slotGo = inventoryButtonParent.GetChild(slotIndex).gameObject;
        var img = FindChild(slotGo, "Item Icon")?.GetComponent<Image>();
        if (img == null) return;

        if (sprite == null)
        {
            img.sprite = null;
            img.color = new Color(1, 1, 1, 0);
        }
        else
        {
            img.sprite = sprite;
            img.color = new Color(1, 1, 1, 1);
        }
    }

    // 解鎖同一清單中的其他按鈕
    private void UnlockOtherItemButtons(Toggle current)
    {
        foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
        {
            if (t != current)
            {
                t.interactable = true;
                t.isOn = false;
            }
        }
    }

    // 重置角色裝備顏色面板
    private void ResetCharacterColorPicker()
    {
        characterEquipmentColorPicker.targetGameObject = null;
        characterEquipmentColorPicker.targetMaterials = new List<Material>();
        characterEquipmentColorPicker.currentMaterialIndex = -1;
        characterEquipmentColorPicker.currentTextureIndex = -1;
        characterEquipmentColorPicker.ClearnButton();
    }

    // 建立右側「物品按鈕」（角色裝備用）
    private void CreateInventoryButtonForEquipment(ItemInstance inv, int slotIndex, bool slotHasEquipment)
    {
        var button = Instantiate(buttonPrefab, itemsButtonParent);

        var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
        if (icon != null)
            icon.sprite = inv.item.icon;

        var label = button.transform.Find("Item Name")?.GetComponent<TMP_Text>();
        if (label != null)
            label.text = GetDisplayName(inv);

        var btn = button.GetComponent<Toggle>();
        if (btn == null) return;

        // 如果這個槽本來就有裝備，就把那個槽的 Remove Button 打開
        if (slotHasEquipment && slotIndex >= 0)
            ShowRemoveButtonForSlot(slotIndex);

        ItemInstance capturedItem = inv;
        btn.onValueChanged.AddListener(isOn =>
        {
            if (!isOn) return;
            OnClickInventoryItem(capturedItem, btn);
        });

        // 已經裝在任意裝備槽上的物品鎖定
        foreach (var slot in EquipmentManager.Instance.equipmentSlots)
        {
            if (slot != null && capturedItem == slot.item)
            {
                btn.interactable = false;
                break;
            }
        }
    }

    // ===== 加入背包 =====

    public void AddItemToInventory(ItemObject item)
    {
        if (item is Armor a)
        {
            var inst = new ArmorInstance { item = item, amount = 1 };

            if (a.skinnedMeshRenderer)
                FillColorFromRenderer(a.skinnedMeshRenderer, out inst.shaderName, inst.colors);

            foreach (EquipmentBuff buff in a.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = a.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
        }
        else if (item is RangeWeapon rw)
        {
            var attachmentPoints = new List<PartInstance>();
            foreach (var part in rw.attachmentPoints)
            {
                attachmentPoints.Add(
                    new PartInstance
                    {
                        item = null,
                        amount = 0,
                        partType = part.allowPart
                    }
                );
            }

            var inst = new RangeWeaponInstance
            {
                item = item,
                amount = 1,
                attachment = attachmentPoints,
                muzzlePoint = rw.GetMuzzlePoint()
            };

            if (rw.meshRenderer)
                FillColorFromRenderer(rw.meshRenderer, out inst.shaderName, inst.colors);

            foreach (EquipmentBuff buff in rw.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = rw.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
        }
        else if (item is RangeWeaponPart rwp)
        {
            var inst = new PartInstance
            {
                item = item,
                amount = 1,
                partType = rwp.partType
            };

            if (rwp.meshRenderer)
                FillColorFromRenderer(rwp.meshRenderer, out inst.shaderName, inst.colors);

            foreach (EquipmentBuff buff in rwp.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = rwp.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
        }
        else
        {
            var inst = new ItemInstance
            {
                item = item,
                amount = 1,
            };
            inventory.Add(inst);
        }
    }

    public void RemoveItemFromInventory(ItemInstance itemInstance)
    {
        if (itemInstance == null) return;

        int idx = inventory.IndexOf(itemInstance);
        if (idx >= 0)
            inventory.RemoveAt(idx);
    }

    public void AddCraftedRangeWeaponToInventory(
        RangeWeaponInstance baseWeaponInstance,
        List<PartInstance> rangeWeaponParts
    )
    {
        if (baseWeaponInstance == null)
        {
            Debug.LogWarning("AddCraftedRangeWeaponToInventory: baseWeaponInstance is null");
            return;
        }

        var newInst = new RangeWeaponInstance
        {
            item = baseWeaponInstance.item,
            amount = 1,
            newWeaponName = baseWeaponInstance.newWeaponName,
            buffs = new List<EquipmentBuff>(baseWeaponInstance.buffs),
            attachment = new List<PartInstance>(),
            muzzlePoint = baseWeaponInstance.muzzlePoint,
            shaderName = baseWeaponInstance.shaderName,
            colors = (baseWeaponInstance.colors != null)
                ? new List<Color>(baseWeaponInstance.colors)
                : new List<Color>()
        };

        if (rangeWeaponParts != null)
        {
            foreach (var part in rangeWeaponParts)
            {
                if (part == null) continue;
                newInst.attachment.Add(part);
            }
        }

        inventory.Add(newInst);
        Debug.Log($"Forged new ranged weapon. Inventory count = {inventory.Count}");
    }

    // ===== 開啟各類型背包 =====

    public void OpenWeaponInventory() => OpenPartsInventory(ItemType.RangeWeapon);
    public void OpenHeadArmorInventory() => OpenPartsInventory(ItemType.HeadArmor);
    public void OpenChestArmorInventory() => OpenPartsInventory(ItemType.ChestArmor);
    public void OpenLeftHandInventory() => OpenPartsInventory(ItemType.LeftHandArmor);
    public void OpenRightHandInventory() => OpenPartsInventory(ItemType.RightHandArmor);
    public void OpenWaistArmorInventory() => OpenPartsInventory(ItemType.WaistArmor);
    public void OpenLegsArmorInventory() => OpenPartsInventory(ItemType.LegsArmor);
    public void OpenThrusterInventory() => OpenPartsInventory(ItemType.Thruster);
    public void OpenConsumableInventory() => OpenPartsInventory(ItemType.Consumable);
    public void OpenMaterialInventory() => OpenPartsInventory(ItemType.Material);

    public void OpenPartsInventory(ItemType itemType)
    {
        Debug.Log("colorBlock.activeSelf: " + characterColorBlock.activeSelf);

        // 顏色模式中：只處理顏色面板
        if (characterColorBlock.activeSelf)
        {
            if (!inventoryToggleGroup.AnyTogglesOn())
                ResetCharacterColorPicker();
            return;
        }

        // 沒有選任何裝備槽 → 顯示 Stat 頁，關閉 Inventory 頁與 Remove 按鈕
        if (!inventoryToggleGroup.AnyTogglesOn())
        {
            statBlock.SetActive(true);
            inventoryBlock.SetActive(false);
            ToggleAllRemoveButtonsOnInventorySlots(false);

            currentPage = statBlock;
            pageSwitch.pages[0] = currentPage;
            return;
        }

        // 有選裝備槽 → 顯示該類型背包列表
        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        ToggleAllRemoveButtonsOnInventorySlots(false);

        bool slotHasEquipment = EquipmentSlotHasItem(slotIndex);

        foreach (var inv in inventory)
        {
            if (inv == null || inv.item == null || inv.item.type != itemType)
                continue;

            CreateInventoryButtonForEquipment(inv, slotIndex, slotHasEquipment);
        }

        statBlock.SetActive(false);
        inventoryBlock.SetActive(true);
        currentPage = inventoryBlock;
        pageSwitch.pages[0] = currentPage;
    }

    public void ClearInventoryButton()
    {
        for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(itemsButtonParent.GetChild(i).gameObject);
    }

    // 右側物品按鈕被點時
    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        Debug.Log("Working");

        int slotIndex = GetSelectedSlotIndex();
        if (slotIndex < 0)
        {
            Debug.LogWarning("OnClickInventoryItem: no equipment slot selected");
            btn.isOn = false;
            return;
        }

        // Range Weapon（雙手武器）
        if (item is RangeWeaponInstance rw)
        {
            var slotButton = inventoryButtonParent.GetChild(slotIndex);
            var weaponTransform = slotButton.GetComponentInChildren<WeaponTransform>();
            if (weaponTransform == null || weaponTransform.weaponTransform == null)
            {
                Debug.LogWarning($"OnClickInventoryItem (weapon): WeaponTransform missing on slot {slotIndex}");
                btn.isOn = false;
                return;
            }

            Transform mountPoint = weaponTransform.weaponTransform;

            if (!EquipmentManager.Instance.TryEquipWeaponFromInventory(item, mountPoint, slotIndex))
            {
                btn.isOn = false;
                return;
            }

            UnlockOtherItemButtons(btn);
            UpdateSlotIcon(slotIndex, item.item.icon);
            ShowRemoveButtonForSlot(slotIndex);
            btn.interactable = false;
        }
        else
        {
            // Armor / 其他裝備
            if (!EquipmentManager.Instance.TryEquipFromInventory(item))
            {
                btn.isOn = false;
                return;
            }

            UnlockOtherItemButtons(btn);
            UpdateSlotIcon(slotIndex, item.item.icon);
            ShowRemoveButtonForSlot(slotIndex);
            btn.interactable = false;
        }
    }

    public void OpenInventoryPage()
    {
        int index = GetSelectedSlotIndex();
        var slots = EquipmentManager.Instance.equipmentSlots;
        if (index < 0 || index >= slots.Count)
        {
            Debug.LogWarning($"OpenInventoryPage: invalid slot index {index}");
            return;
        }

        OpenPartsInventory(slots[index].equipmentType);
    }

    public void Unequip()
    {
        int index = GetSelectedSlotIndex();
        if (index < 0) return;

        EquipmentManager.Instance.CleanEquipmentSlot(index);

        HideRemoveButtonForSlot(index);
        UpdateSlotIcon(index, null);

        OpenInventoryPage();

        if (characterColorBlock.activeSelf)
            SelectPartToColor(index);
    }

    // ===== 顏色相關 =====

    public void OpenColorPage()
    {
        int index = GetSelectedSlotIndex();

        // 把所有裝備槽上的「Remove Equipment Button」先關掉
        ToggleAllRemoveButtonsOnInventorySlots(false);

        var slots = EquipmentManager.Instance.equipmentSlots;
        if (index < 0 || index >= slots.Count)
            return;
        if (!characterColorBlock.activeSelf) return;

        var go = slots[index].equipedItem;
        if (!go)
        {
            ResetCharacterColorPicker();
        }
        else
        {
            characterEquipmentColorPicker.targetGameObject = go;
            characterEquipmentColorPicker.AddTargetMaterialsToList();
            characterEquipmentColorPicker.CreateButtons();
        }
    }

    public void SelectPartToColor(int equipmentSlotsIndex)
    {
        if (!characterColorBlock.activeSelf)
            return;

        var slots = EquipmentManager.Instance.equipmentSlots;
        if (equipmentSlotsIndex < 0 || equipmentSlotsIndex >= slots.Count)
            return;

        var equipped = slots[equipmentSlotsIndex].equipedItem;
        if (equipped != null)
        {
            characterEquipmentColorPicker.targetGameObject = equipped;
            characterEquipmentColorPicker.AddTargetMaterialsToList();
            characterEquipmentColorPicker.CreateButtons();
        }
        else
        {
            ResetCharacterColorPicker();
        }
    }

    // ===== slot / child 搜尋 =====

    public int GetSelectedSlotIndex()
    {
        for (int i = 0; i < inventoryButtonParent.childCount; i++)
        {
            var child = inventoryButtonParent.GetChild(i);
            var t = child.GetComponentInChildren<Toggle>(true);
            if (t != null && t.isOn)
                return i;
        }
        return -1;
    }

    public GameObject FindChild(GameObject parentObject, string targetName)
    {
        foreach (Transform childTransform in parentObject.transform)
        {
            if (childTransform.gameObject.name == targetName)
                return childTransform.gameObject;

            GameObject found = FindChild(childTransform.gameObject, targetName);
            if (found != null)
                return found;
        }
        return null;
    }
}
