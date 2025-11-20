using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public static CraftingManager Instance { get; private set; }

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

    public ColorPicker weaponPartColorPicker;
    public GameObject weaponColorBlock;

    public TMP_InputField newWeaponName;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ===== 封裝用小工具 =====

    // 取得顯示名稱（有 newWeaponName 就優先）
    private string GetDisplayName(ItemInstance inv)
    {
        if (inv is RangeWeaponInstance rwi && !string.IsNullOrEmpty(rwi.newWeaponName))
            return rwi.newWeaponName;
        return inv.item.itemName;
    }

    // 判斷 RangeWeaponInstance 是否已經鍛造過（有裝零件）
    private bool IsForgedWeapon(RangeWeaponInstance rwi)
    {
        if (rwi.attachment == null) return false;
        foreach (var part in rwi.attachment)
        {
            if (part != null && part.item != null)
                return true;
        }
        return false;
    }

    // 左側所有 slot 的 Remove 按鈕關掉
    private void HideAllRemoveButtonsOnCraftingSlots()
    {
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

    // 某個 slot 是否已有東西
    private bool SlotHasEquipment(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= craftingSlots.Count) return false;
        var slot = craftingSlots[slotIndex];
        return slot != null && slot.item != null && slot.item.item != null;
    }

    // 指定 slot 的 Remove 按鈕依照有無裝備刷新
    private void RefreshRemoveButtonForSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= craftingPartsButtonParent.childCount)
            return;

        bool hasEquipment = SlotHasEquipment(slotIndex);
        var removeBtnGo = InventoryManager.Instance.FindChild(
            craftingPartsButtonParent.GetChild(slotIndex).gameObject,
            "Remove Equipment Button"
        );
        if (removeBtnGo != null)
            removeBtnGo.SetActive(hasEquipment);
    }

    // 右側「背包物品按鈕」建立（合成畫面專用）
    private void CreateInventoryButtonForCraftingItem(
        ItemInstance inv,
        bool slotHasEquipment,
        int slotIndex
    )
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

        // 插槽已有裝備 → 顯示 Remove 按鈕
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

        // 已經裝在某個 slot 的東西不能再選
        foreach (var slot in craftingSlots)
        {
            if (slot != null && capturedItem == slot.item)
            {
                btn.interactable = false;
                break;
            }
        }
    }

    // ===== 提供給 UI 的入口 =====

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

        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
        {
            // 沒選插槽或正在開顏色頁面 → 關掉 Remove 按鈕 + 清空列表
            HideAllRemoveButtonsOnCraftingSlots();
            ClearInventoryButton();
            return;
        }

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {
            if (inv == null || inv.item == null || inv.item.type != itemType)
                continue;

            // 只列出「還沒鍛造的 blueprint 武器」
            if (inv is RangeWeaponInstance rwi && IsForgedWeapon(rwi))
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }

    // 打開「武器零件」的背包清單，會再用 weaponPartType 過濾
    public void OpenRangeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
            return;

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {
            if (inv == null || inv.item == null || inv.item.type != itemType)
                continue;

            if (!(inv.item is RangeWeaponPart rwp) || rwp.partType != weaponPartType)
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }

    // 右側「背包物品按鈕」被勾選時的處理
    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        // 整把武器
        if (item.item is RangeWeapon rw)
        {
            // 先清掉舊預覽與舊插槽（避免重複產生 part slot）
            if (weaponPreview != null)
                Destroy(weaponPreview);

            CleanCraftingSlots();   // 清左側 UI + craftingSlots

            // 建立新的武器預覽
            GameObject weapon = Instantiate(rw.weaponPrefab, weaponPreviewTransform);
            weaponPreview = weapon;
            rangeWeapon = rw;

            // 槽 0：整把武器本體
            craftingSlots.Add(new CraftingSlot
            {
                assembledPart = weapon,
                attachmentPointTransform = null,
                equipmentType = WeaponPartType.Gun,
                item = item
            });

            if (item is RangeWeaponInstance rwi)
            {
                newWeaponName.text = !string.IsNullOrEmpty(rwi.newWeaponName)
                    ? rwi.newWeaponName
                    : item.item.itemName;
            }

            // 之後的槽：各個掛點
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

            // 依照新的 craftingSlots 重新產生左側插槽按鈕
            CreateCraftingSlots();

            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
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
                if (attachmentPoint.assembledPart != null)
                {
                    Destroy(attachmentPoint.assembledPart);
                }

                GameObject part = Instantiate(rwp.rangeWeaponPartPrefab, attachmentPoint.attachmentPointTransform);
                attachmentPoint.assembledPart = part;
                attachmentPoint.item = item;
            }
            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
        }

        // 更新左側插槽圖示
        int selectedIndex = GetSelectedSlotIndex();
        if (selectedIndex >= 0 && selectedIndex < craftingPartsButtonParent.childCount)
        {
            Image spriteImage = InventoryManager.Instance
                .FindChild(craftingPartsButtonParent.GetChild(selectedIndex).gameObject, "Item Icon")
                .GetComponent<Image>();

            spriteImage.sprite = item.item.icon;
            spriteImage.color = new Color(1, 1, 1, 1);
        }

        // 更新 Remove 按鈕顯示
        RefreshRemoveButtonForSlot(GetSelectedSlotIndex());

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
            var t = child.GetComponentInChildren<Toggle>(true);
            if (t != null && t.isOn)
                return i;
        }
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

            // 移除左側 UI（保留第 0 個按鈕）
            for (int i = craftingPartsButtonParent.childCount - 1; i > 0; i--)
            {
                Destroy(craftingPartsButtonParent.GetChild(i).gameObject);
            }

            craftingSlots.Clear();
            weaponPreview = null;
            rangeWeapon = null;

            // 重置第 0 個 icon
            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(0).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);

            OpenRangeWeaponInventory();
        }
        else
        {
            var slot = craftingSlots[index];
            if (slot.assembledPart != null)
                Destroy(slot.assembledPart);

            slot.assembledPart = null;
            slot.item = null;

            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(index).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);

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
        if (craftingSlots.Count <= 1) return;

        // 從 index 1 開始（0 是整把武器按鈕）
        for (int i = 1; i < craftingSlots.Count; i++)
        {
            var slot = craftingSlots[i];
            var slotButton = Instantiate(craftingSlotPrefab, craftingPartsButtonParent);

            Image icon = FindChildRecursive(slotButton.transform, "Slot Icon")?.GetComponent<Image>();
            if (icon != null)
            {
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

                int slotIndex = i;
                var capturedSlot = slot;

                btn.onValueChanged.AddListener(isOn =>
                {
                    if (!isOn)
                    {
                        HideAllRemoveButtonsOnCraftingSlots();
                        ClearInventoryButton();
                        return;
                    }

                    OpenRangeWeaponPartsInventory(ItemType.WeaponPart, capturedSlot.equipmentType);
                    SelectWeaponPartToColor(slotIndex);
                });
            }
        }
        uiPageSwitch.UpdateToggles();
    }

    public void SelectWeaponPartToColor(int equipmentSlotsIndex)
    {
        if (craftingSlots == null)
        {
            Debug.LogWarning("SelectWeaponPartToColor: craftingSlots is null");
            return;
        }

        if (equipmentSlotsIndex < 0 || equipmentSlotsIndex >= craftingSlots.Count)
        {
            Debug.LogWarning(
                $"SelectWeaponPartToColor: index out of range. index={equipmentSlotsIndex}, count={craftingSlots.Count}"
            );
            return;
        }

        var slot = craftingSlots[equipmentSlotsIndex];

        // 顏色區塊沒開就不用做事
        if (!weaponColorBlock.activeSelf)
            return;

        if (slot != null && slot.assembledPart != null && craftingPartsToggleGroup.AnyTogglesOn())
        {
            weaponPartColorPicker.targetGameObject = slot.assembledPart;
            weaponPartColorPicker.AddTargetMaterialsToList();
            weaponPartColorPicker.CreateButtons();
        }
        else
        {
            weaponPartColorPicker.targetGameObject = null;
            weaponPartColorPicker.targetMaterials = new List<Material>();
            weaponPartColorPicker.currentMaterialIndex = -1;
            weaponPartColorPicker.currentTextureIndex = -1;
            weaponPartColorPicker.ClearnButton();
        }
    }

    public void OpenColorPage()
    {
        int index = GetSelectedSlotIndex();

        // 把所有裝備槽上的「Remove Equipment Button」先關掉
        HideAllRemoveButtonsOnCraftingSlots();

        var slots = craftingSlots;
        if (index < 0 || index >= slots.Count)
            return;
        if (!weaponColorBlock.activeSelf) return;

        var go = slots[index].assembledPart;
        if (!go)
        {
            weaponPartColorPicker.targetGameObject = null;
            weaponPartColorPicker.targetMaterials = new List<Material>();
            weaponPartColorPicker.currentMaterialIndex = -1;
            weaponPartColorPicker.currentTextureIndex = -1;
            weaponPartColorPicker.ClearnButton();
        }
        else
        {
            weaponPartColorPicker.targetGameObject = go;
            weaponPartColorPicker.AddTargetMaterialsToList();
            weaponPartColorPicker.CreateButtons();

            if (slots[index].item is RangeWeaponInstance rwi)
            {
                // 可選：用材質當作 fallback
                var renderer = go.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var mat = renderer.material;
                    if (mat != null && mat.HasProperty("_BaseColor"))
                        weaponPartColorPicker.CursorToColor(mat.GetColor("_BaseColor"));
                }
            }
        }
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

    public void Forge()
    {
        Debug.Log("Forge: start");

        if (craftingSlots == null || craftingSlots.Count == 0)
        {
            Debug.LogWarning("Forge: craftingSlots is empty");
            return;
        }

        var baseSlot = craftingSlots[0];
        if (baseSlot == null || baseSlot.item == null)
        {
            Debug.LogWarning("Forge: base slot has no item");
            return;
        }

        if (!(baseSlot.item is RangeWeaponInstance weaponInstance))
        {
            Debug.LogWarning($"Forge: base slot item is not RangeWeaponInstance, actual={baseSlot.item.GetType().Name}");
            return;
        }

        // 先把「預覽武器本體」上改好的顏色，回寫到 RangeWeaponInstance
        if (baseSlot.assembledPart != null)
        {
            string baseShaderName;
            var baseColors = ExtractColorsFromGameObject(baseSlot.assembledPart, out baseShaderName);

            if (baseColors.Count > 0)
            {
                weaponInstance.colors = baseColors;
                weaponInstance.shaderName = baseShaderName;
            }
        }

        // 收集零件（同時同步每個零件的顏色）
        var rangeWeaponPartInstances = new List<PartInstance>();

        if (craftingSlots.Count > 1)
        {
            for (int i = 1; i < craftingSlots.Count; i++)
            {
                var slot = craftingSlots[i];
                if (slot == null || slot.item == null)
                    continue;

                if (slot.item is PartInstance pi)
                {
                    if (slot.assembledPart != null)
                    {
                        string partShaderName;
                        var partColors = ExtractColorsFromGameObject(slot.assembledPart, out partShaderName);

                        if (partColors.Count > 0)
                        {
                            pi.colors = partColors;
                            pi.shaderName = partShaderName;
                        }
                    }

                    rangeWeaponPartInstances.Add(pi);
                }
                else
                {
                    Debug.LogWarning($"Forge: slot[{i}] item is not PartInstance, actual={slot.item.GetType().Name}");
                }
            }
        }

        if (!string.IsNullOrEmpty(newWeaponName.text))
        {
            Debug.Log("Forge: setting new weapon name to " + newWeaponName.text);
            weaponInstance.newWeaponName = newWeaponName.text;
        }

        if (rangeWeaponPartInstances.Count == 0)
        {
            Debug.LogWarning("Forge: no parts selected, cannot forge");
            return;
        }

        // 1) 生成鍛造後的新武器，加入背包
        InventoryManager.Instance.AddCraftedRangeWeaponToInventory(weaponInstance, rangeWeaponPartInstances);

        // 2) 消耗掉原本的 blueprint 武器 + 零件
        InventoryManager.Instance.RemoveItemFromInventory(baseSlot.item);
        foreach (var part in rangeWeaponPartInstances)
        {
            InventoryManager.Instance.RemoveItemFromInventory(part);
        }

        // 清畫面
        if (weaponPreview != null)
        {
            Destroy(weaponPreview);
            weaponPreview = null;
        }
        CleanCraftingSlots();

        Debug.Log("Forge: success");
    }

    public void CleanCraftingSlots()
    {
        for (int i = craftingPartsButtonParent.childCount - 1; i > 0; i--)
        {
            Destroy(craftingPartsButtonParent.GetChild(i).gameObject);
        }

        if (craftingPartsButtonParent.childCount > 0)
        {
            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(0).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);
        }

        craftingSlots.Clear();
        HideAllRemoveButtonsOnCraftingSlots();
    }

    // 從實際場景中的 GameObject 抽出顏色與 shader 名稱
    private List<Color> ExtractColorsFromGameObject(GameObject go, out string shaderName)
    {
        shaderName = null;
        var colors = new List<Color>();

        if (!go) return colors;

        var renderer = go.GetComponentInChildren<Renderer>();
        if (!renderer) return colors;

        var mat = renderer.material;
        if (!mat || mat.shader == null) return colors;

        shaderName = mat.shader.name;

        if (shaderName.Contains("Mix 3"))
        {
            if (mat.HasProperty("_BaseColor")) colors.Add(mat.GetColor("_BaseColor"));
            if (mat.HasProperty("_Layer1Color")) colors.Add(mat.GetColor("_Layer1Color"));
            if (mat.HasProperty("_Layer2Color")) colors.Add(mat.GetColor("_Layer2Color"));
        }
        else if (shaderName.Contains("Mix 4"))
        {
            if (mat.HasProperty("_BaseColor")) colors.Add(mat.GetColor("_BaseColor"));
            if (mat.HasProperty("_Layer1Color")) colors.Add(mat.GetColor("_Layer1Color"));
            if (mat.HasProperty("_Layer2Color")) colors.Add(mat.GetColor("_Layer2Color"));
            if (mat.HasProperty("_Layer3Color")) colors.Add(mat.GetColor("_Layer3Color"));
        }
        else if (shaderName.Contains("Mix 5"))
        {
            if (mat.HasProperty("_BaseColor")) colors.Add(mat.GetColor("_BaseColor"));
            for (int i = 1; i < 5; i++)
            {
                string prop = $"_Layer{i}Color";
                if (mat.HasProperty(prop))
                    colors.Add(mat.GetColor(prop));
            }
        }

        return colors;
    }
}
