using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#region Instances (keep in this file for compatibility)

[System.Serializable]
public class ItemInstance
{
    public ItemObject item;
    public int amount;
}

[System.Serializable]
public class RangeWeaponInstance : ItemInstance
{
    public string newWeaponName;                 // 用來存鍛造後的新名稱
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<PartInstance> attachment;
    public Transform muzzlePoint;

    // 用來存不同 shader 的顏色
    [SerializeField] public List<Color> colors = new List<Color>();

    // 記錄此裝備用的 shader 名稱，方便還原
    [SerializeField] public string shaderName;
}

[System.Serializable]
public class MeleeWeaponInstance : ItemInstance
{
    public string newWeaponName;                 // 用來存鍛造後的新名稱
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<PartInstance> attachment;

    // 用來存不同 shader 的顏色
    [SerializeField] public List<Color> colors = new List<Color>();

    // 記錄此裝備用的 shader 名稱，方便還原
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

#endregion

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Slot")]
    [SerializeReference] public List<ItemInstance> inventory = new();

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;
    public Transform itemsButtonParent;      // 右邊清單按鈕容器
    public Transform inventoryButtonParent;  // 左邊裝備槽按鈕容器
    [Header("UI Button Image")]
    public Sprite scopeIconImage;
    public Sprite barrelIconImage;
    public Sprite gunIconImage;

    [Header("Color Block/ Inventory Block")]
    public GameObject inventoryBlock;
    public GameObject characterColorBlock;
    public GameObject statBlock;

    [Header("Color Panel")]
    public ColorPicker characterEquipmentColorPicker;

    [Header("Button Toggle Group")]
    public ToggleGroup inventoryToggleGroup;         // 裝備槽 ToggleGroup
    public ToggleGroup WeaponPartsColorToggleGroup;  // 槍/零件 ToggleGroup

    [Header("Page Switch")]
    [SerializeField] private UIPageSwitch pageSwitch;
    private GameObject currentPage;

    [Header("Weapon Parts Buttons")]
    public Transform partsButtonParent;
    public GameObject partButtonPrefab;
    public Color selectedColor;
    public Color normalColor;

    // 紀錄右邊清單 Toggle 對應的物品
    private readonly Dictionary<Toggle, ItemInstance> toggleItemMap = new();

    private bool IsColorMode => characterColorBlock != null && characterColorBlock.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #region Public entry for Equipment Slot Toggles (IMPORTANT)

    // 主人：把每個「裝備槽 Toggle」的 OnValueChanged(bool) 只綁這個就好
    public void OnEquipmentSlotToggleChanged(bool isOn)
    {
        // ON：照舊，走「開背包」或「切上色目標」
        if (isOn)
        {
            if (IsColorMode)
            {
                OpenColorPage();
            }
            else
            {
                OpenInventoryPage();
            }
            return;
        }

        // OFF：如果目前已經沒有任何裝備槽被選中，就要把 UI 清乾淨
        bool anySlotSelected = inventoryToggleGroup != null && inventoryToggleGroup.AnyTogglesOn();
        if (anySlotSelected) return;

        SetAllRemoveButtonsActive(false);

        if (IsColorMode)
        {
            // Color Mode：清空零件按鈕 & ColorPicker 目標
            CleanPartButtons();
            ClearColorPickerState();
            return;
        }

        // Inventory Mode：清空右側清單，回到 Stat Page
        ClearInventoryButton();
        ShowStatPage();
    }

    #endregion

    #region Inventory CRUD

