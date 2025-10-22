using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[System.Serializable]
public class ItemInstance
{
    public ItemObject item;
    public int amount;
}
[System.Serializable]
public class WeaponInstance: ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();

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

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI Button Prefab")]
    public GameObject ButtonPrefab;
    public Transform contentParent;      // 放按鈕的容器(ScrollView/Panel)
    public bool clearOldButtons = true;  // 切換分頁時是否清空舊按鈕

    private int currentEquipSlotId = -1;

    [Header("EquipmentManager")]
    public EquipmentManager equipmentManager;
    [Header("Inventory Slot")]
    [SerializeReference] public List<ItemInstance> slots = new();

    private readonly Dictionary<ItemType, List<int>> buckets = new();


    public int version { get; private set; } = 0;
    public event Action<int> OnChanged;

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

        // Optional: Make the GameObject persistent across scene loads
        DontDestroyOnLoad(gameObject);

        // 初始化所有枚舉類別的 bucket
        foreach (ItemType t in Enum.GetValues(typeof(ItemType)))
            buckets[t] = new List<int>();
    }
    // 取得 bucket（UI 直接讀這個）
    public IReadOnlyList<int> GetBucket(ItemType t) => buckets[t];

    // 由索引取實體
    public ItemInstance GetAt(int id) => slots[id];



    public void Add(ItemObject item)
    {
        if (item is Armor a)
        {
            var inst = new ArmorInstance { item = item, amount = 1 };

            var smr = a.skinnedMeshRenderer;
            if (smr && smr.sharedMaterial)
            {
                var mat = smr.sharedMaterial;
                var shader = mat.shader;
                inst.shaderName = shader.name;

                // 根據 shader 名稱決定要存幾個顏色
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

            foreach (EquipmentBuff buff in a.buffs)
            {
                inst.buffs.Add(buff);
            }

            var pickedBuff = a.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            int id = slots.Count;
            slots.Add(inst);
            buckets[item.type].Add(id);
            Touch();
        }
        else
        {
            var inst = new ItemInstance
            {
                item = item,
                amount = 1,
            };
            int id = slots.Count;
            slots.Add(inst);
            buckets[item.type].Add(id);  // ★ 同步更新 bucket
            Touch();
        }
    }

    // （可選）移除：示範 swap-remove，維護 bucket
    public void RemoveAt(int id)
    {
        int last = slots.Count - 1;
        if (id < 0 || id > last) return;

        // 記住被刪物件的類型（之後從對應 bucket 刪除 id）
        var removedItem = slots[id];
        var removedType = removedItem.item.type;

        if (id != last)
        {
            // 取出最後一個，搬到 id 位置
            var lastItem = slots[last];
            slots[id] = lastItem;

            // 在「最後一個的 bucket」裡把索引 last 改成 id
            var lastBucket = buckets[lastItem.item.type];
            int k = lastBucket.IndexOf(last);
            if (k >= 0) lastBucket[k] = id;
        }

        // 從「被刪物件的 bucket」移除 id（注意：不是移除 last）
        var removedBucket = buckets[removedType];
        removedBucket.Remove(id);

        // 最後刪掉尾巴
        slots.RemoveAt(last);
        Touch();
    }

    // 載入或大量變動後重建快取
    [ContextMenu("RebuildBuckets")]
    public void RebuildBuckets()
    {
        foreach (var kv in buckets) kv.Value.Clear();
        for (int i = 0; i < slots.Count; i++)
            buckets[slots[i].item.type].Add(i);
        Touch();
    }

    void Touch() { version++; OnChanged?.Invoke(version); }

    public void printSlots()
    {
        foreach (ItemInstance item in slots)
        {
            if (item is ArmorInstance a)
            {
                string buffText = "";
                foreach (EquipmentBuff buff in a.buffs)
                {
                    buffText = buffText + "Buff: " + buff.attribute + ", Vale:" + buff.value + " |";
                }


                Debug.Log("Item:" + a.item.itemName + " ," + buffText);
            }
            else if (item is ItemInstance i)
            {
                Debug.Log("Item:" + i.item.itemName);
            }
        }
    }

    public List<SkinnedMeshRenderer> EquipTest()
    {

        var temp = new List<SkinnedMeshRenderer>();   // ✅ 初始化

        foreach (var item in slots)
        {
            if (item is ArmorInstance ai && ai.item is Armor armor)
            {
                if (armor.skinnedMeshRenderer != null)   // ✅ 防呆
                {
                    temp.Add(armor.skinnedMeshRenderer);
                }
            }
        }
        return temp;
    }
    //run when button clicked

    public void OpenWeaponInventory() => OpenPartsInventory(ItemType.Weapon);
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
        if (contentParent == null || ButtonPrefab == null)
        {
            Debug.LogWarning("[Inventory] UI 未設置");
            return;
        }

        // 取得當前分頁的槽位ID
        int equipSlotId = -1;
        var src = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        var bid = src ? src.GetComponentInParent<ButtonID>() : null;
        if (bid) equipSlotId = bid.ID;
        currentEquipSlotId = equipSlotId;

        // 1) 先全部關閉
        HideAllRemoveButtons();

        // 2) 再只開當前分頁底下的
        ShowRemoveButtonUnder(src ? src.transform : null, equipSlotId);

        // 3) 清/生清單（原樣）
        if (clearOldButtons) ClearInventoryButton();
        var ids = GetBucket(itemType);
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            var inst = GetAt(id);
            if (inst?.item == null) continue;

            var go = Instantiate(ButtonPrefab, contentParent);
            var icon = go.transform.Find("Item Icon")?.GetComponent<Image>();
            if (icon) icon.sprite = inst.item.icon;
            var label = go.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
            if (label) label.text = inst.item.itemName;

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                int slotId = equipSlotId;
                btn.onClick.AddListener(() => OnClickInventoryItem(id, slotId));
            }
        }
    }
    private void OnClickInventoryItem(int id, int slotId)
    {
        var inst = GetAt(id);
        if (inst is ArmorInstance ai && ai.item is Armor ar && ar.skinnedMeshRenderer != null)
        {
            if (slotId < 0 || slotId >= equipmentManager.equipmentSlots.Count)
            {
                Debug.LogWarning("尚未選擇裝備槽");
                return;
            }

            // 嘗試從背包裝備到指定槽
            if (equipmentManager.TryEquipFromInventory(this, id, slotId))
            {
                // 從背包移除該項（注意：RemoveAt 是 swap-remove，UI 需重建）
                RemoveAt(id);

                // 依當前分頁類型重建清單（或記住當前 ItemType 後再呼叫）
                // 這裡用物件的類型重建一次
                OpenPartsInventory(inst.item.type);
            }
        }
        else
        {
            Debug.Log($"Select {inst.item.itemName}");
        }
    }

    public void ClearInventoryButton()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);
    }

    public void HideAllRemoveButtons()
    {
        if (!equipmentManager.EquipmentPage) return;
        foreach (Transform t in equipmentManager.EquipmentPage.GetComponentsInChildren<Transform>(true))
            if (t.name == "Remove Equipment Button") t.gameObject.SetActive(false);
    }

    private void ShowRemoveButtonUnder(Transform root, int equipSlotId)
    {
        if (!root) return;
        if (equipSlotId < 0 || equipSlotId >= equipmentManager.equipmentSlots.Count) return;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Remove Equipment Button" &&
                equipmentManager.equipmentSlots[equipSlotId].equipedItem != null)
            {
                var go = t.gameObject;
                go.SetActive(true);

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();                // ★ 先清舊的
                    int slotId = equipSlotId;                        // 捕捉
                    btn.onClick.AddListener(() => RemoveEquipment(t, slotId));
                }
                break;
            }
        }
    }

    public void RemoveEquipment(Transform t, int equipSlotId)
    {
        equipmentManager.UnequipItem(equipSlotId);
        if (equipmentManager.equipmentSlots[equipSlotId].equipedItem == null)
        {
            t.gameObject.SetActive(false);
        }

    }

    public void AddInstance(ItemInstance inst)
    {
        if (inst == null || inst.item == null) return;

        int id = slots.Count;
        slots.Add(inst);
        buckets[inst.item.type].Add(id);
        Touch();
    }
}
