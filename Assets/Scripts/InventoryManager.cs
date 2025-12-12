using System;
using System.Collections.Generic;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public string newWeaponName;// 用來存鍛造後的新名稱
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<PartInstance> attachment;
    public Transform muzzlePoint;
    // 用來存不同 shader 的顏色
    [SerializeField]
    public List<Color> colors = new List<Color>();

    // 可選：記錄此裝備用的 shader 名稱，方便還原
    [SerializeField]
    public string shaderName;
}
[System.Serializable]
public class ArmorInstance : ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();

    // 用來存不同 shader 的顏色
    [SerializeField]
    public List<Color> colors = new List<Color>();

    // 可選：記錄此裝備用的 shader 名稱，方便還原
    [SerializeField]
    public string shaderName;
}
[System.Serializable]
public class PartInstance : ItemInstance
{
    public WeaponPartType partType;
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    // 用來存不同 shader 的顏色
    [SerializeField]
    public List<Color> colors = new List<Color>();
    // 可選：記錄此裝備用的 shader 名稱，方便還原
    [SerializeField]
    public string shaderName;
}


public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    [Header("Inventory Slot")]
    [SerializeReference] public List<ItemInstance> inventory = new();

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;
    public Transform itemsButtonParent;// 放按鈕的容器
    public Transform inventoryButtonParent;// 放按鈕的容器
    [Header("Color Block/ Inventory Block")]
    public GameObject inventoryBlock;
    public GameObject characterColorBlock;
    public GameObject statBlock;

    [Header("Color Panel")]
    public ColorPicker characterEquipmentColorPicker;
    [Header("Button Toggle Group")]
    public ToggleGroup inventoryToggleGroup;

    private GameObject currentPage;
    [SerializeField]
    private UIPageSwitch pageSwitch;

    // 紀錄每個 Toggle 對應的物品
    private Dictionary<Toggle, ItemInstance> toggleItemMap = new();

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

    public void AddItemToInventory(ItemObject item)
    {

        if (item is Armor a)
        {
            var inst = new ArmorInstance
            {
                item = item,
                amount = 1,
            };

            // 如果 Armor 有 SkinnedMeshRenderer，就讀取該材質的顏色
            if (a.skinnedMeshRenderer)
            {
                var renderer = a.skinnedMeshRenderer; // 先抓出 renderer (頂多 1~2 行)
                if (renderer.sharedMaterial)          // 再從 sharedMaterial 讀取 shader 與顏色
                {
                    var mat = renderer.sharedMaterial;
                    var shader = mat.shader;
                    inst.shaderName = shader.name;

                    // 根據 shader 名稱判斷要讀幾組顏色
                    if (shader.name.Contains("Mix 3"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        inst.colors.Add(mat.GetColor("_Layer1Color"));
                        inst.colors.Add(mat.GetColor("_Layer2Color"));
                    }
                    else if (shader.name.Contains("Mix 4"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        inst.colors.Add(mat.GetColor("_Layer1Color"));
                        inst.colors.Add(mat.GetColor("_Layer2Color"));
                        inst.colors.Add(mat.GetColor("_Layer3Color"));
                    }
                    else if (shader.name.Contains("Mix 5"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        for (int i = 1; i < 5; i++)
                            inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                    }
                }
            }
            // (1) 固定 Buff 先加
            foreach (EquipmentBuff buff in a.buffs)
            {
                inst.buffs.Add(buff);
            }
            // (2) 隨機 Buff，再從 Armor 的 RandomBuffPool 裡抽（如果 Armor 有設定 RandomBuffPool）
            var pickedBuff = a.GetRandomBuff(); // 回傳的可能是 null（代表沒抽到）
            if (pickedBuff != null)
            {
                inst.buffs.Add(pickedBuff.buff); // 只取其中一條隨機 Buff，也可以改成多條
            }

            // 最後把這個帶 Buff 的實例加到背包
            inventory.Add(inst);
            Debug.Log($"ArmorInstance added. Total buffs: {inst.buffs.Count}");
        }

        else if (item is RangeWeapon rw)
        {
            var attachmentPoints = new List<PartInstance>();

            foreach (var part in rw.attachmentPoints)
            {
                var attachment = new PartInstance
                {
                    item = null,
                    amount = 0,
                    partType = part.allowPart
                };
                attachmentPoints.Add(attachment);


            }

            var inst = new RangeWeaponInstance
            {
                item = item,
                amount = 1,
                attachment = attachmentPoints,
                muzzlePoint = rw.weaponPrefab != null
                                ? rw.weaponPrefab.transform.Find("MuzzlePoint")
                                : null,
            };

            if (rw.meshRenderer)
            {
                // 這裡以 meshRenderer.sharedMaterial 為主，減少重複程式碼深度
                var renderer = rw.meshRenderer;
                if (renderer.sharedMaterial)
                {
                    var mat = renderer.sharedMaterial;
                    var shader = mat.shader;
                    inst.shaderName = shader.name;
                    if (shader.name.Contains("Mix 3"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        inst.colors.Add(mat.GetColor("_Layer1Color"));
                        inst.colors.Add(mat.GetColor("_Layer2Color"));
                    }
                    else if (shader.name.Contains("Mix 4"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        inst.colors.Add(mat.GetColor("_Layer1Color"));
                        inst.colors.Add(mat.GetColor("_Layer2Color"));
                        inst.colors.Add(mat.GetColor("_Layer3Color"));
                    }
                    else if (shader.name.Contains("Mix 5"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        for (int i = 1; i < 5; i++)
                            inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                    }
                }
            }

            // (1) 固定 Buff 先加
            foreach (EquipmentBuff buff in rw.buffs)
            {
                inst.buffs.Add(buff);
            }

            // (2) 隨機 Buff，再從 Armor 的 RandomBuffPool 裡抽（如果 Armor 有設定 RandomBuffPool）
            var pickedBuff = rw.GetRandomBuff(); // 回傳的可能是 null（代表沒抽到）
            if (pickedBuff != null)
            {
                inst.buffs.Add(pickedBuff.buff); // 只取其中一條隨機 Buff，也可以改成多條
            }

            // 最後把這個帶 Buff 的實例加到背包
            inventory.Add(inst);
            Debug.Log($"RangeWeaponInstance added. Total buffs: {inst.buffs.Count}");
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
            {
                var renderer = rwp.meshRenderer;
                if (renderer.sharedMaterial)
                {
                    var mat = renderer.sharedMaterial;
                    var shader = mat.shader;
                    inst.shaderName = shader.name;
                    if (shader.name.Contains("Mix 3"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        inst.colors.Add(mat.GetColor("_Layer1Color"));
                        inst.colors.Add(mat.GetColor("_Layer2Color"));
                    }
                    else if (shader.name.Contains("Mix 4"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        inst.colors.Add(mat.GetColor("_Layer1Color"));
                        inst.colors.Add(mat.GetColor("_Layer2Color"));
                        inst.colors.Add(mat.GetColor("_Layer3Color"));
                    }
                    else if (shader.name.Contains("Mix 5"))
                    {
                        inst.colors.Add(mat.GetColor("_BaseColor"));
                        for (int i = 1; i < 5; i++)
                            inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                    }

                }
            }
            foreach (EquipmentBuff buff in rwp.buffs)
            {
                inst.buffs.Add(buff);
            }
            // (2) 隨機 Buff，再從 Armor 的 RandomBuffPool 裡抽（如果 Armor 有設定 RandomBuffPool）
            var pickedBuff = rwp.GetRandomBuff(); // 回傳的可能是 null（代表沒抽到）
            if (pickedBuff != null)
            {
                inst.buffs.Add(pickedBuff.buff); // 只取其中一條隨機 Buff，也可以改成多條
            }

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

        // 以參考相等為準，如果外面保留的是同一個實例，
        // 這裡就能正確找到並移除。
        int idx = inventory.IndexOf(itemInstance);
        if (idx >= 0)
        {
            inventory.RemoveAt(idx);
        }
        else
        {
            // 如果沒找到，也可以（選擇性）做一個 fallback：
            // 例如比對 item / amount 等欄位。視需求而定。
        }
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

        // 建一個全新的 RangeWeaponInstance，避免直接修改原本那一支
        var newInst = new RangeWeaponInstance
        {
            item = baseWeaponInstance.item,
            amount = 1,
            newWeaponName = baseWeaponInstance.newWeaponName,

            // Buff 直接複製一份（淺拷貝即可，視設計而定）
            buffs = new List<EquipmentBuff>(baseWeaponInstance.buffs),

            // 新的 Part 列表
            attachment = new List<PartInstance>(),

            // 槍口 Transform 直接沿用（因為多半指的是武器 prefab 上特定 child）
            muzzlePoint = baseWeaponInstance.muzzlePoint,

            // 從 baseWeaponInstance 複製顏色與 shaderName
            shaderName = baseWeaponInstance.shaderName,
            colors = (baseWeaponInstance.colors != null)
                        ? new List<Color>(baseWeaponInstance.colors)
                        : new List<Color>()
        };

        // 把剛剛 Forge() 收集好的零件 (PartInstance) 裝進去
        if (rangeWeaponParts != null)
        {
            foreach (var part in rangeWeaponParts)
            {
                if (part == null) continue;
                newInst.attachment.Add(part);
            }
        }

        // 加進背包
        inventory.Add(newInst);

        // 除錯資訊
        Debug.Log($"Forged new ranged weapon. Inventory count = {inventory.Count}");
    }
    public void OpenWeaponInventory()
    {
        OpenPartsInventory(ItemType.RangeWeapon);
    }
    public void OpenHeadArmorInventory()
    {
        OpenPartsInventory(ItemType.HeadArmor);
    }
    public void OpenChestArmorInventory()
    {
        OpenPartsInventory(ItemType.ChestArmor);
    }
    public void OpenLeftHandInventory()
    {
        OpenPartsInventory(ItemType.LeftHandArmor);
    }
    public void OpenRightHandInventory()
    {
        OpenPartsInventory(ItemType.RightHandArmor);
    }

    public void OpenWaistArmorInventory()
    {
        OpenPartsInventory(ItemType.WaistArmor);
    }
    public void OpenLegsArmorInventory()
    {
        OpenPartsInventory(ItemType.LegsArmor);
    }

    public void OpenThrusterInventory()
    {
        OpenPartsInventory(ItemType.Thruster);
    }

    public void OpenConsumableInventory()
    {
        OpenPartsInventory(ItemType.Consumable);
    }

    public void OpenMaterialInventory()
    {
        OpenPartsInventory(ItemType.Material);
    }

    public void OpenPartsInventory(ItemType itemType)
    {
        Debug.Log("colorBlock.activeSelf: " + characterColorBlock.activeSelf);
        if (characterColorBlock.activeSelf != true)
        {
            Debug.Log("inventoryToggleGroup.AnyTogglesOn(): " + inventoryToggleGroup.AnyTogglesOn());
            if (inventoryToggleGroup.AnyTogglesOn())
            {

                // 先清空右邊的物品列表
                ClearInventoryButton();

                // 先記住目前選到哪個裝備槽
                int slotIndex = GetSelectedSlotIndex();

                // 把所有裝備槽上的「Remove Equipment Button」先關掉
                for (int i = 0; i < inventoryButtonParent.childCount; i++)
                {
                    var removeBtn = FindChild(inventoryButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
                    if (removeBtn != null)
                        removeBtn.SetActive(false);
                }

                // 檢查這個槽現在是不是有裝東西
                bool slotHasEquipment = false;
                if (slotIndex >= 0 &&
                    EquipmentManager.Instance != null &&
                    slotIndex < EquipmentManager.Instance.equipmentSlots.Count)
                {
                    slotHasEquipment = EquipmentManager.Instance.equipmentSlots[slotIndex].equipedItem != null;
                }

                // 開始生成符合這個類型的物品按鈕
                foreach (var inv in inventory)
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
                    var label = button.transform.Find("Item Name")?.GetComponent<TMP_Text>();
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

                    // Toggle
                    var btn = button.GetComponent<Toggle>();
                    if (btn != null)
                    {
                        // 如果這個槽本來就有裝備，就把那個槽的 Remove Button 打開
                        if (slotHasEquipment && slotIndex >= 0)
                        {
                            var removeBtn = FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
                            if (removeBtn != null)
                                removeBtn.SetActive(true);
                        }

                        // 為了避免閉包問題，做一個本地變數
                        ItemInstance capturedItem = inv;
                        toggleItemMap[btn] = capturedItem;
                        btn.onValueChanged.AddListener(isOn =>
                        {
                            if (!isOn) return;
                            OnClickInventoryItem(capturedItem, btn);
                        });

                        // 如果這個物品已經裝在任一個槽上，就把它鎖住
                        foreach (var slot in EquipmentManager.Instance.equipmentSlots)
                        {
                            if (slot != null && capturedItem == slot.item)
                            {
                                btn.interactable = false;
                                break;
                            }
                        }
                    }
                }
                statBlock.SetActive(false);
                inventoryBlock.SetActive(true);
                currentPage = inventoryBlock;
                pageSwitch.pages[0] = currentPage;
            }
            else
            {
                statBlock.SetActive(true);
                inventoryBlock.SetActive(false);
                // 其他兩個 Block 可視需求加上 .SetActive(false)
                // characterColorBlock.SetActive(false);
                // 把所有裝備槽上的「Remove Equipment Button」先關掉
                for (int i = 0; i < inventoryButtonParent.childCount; i++)
                {
                    var removeBtn = FindChild(inventoryButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
                    if (removeBtn != null)
                        removeBtn.SetActive(false);
                }
                // 記得更新 currentPage
                currentPage = statBlock;
                pageSwitch.pages[0] = currentPage;
            }
        }
        {
            if (characterColorBlock.activeSelf == true)
            {
                if (inventoryToggleGroup.AnyTogglesOn())
                {
                    SelectPartToColor(GetSelectedSlotIndex()); // 傳入裝備槽索引
                }
                else
                {
                    characterEquipmentColorPicker.targetGameObject = null;
                    characterEquipmentColorPicker.targetMaterials = new List<Material>();
                    characterEquipmentColorPicker.currentMaterialIndex = -1;
                    characterEquipmentColorPicker.currentTextureIndex = -1;
                    characterEquipmentColorPicker.ClearnButton();
                }
            }
        }
    }
    public void ClearInventoryButton()
    {
        for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(itemsButtonParent.GetChild(i).gameObject);

        toggleItemMap.Clear();
    }
    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        Debug.Log("Working");
        if (item is RangeWeaponInstance rw)
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

            if (EquipmentManager.Instance.TryEquipWeaponFromInventory(item, mountPoint, slotIndex))
            {
                // 解鎖同一清單中的其他按鈕
                foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
                {
                    if (t == btn)
                        continue;

                    // 關掉其它按鈕的選取狀態
                    t.isOn = false;

                    // 找不到對應物品 → 當成一般按鈕，解鎖即可
                    if (!toggleItemMap.TryGetValue(t, out var mappedItem))
                    {
                        t.interactable = true;
                        continue;
                    }

                    // 檢查這個物品有沒有裝在任何一個裝備槽
                    bool equipped = false;
                    foreach (var slot in EquipmentManager.Instance.equipmentSlots)
                    {
                        if (slot != null && slot.item == mappedItem)
                        {
                            equipped = true;
                            break;
                        }
                    }

                    // 如果已經裝備，就保持鎖住；沒裝就可以互動
                    t.interactable = !equipped;
                }

                Image spriteImage =
                    FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Item Icon")
                    .GetComponent<Image>();
                spriteImage.sprite = item.item.icon;
                spriteImage.color = new Color(1, 1, 1, 1);

                FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button")
                    .SetActive(true);

                // 鎖住當前已裝備的按鈕
                btn.interactable = false;
            }
            else
            {
                btn.isOn = false;
            }
        }
        else
        {
            // 原本 Armor / 其他裝備邏輯
            if (EquipmentManager.Instance.TryEquipFromInventory(item))
            {
                foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
                {
                    if (t == btn)
                        continue;

                    // 關掉其它按鈕的選取狀態
                    t.isOn = false;

                    // 找不到對應物品 → 當成一般按鈕，解鎖即可
                    if (!toggleItemMap.TryGetValue(t, out var mappedItem))
                    {
                        t.interactable = true;
                        continue;
                    }

                    // 檢查這個物品有沒有裝在任何一個裝備槽
                    bool equipped = false;
                    foreach (var slot in EquipmentManager.Instance.equipmentSlots)
                    {
                        if (slot != null && slot.item == mappedItem)
                        {
                            equipped = true;
                            break;
                        }
                    }

                    // 如果已經裝備，就保持鎖住；沒裝就可以互動
                    t.interactable = !equipped;
                }

                Image spriteImage =
                    FindChild(inventoryButtonParent.GetChild(GetSelectedSlotIndex()).gameObject, "Item Icon")
                    .GetComponent<Image>();
                spriteImage.sprite = item.item.icon;
                spriteImage.color = new Color(1, 1, 1, 1);
                FindChild(inventoryButtonParent.GetChild(GetSelectedSlotIndex()).gameObject, "Remove Equipment Button")
                    .SetActive(true);

                btn.interactable = false;
            }
            else
            {
                btn.isOn = false;
            }
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

        // 清空該設備槽
        EquipmentManager.Instance.CleanEquipmentSlot(index);

        // 找到該槽位下的 Remove Equipment Button 並隱藏
        var removeBtn = FindChild(inventoryButtonParent.GetChild(index).gameObject, "Remove Equipment Button");
        if (removeBtn != null)
            removeBtn.SetActive(false);

        Image spriteImage = FindChild(inventoryButtonParent.GetChild(index).gameObject, "Item Icon")
                            .GetComponent<Image>();
        spriteImage.sprite = null;
        spriteImage.color = new Color(1, 1, 1, 0);  // alpha=0 代表完全透明

        // 重新打開該槽位種類對應的物品清單
        OpenInventoryPage();
        if (characterColorBlock.activeSelf == true)
        {
            SelectPartToColor(index);
        }
    }
    public void OpenColorPage()
    {
        int index = GetSelectedSlotIndex();

        // 把所有裝備槽上的「Remove Equipment Button」先關掉
        for (int i = 0; i < inventoryButtonParent.childCount; i++)
        {
            var removeBtn = FindChild(inventoryButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
            if (removeBtn != null)
                removeBtn.SetActive(false);
        }

        // 嘗試開顏色頁面前的條件檢查：EquipSlot 是否存在、索引是否在範圍內
        var slots = EquipmentManager.Instance.equipmentSlots;
        if (index < 0 || index >= slots.Count)
            return;

        // 如果顏色面板目前沒開，就直接不處理
        if (!characterColorBlock.activeSelf) return;

        // 取得該槽目前裝備的物件
        var go = slots[index].equipedItem;
        if (!go)
        {
            // 若沒裝備則清空 ColorPicker 的所有設定
            characterEquipmentColorPicker.targetGameObject = null;
            characterEquipmentColorPicker.targetMaterials = new List<Material>();
            characterEquipmentColorPicker.currentMaterialIndex = -1;
            characterEquipmentColorPicker.currentTextureIndex = -1;
            characterEquipmentColorPicker.ClearnButton();
        }
        else
        {
            // 若有裝備，則設定 ColorPicker 的目標物件並刷新按鈕
            characterEquipmentColorPicker.targetGameObject = go;
            characterEquipmentColorPicker.AddTargetMaterialsToList();
            characterEquipmentColorPicker.CreateButtons();
        }
    }

    // 新增：選擇不同的裝備槽時，重新指定 ColorPicker 的 target
    public void SelectPartToColor(int equipmentSlotsIndex)
    {
        if (characterColorBlock.activeSelf == true)
        {

            var slots = EquipmentManager.Instance.equipmentSlots;
            if (equipmentSlotsIndex < 0 || equipmentSlotsIndex >= slots.Count)
                return;

            var go = slots[equipmentSlotsIndex].equipedItem;
            if (go != null)
            {
                characterEquipmentColorPicker.targetGameObject = go;
                characterEquipmentColorPicker.AddTargetMaterialsToList();
                characterEquipmentColorPicker.CreateButtons();
            }
            else
            {
                characterEquipmentColorPicker.targetGameObject = null;
                characterEquipmentColorPicker.targetMaterials = new List<Material>();
                characterEquipmentColorPicker.currentMaterialIndex = -1;
                characterEquipmentColorPicker.currentTextureIndex = -1;
                characterEquipmentColorPicker.ClearnButton();
            }
        }
    }

    public int GetSelectedSlotIndex()
    {
        for (int i = 0; i < inventoryButtonParent.childCount; i++)
        {
            var child = inventoryButtonParent.GetChild(i);
            var t = child.GetComponentInChildren<Toggle>(true);
            if (t != null && t.isOn)
            {
                return i; // 找到當前被勾選的（isOn = true）按鈕所在的索引
            }
        }
        return -1; // 都沒有人被選到
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
