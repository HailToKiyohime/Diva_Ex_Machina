using System.Collections.Generic;
using System.Net.Mail;
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
        // If an instance already exists and it's not this one, destroy this new instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // Exit to prevent further execution of this Awake method
        }
        // Otherwise, set this instance as the Singleton
        Instance = this;
    }

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

        if (craftingPartsToggleGroup.AnyTogglesOn() && weaponColorBlock.activeSelf == false)
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
                {
                    if (itemType == ItemType.RangeWeapon)
                    {
                        if (inv is RangeWeaponInstance rwi)
                        {
                            Debug.Log(" rwi.newWeaponName = " + rwi.newWeaponName);

                            string displayName = inv.item.itemName;   // 先用 ScriptableObject 的名字當預設

                            if (!string.IsNullOrEmpty(rwi.newWeaponName))
                                displayName = rwi.newWeaponName;      // 只有真的改過名才覆蓋

                            label.text = displayName;
                        }
                    }
                    else
                    {
                        label.text = inv.item.itemName;
                    }
                }

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
            foreach (Transform child in itemsButtonParent)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // 打開「武器零件」的背包清單，會再用 weaponPartType 過濾
    public void OpenRangeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        if (craftingPartsToggleGroup.AnyTogglesOn() && weaponColorBlock.activeSelf == false)
        {
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
                {
                    if (inv is RangeWeaponInstance rwi)
                    {
                        Debug.Log(" rwi.newWeaponName = " + rwi.newWeaponName);

                        string displayName = inv.item.itemName;   // 先用 ScriptableObject 的名字當預設

                        if (!string.IsNullOrEmpty(rwi.newWeaponName))
                            displayName = rwi.newWeaponName;      // 只有真的改過名才覆蓋

                        label.text = displayName;
                    }
                    else
                    {
                        label.text = inv.item.itemName;
                    }
                }
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

            // 保證右側只有一把武器被視為「選中」
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;       // 關掉其他武器的勾選狀態
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

                GameObject part = Instantiate(rwp.rangeWeaponPartPrefab, attachmentPoint.attachmentPointTransform);
                attachmentPoint.assembledPart = part;
                attachmentPoint.item = item;
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
            for(int i =1; i < craftingPartsButtonParent.childCount; i++)
            {
                Destroy(craftingPartsButtonParent.GetChild(i).gameObject);
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
        Image spriteImage = InventoryManager.Instance.FindChild(
        craftingPartsButtonParent.GetChild(GetSelectedSlotIndex()).gameObject,"Item Icon").GetComponent<Image>();
        spriteImage.sprite = null;
        spriteImage.color = new Color(1, 1, 1, 0);
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

                    // 這兩行是關鍵：用 local copy 固定住當前迴圈的值
                    int slotIndex = i;
                    var capturedSlot = slot;

                    btn.onValueChanged.AddListener(isOn =>
                    {
                        if (!isOn)
                        {
                            // 先關閉所有插槽上的移除按鈕
                            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
                            {
                                var removeBtnGo = InventoryManager.Instance.FindChild(
                                    craftingPartsButtonParent.GetChild(i).gameObject,
                                    "Remove Equipment Button"
                                );
                                if (removeBtnGo != null)
                                    removeBtnGo.SetActive(false);

                                ClearInventoryButton();
                            }
                        }

                        // 用固定好的 capturedSlot / slotIndex
                        OpenRangeWeaponPartsInventory(ItemType.WeaponPart, capturedSlot.equipmentType);
                        SelectWeaponPartToColor(slotIndex);
                    });
                }
            }
            uiPageSwitch.UpdateToggles();
        }
    }
    public void SelectWeaponPartToColor(int equipmentSlotsIndex)
    {
        // List 本身是不是 null
        if (craftingSlots == null)
        {
            Debug.LogWarning("SelectWeaponPartToColor: craftingSlots is null");
            return;
        }

        // 這裡才是關鍵：index 是否在 0 ~ Count-1 範圍內
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
            Debug.Log("SelectWeaponPartToColor: setting targetGameObject");
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
        for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
        {
            var removeBtn = FindChild(craftingPartsButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
            if (removeBtn != null)
                removeBtn.SetActive(false);
        }
        var slots = craftingSlots;
        if (index < 0 || index >= slots.Count) // 先驗證索引範圍
        {
            return;
        }
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
        }
    }

    public GameObject FindChild(GameObject parentObject, string targetName)
    {
        foreach (Transform childTransform in parentObject.transform)
        {
            if (childTransform.gameObject.name == targetName)
            {
                return childTransform.gameObject; // Found the grandchild
            }

            // Recursively search in the child's children (grandchild's children, etc.)
            GameObject foundGrandchild = FindChild(childTransform.gameObject, targetName);
            if (foundGrandchild != null)
            {
                return foundGrandchild;
            }
        }
        return null; // Grandchild not found in this branch
    }
    public void Forge()
    {
        Debug.Log("Forge: start");

        // 檢查有沒有任何 slot
        if (craftingSlots == null || craftingSlots.Count == 0)
        {
            Debug.LogWarning("Forge: craftingSlots is empty");
            return;
        }

        // 槽 0 是整把武器
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

        // ★ 先把「預覽武器本體」上改好的顏色，回寫到 RangeWeaponInstance
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
                    // ★ 從 slot.assembledPart 讀顏色，寫回 PartInstance
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

        if (newWeaponName.text != "")
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
        //    weaponInstance.colors / 每個 PartInstance.colors 已在上面同步好
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
        Image spriteImage = InventoryManager.Instance.FindChild(
        craftingPartsButtonParent.GetChild(0).gameObject, "Item Icon").GetComponent<Image>();
        spriteImage.sprite = null;
        spriteImage.color = new Color(1, 1, 1, 0);
        craftingSlots.Clear();
    }

    // 從實際場景中的 GameObject 抽出顏色與 shader 名稱
    private List<Color> ExtractColorsFromGameObject(GameObject go, out string shaderName)
    {
        shaderName = null;
        var colors = new List<Color>();

        if (!go) return colors;

        // 找一個 Renderer（MeshRenderer / SkinnedMeshRenderer 都可以）
        var renderer = go.GetComponentInChildren<Renderer>();
        if (!renderer) return colors;

        // 用 instance material，而不是 sharedMaterial
        var mat = renderer.material;
        if (!mat || mat.shader == null) return colors;

        shaderName = mat.shader.name;

        // 根據 shader 名稱讀取顏色欄位
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
