using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EquipmentSlot
{
    public GameObject equipedItem;
    public ItemInstance item;
}


public class EquipmentManager : MonoBehaviour
{
    [SerializeReference] public List<EquipmentSlot> equipmentSlots = new();
    public BoneCombiner boneCombiner;
    public Transform EquipmentPage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        CreateEquipmentSlot();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void CreateEquipmentSlot()
    {
        equipmentSlots.Clear();

        if (EquipmentPage == null) return;

        for (int i = 0; i < EquipmentPage.childCount; i++)
        {
            // 若需要：確保每個子物件有 ButtonID
            var child = EquipmentPage.GetChild(i).gameObject;
            var buttonID = child.GetComponent<ButtonID>() ?? child.AddComponent<ButtonID>();
            buttonID.ID = i;

            // ★ 加入實例，而不是 null
            equipmentSlots.Add(new EquipmentSlot());
        }
    }

    public bool TryEquipFromInventory(InventoryManager inv, int invIndex, int slotId)
    {
        // 邊界檢查
        if (invIndex < 0 || invIndex >= inv.slots.Count) return false;
        if (slotId < 0 || slotId >= equipmentSlots.Count) return false;

        var inst = inv.slots[invIndex];
        if (inst == null || inst.item == null) return false;

        // 外觀：只示範 Armor（其他型別自行擴充）
        if (inst is ArmorInstance ai && ai.item is Armor armor)
        {
            // 先把舊裝備（若有）丟回背包
            if (equipmentSlots[slotId].item != null)
            {
                inv.AddInstance(equipmentSlots[slotId].item);  // 回背包
                                                               // 卸下舊外觀
                if (equipmentSlots[slotId].equipedItem)
                    Destroy(equipmentSlots[slotId].equipedItem);
            }


            // 設置新裝備（把 ItemInstance 轉移到裝備槽）
            equipmentSlots[slotId].item = inst;
            equipmentSlots[slotId].equipedItem =
                boneCombiner.InstantiateEquipmentRenderer(armor.skinnedMeshRenderer, ai.colors);

            if (equipmentSlots[slotId].item.item.type == ItemType.LegsArmor)
            {
                boneCombiner.HideLegs();
            }
            return true;
        }

        // 非 Armor 的處理…（依需求擴充）
        return false;
    }

    public void UnequipItem(int buttonID)
    {
        if (buttonID < 0 || buttonID >= equipmentSlots.Count) return;
        var slot = equipmentSlots[buttonID];

        if (slot.equipedItem) Destroy(slot.equipedItem);
        slot.equipedItem = null;

        if (slot.item != null)
        {
            // 丟回背包並更新 buckets
            InventoryManager.Instance.AddInstance(slot.item);
            InventoryManager.Instance.OpenPartsInventory(slot.item.item.type);
            if (slot.item.item.type == ItemType.LegsArmor)
            {
                boneCombiner.ShowLegs();
            }
            slot.item = null;
        }
    }
}
