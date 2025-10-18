using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;

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
}

public class InventoryManager : MonoBehaviour
{
    [Header("UI Button Prefab")]
    public GameObject ButtonPrefab;
    public Transform contentParent;      // 放按鈕的容器(ScrollView/Panel)
    public bool clearOldButtons = true;  // 切換分頁時是否清空舊按鈕

    [Header("BoneCombiner")]
    public BoneCombiner boneCombiner;
    [Header("Inventory Slot")]
    public ItemObject test1;
    public ItemObject test2;
    public ItemObject test3;
    public ItemObject test4;
    public ItemObject test5;
    public ItemObject test6;
    public ItemObject test7;
    [SerializeReference] public List<ItemInstance> slots = new();

    private readonly Dictionary<ItemType, List<int>> buckets = new();


    public int version { get; private set; } = 0;
    public event Action<int> OnChanged;

    void Awake()
    {
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

        // 取得最後元素與其類別 bucket
        var lastItem = slots[last];
        var lastBucket = buckets[lastItem.item.type];

        // 如果刪除的不是最後一個，做 swap
        if (id != last)
        {
            slots[id] = lastItem;

            // 在 lastItem 所在 bucket 裡把「last -> id」
            int k = lastBucket.IndexOf(last);
            if (k >= 0) lastBucket[k] = id;
        }

        // 從各自 bucket 移除對應索引
        var removedType = slots[last].item.type;
        buckets[removedType].Remove(last);

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
            Debug.LogWarning("[Inventory] contentParent 或 ButtonPrefab 未設置。");
            return;
        }
        if (clearOldButtons) ClearChildren(contentParent);

        var ids = GetBucket(itemType);
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];                   // 重要：閉包捕獲
            var inst = GetAt(id);
            if (inst?.item == null) continue;

            var go = Instantiate(ButtonPrefab, contentParent);

            var icon = go.transform.Find("Item Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = inst.item.icon;

            // 設置文字（TMP）
            var labelTMP = go.transform.Find("Item Name")?.GetComponent<TMP_Text>();
            if (labelTMP != null) labelTMP.text = inst.item.itemName;



            // 綁定點擊事件
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                Debug.Log("Working");
                btn.onClick.AddListener(() => OnClickInventoryItem(id));
            }
        }
    }
    private void OnClickInventoryItem(int id)
    {
        var inst = GetAt(id);
        if (inst is ArmorInstance ai && ai.item is Armor ar && ar.skinnedMeshRenderer != null)
        {
            Debug.Log($"Equip {ai.item.itemName}");
            boneCombiner.InstantiateEquipmentRenderer(ar.skinnedMeshRenderer);
        }
        else
        {
            Debug.Log($"Select {inst.item.itemName}");
            // 開啟詳情/比較視窗…
        }
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
