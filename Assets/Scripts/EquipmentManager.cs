using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class EquipmentSlot
{
    public GameObject equipedItem;
    public ItemType equipmentType;

    public ItemInstance item;
}


public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }
    public Transform equipmentPage;
    [SerializeField] public List<EquipmentSlot> equipmentSlots = new();
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

    public bool TryEquipFromInventory(ItemInstance item)
    {
        if (item == null || item.item == null) return false;

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            var slot = equipmentSlots[i];

            if (slot.equipmentType != item.item.type) continue;

            // 清掉舊實例
            if (slot.equipedItem) Destroy(slot.equipedItem);

            // 裝甲：生成並綁骨
            if (item.item is Armor armor && armor.skinnedMeshRenderer)
            {
                slot.equipedItem = BoneCombiner.Instance.InstantiateMesh(armor.skinnedMeshRenderer);
                if (!slot.equipedItem)
                {
                    return false; // 實例化失敗時不要當作成功
                }
                slot.item = item;

                // 若有存色彩，這裡套回去（可選）
                if (item is ArmorInstance ai && slot.equipedItem)
                {
                    var smr = slot.equipedItem.GetComponent<SkinnedMeshRenderer>();
                    if (smr)
                    {
                        var mat = smr.material; // 每件裝備獨立材質
                        if (!string.IsNullOrEmpty(ai.shaderName))
                        {
                            // 範例：依 shaderName 套回顏色
                            if (ai.shaderName.Contains("Mix 3") && ai.colors.Count >= 3)
                            {
                                mat.SetColor("_BaseColor", ai.colors[0]);
                                mat.SetColor("_Layer1Color", ai.colors[1]);
                                mat.SetColor("_Layer2Color", ai.colors[2]);
                            }
                            else if (ai.shaderName.Contains("Mix 4") && ai.colors.Count >= 4)
                            {
                                mat.SetColor("_BaseColor", ai.colors[0]);
                                mat.SetColor("_Layer1Color", ai.colors[1]);
                                mat.SetColor("_Layer2Color", ai.colors[2]);
                                mat.SetColor("_Layer3Color", ai.colors[3]);
                            }
                            else if (ai.shaderName.Contains("Mix 5") && ai.colors.Count >= 5)
                            {
                                mat.SetColor("_BaseColor", ai.colors[0]);
                                for (int k = 1; k < 5; k++) mat.SetColor($"_Layer{k}Color", ai.colors[k]);
                            }
                        }
                    }
                }
                return true; // 成功後立即回傳
            }
            return false; // 類型吻合但不是 Armor（或缺Renderer）
        }
        return false; // 找不到對應槽
    }

    public void CleanEquipmentSlot(int equipmentSlotsIndex)
    {
        EquipmentSlot slot = equipmentSlots[equipmentSlotsIndex];
        Destroy(slot.equipedItem);
        slot.equipedItem = null; 
        slot.item = null;
    }
}
