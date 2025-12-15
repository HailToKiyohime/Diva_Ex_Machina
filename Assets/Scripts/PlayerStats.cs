using System.Collections.Generic;
using UnityEngine;
using System; // 加在檔案最上面

public enum Attributes
{
    //Damage Type
    PhysicalDamage,
    ExplosionDamage,
    EnergyDamage,
    ColdDamage,
    //Defence Type
    PhysicalDefense,
    ExplosionDefense,
    EnergyDefense,
    ColdDefense,
    //Range Weapon Specific
    reloadTime,
    bulletPerShot,
    roundPerTap,
    timeBetweenShooting,
    timeBetweenShots,
    spread,
    magazineSize,
    bulletSpeed,
    FiringMode,//0:Single, 1:Auto, 2:Charge
    //Energy
    MaxEnergy,
    EnergyRegen,
    DashEnergyCost,
    FlyEnergyCost,
    //Critical Attack
    CriticalChance,
    CriticalMultiplier,
    //Movement
    SprintSpeed,
    DashSpeed,
    //Jump
    JumpHeight,
    FlyForce,
    //Health
    MaxHealth,
    //Aiming
    AimingDistance,
}

public enum AnimationType
{
    Bipedal,
    Hover,
}

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

[System.Serializable]
public class BaseStats
{

    [Header("Base Defense")]
    public float physicalDefense = 0f;
    public float explosionDefense = 0f;
    public float energyDefense = 0f;
    public float coldDefense = 0f;

    [Header("Base Critical")]
    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;

    [Header("Base Energy")]
    public float maxEnergy = 1000f;
    public float energyRegen = 50f;
    public float dashEnergyCost = 350f;
    public float flyEnergyCost = 30f;

    [Header("Base Movement")]
    public float sprintSpeed = 20f;
    public float dashSpeed = 40f;

    [Header("Base Jump / Fly")]
    public float jumpHeight = 2f;
    public float flyForce = 10f;

    [Header("Base Health")]
    public float maxHealth = 1000f;
    [Header("Base Aiming")]
    public float aimingDistance = 100f;
}


public class PlayerStats : MonoBehaviour
{
    public event Action<VisualChange> OnLegVisualChanged;
    public event Action OnHandWeaponDataChanged;
    public static PlayerStats Instance { get; private set; }

    //Buff之前全身基礎屬性 
    [Header("Base Stats (Foldout)")]
    public BaseStats baseStats = new BaseStats();
    // === 全身基礎屬性（只加「非手部裝甲」 + 武器的通用加成） ===
    [Header("Defense")]
    public float physicalDefense;
    public float explosionDefense;
    public float energyDefense;
    public float coldDefense;
    [Header("RangeWeapon")]
    public float reloadTime;
    public float bulletPerShot;
    public float roundPerTap;
    public float timeBetweenShooting;
    public float timeBetweenShots;
    public float spread;
    public float magazineSize;
    public float bulletSpeed;
    public int firingMode; //0:Single, 1:Auto, 2:Charge
    [Header("Critical")]
    public float criticalChance;
    public float criticalMultiplier;
    [Header("Energy")]
    public float maxEnergy;
    public float energyRegen;
    public float currentEnergy;
    public float dashEnergyCost;
    public float flyEnergyCost;
    [Header("Movement")]
    public float sprintSpeed;
    public float dashSpeed;
    [Header("Jump / Fly")]
    public float jumpHeight;
    public float flyForce;
    [Header("Health")]
    public float maxHealth;
    [Header("Aiming")]
    public float aimingDistance;
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

        ResetState();

        // ✅ 開場保證清空左右手狀態（避免殘留引用）
        leftHand.Reset();
        rightHand.Reset();