    public void AddItemToInventory(ItemObject item)
    {
        if (item == null) return;

        if (item is Armor a)
        {
            var inst = new ArmorInstance
            {
                item = item,
                amount = 1,
            };

            // 讀顏色
            if (a.skinnedMeshRenderer && a.skinnedMeshRenderer.sharedMaterial)
            {
                var mat = a.skinnedMeshRenderer.sharedMaterial;
                inst.shaderName = mat.shader.name;

                if (inst.shaderName.Contains("Mix 3"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                }
                else if (inst.shaderName.Contains("Mix 4"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                    inst.colors.Add(mat.GetColor("_Layer3Color"));
                }
                else if (inst.shaderName.Contains("Mix 5"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    for (int i = 1; i < 5; i++)
                        inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                }
            }

            foreach (EquipmentBuff buff in a.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = a.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
            return;
        }

        if (item is RangeWeapon rw)
        {
            var attachmentPoints = new List<PartInstance>();
            foreach (var ap in rw.attachmentPoints)
            {
                attachmentPoints.Add(new PartInstance
                {
                    item = null,
                    amount = 0,
                    partType = ap.allowPart
                });
            }

            var inst = new RangeWeaponInstance
            {
                item = item,
                amount = 1,
                attachment = attachmentPoints,
                muzzlePoint = rw.weaponPrefab != null ? rw.weaponPrefab.transform.Find("MuzzlePoint") : null,
            };

            // 讀顏色（修正：Mix5 也要把 BaseColor 放進來）
            if (rw.meshRenderer && rw.meshRenderer.sharedMaterial)
            {
                var mat = rw.meshRenderer.sharedMaterial;
                inst.shaderName = mat.shader.name;

                if (inst.shaderName.Contains("Mix 3"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                }
                else if (inst.shaderName.Contains("Mix 4"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                    inst.colors.Add(mat.GetColor("_Layer3Color"));
                }
                else if (inst.shaderName.Contains("Mix 5"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    for (int i = 1; i < 5; i++)
                        inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                }
            }

            foreach (EquipmentBuff buff in rw.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = rw.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
            return;
        }

        if (item is RangeWeaponPart rwp)
        {
            var inst = new PartInstance
            {
                item = item,
                amount = 1,
                partType = rwp.partType
            };

            if (rwp.meshRenderer && rwp.meshRenderer.sharedMaterial)
            {
                var mat = rwp.meshRenderer.sharedMaterial;
                inst.shaderName = mat.shader.name;

                if (inst.shaderName.Contains("Mix 3"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                }
                else if (inst.shaderName.Contains("Mix 4"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                    inst.colors.Add(mat.GetColor("_Layer3Color"));
                }
                else if (inst.shaderName.Contains("Mix 5"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    for (int i = 1; i < 5; i++)
                        inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                }
            }

            foreach (EquipmentBuff buff in rwp.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = rwp.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
            return;
        }

        if (item is MeleeWeapon mw)
        {
            var attachmentPoints = new List<PartInstance>();
            foreach (var ap in mw.attachmentPoints)
            {
                attachmentPoints.Add(new PartInstance
                {
                    item = null,
                    amount = 0,
                    partType = ap.allowPart
                });
            }

            var inst = new MeleeWeaponInstance
            {
                item = item,
                amount = 1,
                attachment = attachmentPoints,
            };

            // 讀顏色（修正：Mix5 也要把 BaseColor 放進來）
            if (mw.meshRenderer && mw.meshRenderer.sharedMaterial)
            {
                var mat = mw.meshRenderer.sharedMaterial;
                inst.shaderName = mat.shader.name;

                if (inst.shaderName.Contains("Mix 3"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                }
                else if (inst.shaderName.Contains("Mix 4"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                    inst.colors.Add(mat.GetColor("_Layer3Color"));
                }
                else if (inst.shaderName.Contains("Mix 5"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    for (int i = 1; i < 5; i++)
                        inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                }
            }

            foreach (EquipmentBuff buff in mw.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = mw.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
            return;
        }
        if (item is MeleeWeaponPart mwp)
        {
            var inst = new PartInstance
            {
                item = item,
                amount = 1,
                partType = mwp.partType
            };

            // 讀顏色（跟 RangeWeaponPart 同規格：Mix3/4/5）
            if (mwp.meshRenderer && mwp.meshRenderer.sharedMaterial)
            {
                var mat = mwp.meshRenderer.sharedMaterial;
                inst.shaderName = mat.shader.name;

                if (inst.shaderName.Contains("Mix 3"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                }
                else if (inst.shaderName.Contains("Mix 4"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                    inst.colors.Add(mat.GetColor("_Layer3Color"));
                }
                else if (inst.shaderName.Contains("Mix 5"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    for (int i = 1; i < 5; i++)
                        inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                }
            }

            // buffs + random buff
            foreach (EquipmentBuff buff in mwp.buffs)
                inst.buffs.Add(buff);

            var pickedBuff = mwp.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
            return;
        }
        inventory.Add(new ItemInstance { item = item, amount = 1 });
    }

    public void RemoveItemFromInventory(ItemInstance itemInstance)
    {
        if (itemInstance == null) return;
        int idx = inventory.IndexOf(itemInstance);
        if (idx >= 0) inventory.RemoveAt(idx);
    }

    public void AddCraftedRangeWeaponToInventory(RangeWeaponInstance baseWeaponInstance, List<PartInstance> rangeWeaponParts)
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
            colors = baseWeaponInstance.colors != null ? new List<Color>(baseWeaponInstance.colors) : new List<Color>()
        };

        if (rangeWeaponParts != null)
        {
            foreach (var part in rangeWeaponParts)
                if (part != null) newInst.attachment.Add(part);
        }

        inventory.Add(newInst);
    }

    public void AddCraftedMeleeWeaponToInventory(MeleeWeaponInstance baseWeaponInstance, List<PartInstance> meleeWeaponParts)
    {
        if (baseWeaponInstance == null)
        {
            Debug.LogWarning("AddCraftedMeleeWeaponToInventory: baseWeaponInstance is null");
            return;
        }

        var newInst = new MeleeWeaponInstance
        {
            item = baseWeaponInstance.item,
            amount = 1,
            newWeaponName = baseWeaponInstance.newWeaponName,
            buffs = new List<EquipmentBuff>(baseWeaponInstance.buffs),
            attachment = new List<PartInstance>(),
            shaderName = baseWeaponInstance.shaderName,
            colors = baseWeaponInstance.colors != null ? new List<UnityEngine.Color>(baseWeaponInstance.colors) : new List<UnityEngine.Color>()
        };

        if (meleeWeaponParts != null)
        {
            foreach (var part in meleeWeaponParts)
                if (part != null) newInst.attachment.Add(part);
        }

        inventory.Add(newInst);
    }


    #endregion

    #region Inventory Page Openers (keep for Inspector)

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

    #endregion

    #region Inventory List UI (right panel)

    public void OpenInventoryPage()
    {
        int index = GetSelectedSlotIndex();
        var slots = EquipmentManager.Instance.equipmentSlots;
        if (index < 0 || index >= slots.Count)
        {
            Debug.LogWarning($"OpenInventoryPage: invalid slot index {index}");
            ClearInventoryButton();
            ShowStatPage();
            SetAllRemoveButtonsActive(false);
            return;
        }
        OpenPartsInventory(slots[index].equipmentTypes);
    }

    public void OpenPartsInventory(ItemType itemType)
    {
        // Color mode 時：不要在這裡開 inventory 清單（避免裝備槽點一下又開清單）
        if (IsColorMode) return;

        bool anySlotSelected = inventoryToggleGroup != null && inventoryToggleGroup.AnyTogglesOn();
        if (!anySlotSelected)
        {
            ClearInventoryButton();
            ShowStatPage();
            SetAllRemoveButtonsActive(false);
            return;
        }

        ClearInventoryButton();
        int slotIndex = GetSelectedSlotIndex();

        SetAllRemoveButtonsActive(false);

        bool slotHasEquipment = false;
        if (slotIndex >= 0 &&
            EquipmentManager.Instance != null &&
            slotIndex < EquipmentManager.Instance.equipmentSlots.Count)
        {
            slotHasEquipment = EquipmentManager.Instance.equipmentSlots[slotIndex].equipedItem != null;
        }

        if (slotHasEquipment)
            SetRemoveButtonForSlot(slotIndex, true);

        foreach (var inv in inventory)
        {
            if (inv == null || inv.item == null) continue;
            if (inv.item.type != itemType) continue;

            var button = Instantiate(buttonPrefab, itemsButtonParent);

            // icon
            var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = inv.item.icon;

            // name
            var label = button.transform.Find("Item Name")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                if (inv is RangeWeaponInstance rwi && !string.IsNullOrEmpty(rwi.newWeaponName))
                    label.text = rwi.newWeaponName;
                else
                    label.text = inv.item.itemName;
            }

            var tgl = button.GetComponent<Toggle>();
            if (tgl == null) continue;

            ItemInstance capturedItem = inv;
            toggleItemMap[tgl] = capturedItem;

            // 先鎖住：已裝備中的物品
            tgl.interactable = !IsItemEquippedAnywhere(capturedItem);

            tgl.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;
                OnClickInventoryItem(capturedItem, tgl);
            });
        }

        ShowInventoryPage();
    }
    public void OpenPartsInventory(List<ItemType> allowedTypes)
    {
        if (IsColorMode) return;

        bool anySlotSelected = inventoryToggleGroup != null && inventoryToggleGroup.AnyTogglesOn();
        if (!anySlotSelected)
        {
            ClearInventoryButton();
            ShowStatPage();
            SetAllRemoveButtonsActive(false);
            return;
        }

        if (allowedTypes == null || allowedTypes.Count == 0)
        {
            ClearInventoryButton();
            ShowStatPage();
            SetAllRemoveButtonsActive(false);
            return;
        }

        ClearInventoryButton();
        int slotIndex = GetSelectedSlotIndex();

        SetAllRemoveButtonsActive(false);

        bool slotHasEquipment = false;
        if (slotIndex >= 0 &&
            EquipmentManager.Instance != null &&
            slotIndex < EquipmentManager.Instance.equipmentSlots.Count)
        {
            slotHasEquipment = EquipmentManager.Instance.equipmentSlots[slotIndex].equipedItem != null;
        }

        if (slotHasEquipment)
            SetRemoveButtonForSlot(slotIndex, true);

        // 用 HashSet 提升 Contains 效率
        var allowed = new HashSet<ItemType>(allowedTypes);

        foreach (var inv in inventory)
        {
            if (inv == null || inv.item == null) continue;
            if (!allowed.Contains(inv.item.type)) continue;

            var button = Instantiate(buttonPrefab, itemsButtonParent);

            var icon = button.transform.Find("Item Icon")?.GetComponent<UnityEngine.UI.Image>();
            if (icon != null) icon.sprite = inv.item.icon;

            var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
            if (label != null)
            {
                if (inv is RangeWeaponInstance rwi && !string.IsNullOrEmpty(rwi.newWeaponName))
                    label.text = rwi.newWeaponName;
                else
                    label.text = inv.item.itemName;
            }

            var tgl = button.GetComponent<UnityEngine.UI.Toggle>();
            if (tgl == null) continue;

            ItemInstance capturedItem = inv;
            toggleItemMap[tgl] = capturedItem;

            tgl.interactable = !IsItemEquippedAnywhere(capturedItem);

            tgl.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;
                OnClickInventoryItem(capturedItem, tgl);
            });
        }

        ShowInventoryPage();
    }
    public void ClearInventoryButton()
    {
        if (itemsButtonParent != null)
        {
            for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
                Destroy(itemsButtonParent.GetChild(i).gameObject);
        }

        toggleItemMap.Clear();
    }

    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        if (item == null || item.item == null)
        {
            btn.isOn = false;
            return;
        }

        // Weapon equip（需要 mountPoint）
        if (item is RangeWeaponInstance)
        {
            int slotIndex = GetSelectedSlotIndex();
            if (slotIndex < 0)
            {
                Debug.LogWarning("OnClickInventoryItem (weapon): no equipment slot selected");
                btn.isOn = false;
                return;
            }

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

            // UI 更新
            SetEquipmentSlotIcon(slotIndex, item.item.icon);
            SetRemoveButtonForSlot(slotIndex, true);

            RefreshItemListLockStates(btn);

            // 鎖住當前已裝備的按鈕
            btn.interactable = false;
            return;
        }
        if (item is MeleeWeaponInstance)
        {
            int slotIndex = GetSelectedSlotIndex();
            if (slotIndex < 0)
            {
                Debug.LogWarning("OnClickInventoryItem (weapon): no equipment slot selected");
                btn.isOn = false;
                return;
            }

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

            // UI 更新
            SetEquipmentSlotIcon(slotIndex, item.item.icon);
            SetRemoveButtonForSlot(slotIndex, true);

            RefreshItemListLockStates(btn);

            // 鎖住當前已裝備的按鈕
            btn.interactable = false;
            return;
        }
        // Armor / other equip
        if (!EquipmentManager.Instance.TryEquipFromInventory(item))
        {
            btn.isOn = false;
            return;
        }

        if (item.item.type == ItemType.LegsArmor)
        {
            BoneCombiner.Instance.HideLegs();
        }
        // UI 更新

        int idx = GetSelectedSlotIndex();
        if (idx >= 0)
        {
            SetEquipmentSlotIcon(idx, item.item.icon);
            SetRemoveButtonForSlot(idx, true);
        }

        RefreshItemListLockStates(btn);
        btn.interactable = false;
    }

    private void RefreshItemListLockStates(Toggle selectedBtn)
    {
        foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
        {
            if (t == null) continue;
            if (t == selectedBtn) continue;

            // 關掉其它按鈕的選取狀態
            t.isOn = false;

            if (!toggleItemMap.TryGetValue(t, out var mappedItem))
            {
                t.interactable = true;
                continue;
            }

            // 已裝備的物品維持鎖住
            t.interactable = !IsItemEquippedAnywhere(mappedItem);
        }
    }

    private bool IsItemEquippedAnywhere(ItemInstance item)
    {
        if (item == null) return false;
        foreach (var slot in EquipmentManager.Instance.equipmentSlots)
        {
            if (slot != null && slot.item == item)
                return true;
        }
        return false;
    }

    #endregion

    #region Unequip

    public void Unequip()
    {
        int index = GetSelectedSlotIndex();
        if (index < 0) return;

        var equippedType = EquipmentManager.Instance.equipmentSlots[index].item?.item != null
            ? EquipmentManager.Instance.equipmentSlots[index].item.item.type
            : (ItemType?)null;

        if (equippedType == ItemType.LegsArmor)
        {
            BoneCombiner.Instance.ShowLegs();
        }
        EquipmentManager.Instance.CleanEquipmentSlot(index);

        SetRemoveButtonForSlot(index, false);
        SetEquipmentSlotIcon(index, null);

        // 依模式刷新
        if (IsColorMode) OpenColorPage();
        else OpenInventoryPage();
    }

    #endregion

    #region Color Page (select equipment / weapon parts)

    public void OpenColorPage()
    {
        if (!IsColorMode) return;

        CleanPartButtons();
        SetAllRemoveButtonsActive(false);

        int index = GetSelectedSlotIndex();
        var slots = EquipmentManager.Instance.equipmentSlots;

        if (index < 0 || index >= slots.Count)
        {
            ClearColorPickerState();
            return;
        }

        var slot = slots[index];
        var equippedGO = slot.equipedItem;

        // 沒裝備：清空 ColorPicker
        if (equippedGO == null)
        {
            ClearColorPickerState();
            return;
        }

        // 預設先選本體（非武器也適用）
        SelectPartToColor(equippedGO, slot.item);

        // 只有 RangeWeapon 才生成零件按鈕
        if (slot.item is not RangeWeaponInstance rwi || rwi.item is not RangeWeapon rw)
            return;

        if (partsButtonParent == null || partButtonPrefab == null || WeaponPartsColorToggleGroup == null)
            return;

        // 建立按鈕：Gun 本體（預設選中）
        CreatePartToggle(gunIconImage, equippedGO, rwi, true);

        // 已裝上的 attachment 逐一建按鈕
        if (rwi.attachment == null) return;

        foreach (var attach in rwi.attachment)
        {
            if (attach?.item == null) continue;

            WeaponPartType partType = attach.partType;
            GameObject partGO = FindEquippedPartGO(equippedGO, rw, partType);
            Sprite icon = GetPartIcon(partType);

            CreatePartToggle(icon, partGO, attach, false);
        }
    }

    private Toggle CreatePartToggle(Sprite iconSprite, GameObject targetPart, ItemInstance instanceToSave, bool defaultOn)
    {
        var goBtn = Instantiate(partButtonPrefab, partsButtonParent);
        var tgl = goBtn.GetComponent<Toggle>();
        if (tgl == null) return null;

        tgl.group = WeaponPartsColorToggleGroup;

        var icon = goBtn.transform.Find("Part Icon")?.GetComponent<Image>();
        if (icon != null) icon.sprite = iconSprite;

        // 找不到對應零件：禁用，避免點了沒反應
        tgl.interactable = (targetPart != null);

        // 先設正常色
        ApplyToggleColors(tgl, false);

        tgl.onValueChanged.AddListener(isOn =>
        {
            ApplyToggleColors(tgl, isOn);
            if (!isOn) return;

            if (targetPart != null)
                SelectPartToColor(targetPart, instanceToSave);
        });

        // 最後才設 isOn（確保事件/顏色都已綁好）
        tgl.isOn = defaultOn;
        return tgl;
    }

    private void ApplyToggleColors(Toggle tgl, bool isOn)
    {
        if (tgl == null) return;

        var cb = tgl.colors;
        var c = isOn ? selectedColor : normalColor;

        cb.normalColor = c;
        cb.selectedColor = c;
        cb.highlightedColor = c;
        cb.pressedColor = c;

        tgl.colors = cb;
    }

    private Sprite GetPartIcon(WeaponPartType partType)
    {
        if (partType == WeaponPartType.Barrel) return barrelIconImage;
        if (partType == WeaponPartType.Scope) return scopeIconImage;
        return null; // 主人要擴充更多零件 icon，就在這裡加
    }

    // partType -> 找 attachmentPoints 的掛點名 -> FindChild -> 取該掛點下第一個 child
    private GameObject FindEquippedPartGO(GameObject weaponRoot, RangeWeapon rw, WeaponPartType partType)
    {
        if (weaponRoot == null || rw == null) return null;

        string mountName = null;
        foreach (var ap in rw.attachmentPoints)
        {
            if (ap.allowPart != partType) continue;
            if (ap.pointTransform == null) continue;
            mountName = ap.pointTransform.name;
            break;
        }

        if (string.IsNullOrEmpty(mountName)) return null;

        var mountGO = FindChild(weaponRoot, mountName);
        if (mountGO == null) return null;

        if (mountGO.transform.childCount <= 0) return null;
        return mountGO.transform.GetChild(0).gameObject;
    }

    private void ClearColorPickerState()
    {
        if (characterEquipmentColorPicker == null) return;

        characterEquipmentColorPicker.targetGameObject = null;
        characterEquipmentColorPicker.targetMaterials = new List<Material>();
        characterEquipmentColorPicker.currentMaterialIndex = -1;
        characterEquipmentColorPicker.currentTextureIndex = -1;
        characterEquipmentColorPicker.ClearnButton();
    }

    public void CleanPartButtons()
    {
        if (partsButtonParent == null) return;

        for (int i = partsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(partsButtonParent.GetChild(i).gameObject);
    }

    #endregion

    #region Select Part To Color

    // 保留舊介面：有些地方可能還在用 index 版本
    public void SelectPartToColor(int equipmentSlotsIndex)
    {
        if (!IsColorMode) return;

        var slots = EquipmentManager.Instance.equipmentSlots;
        if (equipmentSlotsIndex < 0 || equipmentSlotsIndex >= slots.Count)
        {
            ClearColorPickerState();
            return;
        }

        var go = slots[equipmentSlotsIndex].equipedItem;
        SelectPartToColor(go);
    }

    public void SelectPartToColor(GameObject part)
    {
        if (!IsColorMode) return;

        if (characterEquipmentColorPicker == null)
            return;

        if (part == null)
        {
            ClearColorPickerState();
            return;
        }

        characterEquipmentColorPicker.targetGameObject = part;
        characterEquipmentColorPicker.AddTargetMaterialsToList();
        characterEquipmentColorPicker.CreateButtons();
    }
    public void SelectPartToColor(GameObject part, ItemInstance instanceToSave)
    {
        if (!IsColorMode) return;
        if (characterEquipmentColorPicker == null) return;

        characterEquipmentColorPicker.targetItemInstance = instanceToSave;
        SelectPartToColor(part); // 沿用你原本的：設 targetGameObject + AddTargetMaterialsToList + CreateButtons
    }
    #endregion

    #region Utilities

    public int GetSelectedSlotIndex()
    {
        if (inventoryButtonParent == null) return -1;

        for (int i = 0; i < inventoryButtonParent.childCount; i++)
        {
            var child = inventoryButtonParent.GetChild(i);
            var t = child.GetComponentInChildren<Toggle>(true);
            if (t != null && t.isOn) return i;
        }
        return -1;
    }
    public GameObject FindChild(GameObject parentObject, string targetName)
    {
        if (parentObject == null) return null;

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

    private void SetEquipmentSlotIcon(int slotIndex, Sprite sprite)
    {
        if (inventoryButtonParent == null) return;
        if (slotIndex < 0 || slotIndex >= inventoryButtonParent.childCount) return;

        var iconGO = FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Item Icon");
        if (iconGO == null) return;

        var img = iconGO.GetComponent<Image>();
        if (img == null) return;

        img.sprite = sprite;
        img.color = (sprite != null) ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);
    }

    private void SetRemoveButtonForSlot(int slotIndex, bool active)
    {
        if (inventoryButtonParent == null) return;
        if (slotIndex < 0 || slotIndex >= inventoryButtonParent.childCount) return;

        var removeBtn = FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
        if (removeBtn != null) removeBtn.SetActive(active);
    }

    private void SetAllRemoveButtonsActive(bool active)
    {
        if (inventoryButtonParent == null) return;

        for (int i = 0; i < inventoryButtonParent.childCount; i++)
            SetRemoveButtonForSlot(i, active);
    }

    private void ShowInventoryPage()
    {
        if (statBlock != null) statBlock.SetActive(false);
        if (inventoryBlock != null) inventoryBlock.SetActive(true);

        currentPage = inventoryBlock;
        if (pageSwitch != null && pageSwitch.pages != null && pageSwitch.pages.Length > 0)
            pageSwitch.pages[0] = currentPage;
    }

    private void ShowStatPage()
    {
        if (statBlock != null) statBlock.SetActive(true);
        if (inventoryBlock != null) inventoryBlock.SetActive(false);

        currentPage = statBlock;
        if (pageSwitch != null && pageSwitch.pages != null && pageSwitch.pages.Length > 0)
            pageSwitch.pages[0] = currentPage;
    }

    #endregion
}
