using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponStats
{
    // 目前這隻手拿的武器（如果沒有就 null）
    public RangeWeaponInstance weapon;

    // 目前這隻手穿的手甲（LeftHandArmor / RightHandArmor）
    public ArmorInstance handArmor;

    // 這隻手「總共」吃到的 Buff（武器本體 + 武器零件 + 手甲）
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();

    public void Reset()
    {
        weapon = null;
        handArmor = null;
        buffs.Clear();
    }

    // 之後如果要查某一種屬性（例如 RecoilControl）可以用這個
    public float GetAttribute(Attributes attr)
    {
        float total = 0f;
        foreach (var b in buffs)
        {
            if (b.attribute == attr)
                total += b.value;
        }
        return total;
    }
}

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // === 全身基礎屬性（只加「非手部裝甲」 + 武器的通用加成） ===
    public float physicalDamage;
    public float explosionDamage;
    public float energyDamage;
    public float coldDamage;
    public float physicalDefense;
    public float explosionDefense;
    public float energyDefense;
    public float coldDefense;
    public float criticalAttack;

    [Header("手部武器狀態（只在執行時使用）")]
    public WeaponStats leftHand = new WeaponStats();
    public WeaponStats rightHand = new WeaponStats();

    [Header("equipmentSlots 中左右手武器槽的 index")]
    // 請在 Inspector 裡對應到 EquipmentManager.equipmentSlots 的順序
    public int leftWeaponSlotIndex = -1;
    public int rightWeaponSlotIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 依照目前所有 equipmentSlots 重新計算玩家數值
    /// </summary>
    public void RecalculateFromEquipment()
    {
        // 1. 清空全身屬性
        physicalDamage = 0f;
        explosionDamage = 0f;
        energyDamage = 0f;
        coldDamage = 0f;
        physicalDefense = 0f;
        explosionDefense = 0f;
        energyDefense = 0f;
        coldDefense = 0f;
        criticalAttack = 0f;

        // 2. 清空左右手武器狀態
        leftHand.Reset();
        rightHand.Reset();

        if (EquipmentManager.Instance == null ||
            EquipmentManager.Instance.equipmentSlots == null)
            return;

        List<EquipmentSlot> slots = EquipmentManager.Instance.equipmentSlots;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.item == null)
                continue;

            var inst = slot.item;

            // ===== 裝甲 =====
            if (inst is ArmorInstance armorInst)
            {
                // 左手手甲 → 只影響 leftHand，不進入全身 base stats
                if (slot.equipmentType == ItemType.LeftHandArmor)
                {
                    leftHand.handArmor = armorInst;
                    AddBuffListToWeapon(leftHand, armorInst.buffs);
                }
                // 右手手甲 → 只影響 rightHand，不進入全身 base stats
                else if (slot.equipmentType == ItemType.RightHandArmor)
                {
                    rightHand.handArmor = armorInst;
                    AddBuffListToWeapon(rightHand, armorInst.buffs);
                }
                // 其他裝甲（頭、胸、腰、腿、噴射背包...) → 加到全身屬性
                else
                {
                    ApplyBuffListToGlobal(armorInst.buffs);
                }
            }
            // ===== 遠程武器 =====
            else if (inst is RangeWeaponInstance weaponInst)
            {
                // 判斷這把武器裝在左手還是右手（靠 slot index）
                if (i == leftWeaponSlotIndex)
                {
                    leftHand.weapon = weaponInst;
                    CollectWeaponBuffsForHand(leftHand, weaponInst);
                }
                else if (i == rightWeaponSlotIndex)
                {
                    rightHand.weapon = weaponInst;
                    CollectWeaponBuffsForHand(rightHand, weaponInst);
                }

                // 武器本身和零件的 Buff 照舊加到全身屬性
                ApplyBuffListToGlobal(weaponInst.buffs);

                if (weaponInst.attachment != null)
                {
                    foreach (var part in weaponInst.attachment)
                    {
                        if (part == null) continue;
                        ApplyBuffListToGlobal(part.buffs);
                    }
                }
            }
        }

        // ======= local function: 全身屬性累積 =======
        void ApplyBuffListToGlobal(List<EquipmentBuff> buffs)
        {
            if (buffs == null) return;
            foreach (var buff in buffs)
            {
                ApplyBuffToGlobal(buff);
            }
        }

        void ApplyBuffToGlobal(EquipmentBuff buff)
        {
            switch (buff.attribute)
            {
                // Damage
                case Attributes.PhysicalDamage:
                    physicalDamage += buff.value;
                    break;
                case Attributes.ExplosionDamage:
                    explosionDamage += buff.value;
                    break;
                case Attributes.EnergyDamage:
                    energyDamage += buff.value;
                    break;
                case Attributes.ColdDamage:
                    coldDamage += buff.value;
                    break;

                // Defense
                case Attributes.PhysicalDefense:
                    physicalDefense += buff.value;
                    break;
                case Attributes.ExplosionDefense:
                    explosionDefense += buff.value;
                    break;
                case Attributes.EnergyDefense:
                    energyDefense += buff.value;
                    break;
                case Attributes.ColdDefense:
                    coldDefense += buff.value;
                    break;

                // 之後如果在 Attributes 補 CriticalAttack、RecoilControl、MeleeDamage 等
                // 可以在這裡加 case，或只用 per-hand 的 WeaponStats 存就好
                default:
                    break;
            }
        }

        // ======= local function: 把 Buff 丟進某一隻手的 WeaponStats =======
        void AddBuffListToWeapon(WeaponStats target, List<EquipmentBuff> buffs)
        {
            if (buffs == null) return;
            target.buffs.AddRange(buffs);
        }

        void CollectWeaponBuffsForHand(WeaponStats target, RangeWeaponInstance weapon)
        {
            if (weapon == null) return;

            // 武器本體 Buff
            AddBuffListToWeapon(target, weapon.buffs);

            // 零件 Buff
            if (weapon.attachment != null)
            {
                foreach (var part in weapon.attachment)
                {
                    if (part == null) continue;
                    AddBuffListToWeapon(target, part.buffs);
                }
            }
        }
    }
}
