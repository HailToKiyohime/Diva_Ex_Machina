using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static Unity.Burst.Intrinsics.Arm;

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
    public bool TryEquipWeaponFromInventory(ItemInstance item, Transform mountPoint, int slotIndex)
    {
        if (item == null || item.item == null) return false;

        if (slotIndex < 0 || slotIndex >= equipmentSlots.Count)
        {
            Debug.LogWarning($"TryEquipWeaponFromInventory: invalid slot index {slotIndex}");
            return false;
        }

        var slot = equipmentSlots[slotIndex];

        if (slot.equipmentType != item.item.type)
        {
            Debug.LogWarning(
                $"TryEquipWeaponFromInventory: slot[{slotIndex}].equipmentType = {slot.equipmentType}, item.type = {item.item.type}"
            );
            return false;
        }

        // 清掉這一格原本的武器
        if (slot.equipedItem) Destroy(slot.equipedItem);

        // 只處理 RangeWeaponInstance
        if (item is not RangeWeaponInstance rwi || rwi.item is not RangeWeapon rw)
        {
            Debug.LogWarning("TryEquipWeaponFromInventory: item is not RangeWeaponInstance/RangeWeapon");
            return false;
        }

        // 1) 主武器掛在 mountPoint（場景物件）
        slot.equipedItem = Instantiate(rw.weaponPrefab, mountPoint, false);
        slot.equipedItem.transform.localRotation = Quaternion.Euler(new Vector3(-90, 90, 0));
        slot.item = item;

        // 1-1) 把 RangeWeaponInstance 上存的顏色套回去
        if (rwi.colors != null && rwi.colors.Count > 0 && !string.IsNullOrEmpty(rwi.shaderName))
        {
            var rend = slot.equipedItem.GetComponentInChildren<Renderer>();
            if (rend)
            {
                var mat = rend.material; // 實例材質
                if (rwi.shaderName.Contains("Mix 3") && rwi.colors.Count >= 3)
                {
                    mat.SetColor("_BaseColor", rwi.colors[0]);
                    mat.SetColor("_Layer1Color", rwi.colors[1]);
                    mat.SetColor("_Layer2Color", rwi.colors[2]);
                }
                else if (rwi.shaderName.Contains("Mix 4") && rwi.colors.Count >= 4)
                {
                    mat.SetColor("_BaseColor", rwi.colors[0]);
                    mat.SetColor("_Layer1Color", rwi.colors[1]);
                    mat.SetColor("_Layer2Color", rwi.colors[2]);
                    mat.SetColor("_Layer3Color", rwi.colors[3]);
                }
                else if (rwi.shaderName.Contains("Mix 5") && rwi.colors.Count >= 5)
                {
                    mat.SetColor("_BaseColor", rwi.colors[0]);
                    for (int k = 1; k < 5; k++)
                        mat.SetColor($"_Layer{k}Color", rwi.colors[k]);
                }
            }
        }

        // 2) 附件掛在「主武器實例」上的同名掛點，並套回顏色
        if (rwi.attachment != null)
        {
            foreach (var attach in rwi.attachment)
            {
                if (attach?.item is not RangeWeaponPart rwp) continue;

                foreach (var ap in rw.attachmentPoints)
                {
                    if (ap.allowPart != attach.partType) continue;

                    var target = FindChildRecursive(slot.equipedItem.transform, ap.pointTransform.name);
                    if (target == null)
                    {
                        Debug.LogWarning($"Equip: cannot find mount '{ap.pointTransform.name}' on weapon instance");
                        continue;
                    }

                    var part = Instantiate(rwp.rangeWeaponPartPrefab, target, false);
                    part.transform.localPosition = Vector3.zero;
                    part.transform.localRotation = Quaternion.identity;
                    part.transform.localScale = Vector3.one;

                    if (attach.colors != null && attach.colors.Count > 0 && !string.IsNullOrEmpty(attach.shaderName))
                    {
                        var partRend = part.GetComponentInChildren<Renderer>();
                        if (partRend)
                        {
                            var mat = partRend.material;
                            if (attach.shaderName.Contains("Mix 3") && attach.colors.Count >= 3)
                            {
                                mat.SetColor("_BaseColor", attach.colors[0]);
                                mat.SetColor("_Layer1Color", attach.colors[1]);
                                mat.SetColor("_Layer2Color", attach.colors[2]);
                            }
                            else if (attach.shaderName.Contains("Mix 4") && attach.colors.Count >= 4)
                            {
                                mat.SetColor("_BaseColor", attach.colors[0]);
                                mat.SetColor("_Layer1Color", attach.colors[1]);
                                mat.SetColor("_Layer2Color", attach.colors[2]);
                                mat.SetColor("_Layer3Color", attach.colors[3]);
                            }
                            else if (attach.shaderName.Contains("Mix 5") && attach.colors.Count >= 5)
                            {
                                mat.SetColor("_BaseColor", attach.colors[0]);
                                for (int k = 1; k < 5; k++)
                                    mat.SetColor($"_Layer{k}Color", attach.colors[k]);
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"Equipped weapon '{rw.itemName}' to slot index {slotIndex}");
        return true;
    }


    public void CleanEquipmentSlot(int equipmentSlotsIndex)
    {
        EquipmentSlot slot = equipmentSlots[equipmentSlotsIndex];
        Destroy(slot.equipedItem);
        slot.equipedItem = null; 
        slot.item = null;
    }
    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = FindChildRecursive(root.GetChild(i), name);
            if (c) return c;
        }
        return null;
    }
}