        // (可選) 讓監聽者立刻刷新一次
        OnHandWeaponDataChanged?.Invoke();
    }

    /// <summary>
    /// 依照目前所有 equipmentSlots 重新計算玩家數值
    /// </summary>
    public void ResetState()
    {

        physicalDefense = baseStats.physicalDefense;
        explosionDefense = baseStats.explosionDefense;
        energyDefense = baseStats.energyDefense;
        coldDefense = baseStats.coldDefense;

        criticalChance = baseStats.criticalChance;
        criticalMultiplier = baseStats.criticalMultiplier;

        maxEnergy = baseStats.maxEnergy;
        energyRegen = baseStats.energyRegen;
        dashEnergyCost = baseStats.dashEnergyCost;
        flyEnergyCost = baseStats.flyEnergyCost;

        sprintSpeed = baseStats.sprintSpeed;
        dashSpeed = baseStats.dashSpeed;

        jumpHeight = baseStats.jumpHeight;
        flyForce = baseStats.flyForce;

        maxHealth = baseStats.maxHealth;
    }
    public void RecalculateFromEquipment()
    {
        ResetState();


        // 2. 清空左右手武器狀態
        leftHand.Reset();
        rightHand.Reset();

        // reset 腿甲視覺
        CurrentLegVisual.heightOffset = 0f;
        CurrentLegVisual.animationType = AnimationType.Bipedal;
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
                // ★抓腿甲 VisualChange（LegArmor 是 Armor 的子類 :contentReference[oaicite:5]{index=5}）
                if (slot.equipmentType == ItemType.LegsArmor && armorInst.item is LegArmor leg && leg.visualChange != null)
                {
                    CurrentLegVisual.heightOffset = leg.visualChange.heightOffset;
                    CurrentLegVisual.animationType = leg.visualChange.animationType;
                }
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
            else if (inst is RangeWeaponInstance rangeWeaponInst && i == leftWeaponSlotIndex)
            {
                leftHand.weapon = rangeWeaponInst;
                AddBuffListToWeapon(leftHand, rangeWeaponInst.buffs);
            }
            else if (inst is RangeWeaponInstance rangeWeaponInst2 && i == rightWeaponSlotIndex)
            {
                rightHand.weapon = rangeWeaponInst2;
                AddBuffListToWeapon(rightHand, rangeWeaponInst2.buffs);
            }
        }
        OnLegVisualChanged?.Invoke(CurrentLegVisual);
        OnHandWeaponDataChanged?.Invoke();
    }
    void AddBuffListToWeapon(WeaponStats target, List<EquipmentBuff> buffs)
    {
        if (buffs == null) return;
        target.buffs.AddRange(buffs);
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
            case Attributes.MaxEnergy:
                maxEnergy += buff.value;
                break;
            case Attributes.EnergyRegen:
                energyRegen += buff.value;
                break;
            case Attributes.DashEnergyCost:
                dashEnergyCost += buff.value;
                break;
            case Attributes.FlyEnergyCost:
                flyEnergyCost += buff.value;
                break;
            case Attributes.CriticalChance:
                criticalChance += buff.value;
                break;
            case Attributes.CriticalMultiplier:
                criticalMultiplier += buff.value;
                break;
            case Attributes.SprintSpeed:
                sprintSpeed += buff.value;
                break;
            case Attributes.DashSpeed:
                dashSpeed += buff.value;
                break;
            case Attributes.JumpHeight:
                jumpHeight += buff.value;
                break;
            case Attributes.FlyForce:
                flyForce += buff.value;
                break;
            case Attributes.MaxHealth:
                maxHealth += buff.value;
                break;
            case Attributes.AimingDistance:
                aimingDistance += buff.value;
                break;
            // 之後如果在 Attributes 補 CriticalAttack、RecoilControl、MeleeDamage 等
            // 可以在這裡加 case，或只用 per-hand 的 WeaponStats 存就好
            default:
                break;
        }
    }
    public VisualChange CurrentLegVisual { get; private set; } = new VisualChange()
    {
        heightOffset = 0f,
        animationType = default // 依你 AnimationType 的預設值
    };
}
