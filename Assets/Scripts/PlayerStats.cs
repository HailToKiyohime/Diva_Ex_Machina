using System.Collections.Generic;
using UnityEngine;
using System;

public enum BuffApplyMode
{
    Add = 0,         // 加法：+value
    Multiplier = 1   // 倍率：(1 + value) 倍；例如 0.2 = +20%
}

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
    ReloadTime,
    BulletPerShot,
    RoundPerPull,
    TimeBetweenShooting,
    TimeBetweenShots,
    Spread,
    MagazineSize,
    BulletSpeed,
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
    AccelerationSpeed,
    DecelerationSpeed,
    DashSpeed,
    //Jump/Fly
    JumpHeight,
    FlySpeed,
    FlyAcceleration,
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
        float add = 0f;
        float mul = 1f;

        foreach (var b in buffs)
        {
            if (b.attribute != attr) continue;

            if (b.mode == BuffApplyMode.Add) add += b.value;
            else if (b.mode == BuffApplyMode.Multiplier) mul *= (1f + b.value);
        }

        return add * mul;
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
    public float accelerationSpeed = 30f;
    public float decelerationSpeed = 30f;
    public float dashSpeed = 40f;

    [Header("Base Jump / Fly")]
    public float jumpHeight = 2f;
    public float flySpeed = 0f;
    public float flyAcceleration = 0f;
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
    public float roundPerPull;
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
    public float accelerationSpeed;
    public float decelerationSpeed;
    public float dashSpeed;
    [Header("Jump / Fly")]
    public float jumpHeight;
    public float flySpeed;
    public float flyAcceleration;
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

    [System.Serializable]
    public struct EnergyEfficiencyInfo
    {
        // FlyEnergyCost 是「每秒」，EnergyRegen 是「每秒」
        public bool flyInfinite;
        public float flySustainSeconds;

        // DashEnergyCost 是「每次」
        public int dashCountFromFull;
        public float sustainableDashPerSecond;
    }

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
        accelerationSpeed = baseStats.accelerationSpeed;
        decelerationSpeed = baseStats.decelerationSpeed;
        dashSpeed = baseStats.dashSpeed;

        jumpHeight = baseStats.jumpHeight;
        flySpeed = baseStats.flySpeed;
        flyAcceleration = baseStats.flyAcceleration;
        maxHealth = baseStats.maxHealth;
        aimingDistance = baseStats.aimingDistance;
    }
    public void RecalculateFromEquipment()
    {
        ResetState();
        leftHand.Reset();
        rightHand.Reset();

        CurrentLegVisual.heightOffset = 0f;
        CurrentLegVisual.animationType = AnimationType.Bipedal;

        if (EquipmentManager.Instance == null ||
            EquipmentManager.Instance.equipmentSlots == null)
            return;

        // 全身（Global）與武器（Weapon）加成要分流：
        // - 全身：影響 leftStatBlock（Health/Defence/Energy/Speed）與全域 critical
        // - 武器：影響左右手武器最終屬性（包含：Head 等非手甲帶來的武器屬性）
        List<EquipmentBuff> globalBuffs = new List<EquipmentBuff>();
        List<EquipmentBuff> sharedWeaponBuffs = new List<EquipmentBuff>();

        List<EquipmentSlot> slots = EquipmentManager.Instance.equipmentSlots;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.item == null)
                continue;

            var inst = slot.item;

            if (inst is ArmorInstance armorInst)
            {
                if (slot.equipmentType == ItemType.LegsArmor && armorInst.item is LegArmor leg && leg.visualChange != null)
                {
                    CurrentLegVisual.heightOffset = leg.visualChange.heightOffset;
                    CurrentLegVisual.animationType = leg.visualChange.animationType;
                }

                if (slot.equipmentType == ItemType.LeftHandArmor)
                {
                    leftHand.handArmor = armorInst;
                    DistributeArmorBuffs(armorInst.buffs, isHandArmor: true, globalBuffs, weaponBuffs: leftHand.buffs, sharedWeaponBuffs);
                }
                else if (slot.equipmentType == ItemType.RightHandArmor)
                {
                    rightHand.handArmor = armorInst;
                    DistributeArmorBuffs(armorInst.buffs, isHandArmor: true, globalBuffs, weaponBuffs: rightHand.buffs, sharedWeaponBuffs);
                }
                else
                {
                    // 非手甲：
                    // - Global 類屬性進 globalBuffs
                    // - Weapon 類屬性（例如 Spread）進 sharedWeaponBuffs，套用到左右手武器
                    DistributeArmorBuffs(armorInst.buffs, isHandArmor: false, globalBuffs, weaponBuffs: null, sharedWeaponBuffs);
                }
            }
            else if (inst is RangeWeaponInstance rangeWeaponInst && i == leftWeaponSlotIndex)
            {
                leftHand.weapon = rangeWeaponInst;
                AddWeaponAndAttachmentBuffs(leftHand, rangeWeaponInst);
            }
            else if (inst is RangeWeaponInstance rangeWeaponInst2 && i == rightWeaponSlotIndex)
            {
                rightHand.weapon = rangeWeaponInst2;
                AddWeaponAndAttachmentBuffs(rightHand, rangeWeaponInst2);
            }
        }

        // 如果主人只拿一把武器：把另一手手甲的「武器側」buff 也加到這把武器上
        // （例：Spread 兩手疊加到唯一武器；將來手甲若有其他武器屬性也一併支援）
        if (leftHand.weapon != null && rightHand.weapon == null && rightHand.buffs.Count > 0)
        {
            leftHand.buffs.AddRange(rightHand.buffs);
        }
        else if (rightHand.weapon != null && leftHand.weapon == null && leftHand.buffs.Count > 0)
        {
            rightHand.buffs.AddRange(leftHand.buffs);
        }

        // Head / Body 等「非手甲」提供的武器屬性，對左右手武器都生效
        if (sharedWeaponBuffs.Count > 0)
        {
            leftHand.buffs.AddRange(sharedWeaponBuffs);
            rightHand.buffs.AddRange(sharedWeaponBuffs);
        }

        // ★最後統一套用（先加後乘）
        ApplyBuffListToGlobal(globalBuffs);

        OnLegVisualChanged?.Invoke(CurrentLegVisual);
        OnHandWeaponDataChanged?.Invoke();
    }
    void AddBuffListToWeapon(WeaponStats target, List<EquipmentBuff> buffs)
    {
        if (buffs == null) return;
        target.buffs.AddRange(buffs);
    }

    // ======= Buff 分流規則（Armor buffs -> Global / Weapon） =======
    private static bool IsCriticalAttribute(Attributes attr)
        => attr == Attributes.CriticalChance || attr == Attributes.CriticalMultiplier;

    private static bool IsGlobalAttribute(Attributes attr)
    {
        switch (attr)
        {
            // Defence
            case Attributes.PhysicalDefense:
            case Attributes.ExplosionDefense:
            case Attributes.EnergyDefense:
            case Attributes.ColdDefense:
            // Energy
            case Attributes.MaxEnergy:
            case Attributes.EnergyRegen:
            case Attributes.DashEnergyCost:
            case Attributes.FlyEnergyCost:
            // Movement
            case Attributes.SprintSpeed:
            case Attributes.AccelerationSpeed:
            case Attributes.DecelerationSpeed:
            case Attributes.DashSpeed:
            // Jump / Fly
            case Attributes.JumpHeight:
            case Attributes.FlySpeed:
            case Attributes.FlyAcceleration:
            // Health
            case Attributes.MaxHealth:
            // Aiming
            case Attributes.AimingDistance:
                return true;
        }
        return false;
    }

    private static bool IsWeaponAttribute(Attributes attr)
    {
        switch (attr)
        {
            // Damage Type
            case Attributes.PhysicalDamage:
            case Attributes.ExplosionDamage:
            case Attributes.EnergyDamage:
            case Attributes.ColdDamage:
            // Range Weapon Specific
            case Attributes.ReloadTime:
            case Attributes.BulletPerShot:
            case Attributes.RoundPerPull:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
            case Attributes.Spread:
            case Attributes.MagazineSize:
            case Attributes.BulletSpeed:
            case Attributes.FiringMode:
                return true;
        }
        return false;
    }

    /// <summary>
    /// 把 Armor buffs 依規則分流到：
    /// - globalBuffs：全身屬性（Health/Defence/Energy/Speed...）+ 非手甲的 Critical
    /// - weaponBuffs：手甲的武器側屬性（含手甲 Critical）
    /// - sharedWeaponBuffs：非手甲提供的武器側屬性（例如 Head 的 Spread），套用到左右手武器
    /// </summary>
    private static void DistributeArmorBuffs(
        List<EquipmentBuff> buffs,
        bool isHandArmor,
        List<EquipmentBuff> globalBuffs,
        List<EquipmentBuff> weaponBuffs,
        List<EquipmentBuff> sharedWeaponBuffs)
    {
        if (buffs == null) return;

        for (int i = 0; i < buffs.Count; i++)
        {
            var b = buffs[i];

            // Critical：依裝備位決定是否算作 Global 或 Weapon-side
            if (IsCriticalAttribute(b.attribute))
            {
                if (isHandArmor)
                {
                    if (weaponBuffs != null) weaponBuffs.Add(b);
                }
                else
                {
                    if (globalBuffs != null) globalBuffs.Add(b);
                }
                continue;
            }

            // 全身屬性：永遠進 global
            if (IsGlobalAttribute(b.attribute))
            {
                if (globalBuffs != null) globalBuffs.Add(b);
                continue;
            }

            // 武器屬性：手甲->該手；非手甲->共享（左右手都吃）
            if (IsWeaponAttribute(b.attribute))
            {
                if (isHandArmor)
                {
                    if (weaponBuffs != null) weaponBuffs.Add(b);
                }
                else
                {
                    if (sharedWeaponBuffs != null) sharedWeaponBuffs.Add(b);
                }
                continue;
            }

            // 其他未知屬性：目前不處理（避免把未定義的武器狀態誤加到 global）
        }
    }

    // ======= local function: 全身屬性累積 =======
    void ApplyBuffListToGlobal(List<EquipmentBuff> buffs)
    {
        if (buffs == null) return;

        // Pass 1: Add
        foreach (var buff in buffs)
        {
            if (buff.mode == BuffApplyMode.Add)
                ApplyAddToGlobal(buff);
        }

        // Pass 2: Multiplier
        foreach (var buff in buffs)
        {
            if (buff.mode == BuffApplyMode.Multiplier)
                ApplyMultiplierToGlobal(buff);
        }
    }
    void ApplyAddToGlobal(EquipmentBuff buff)
    {
        switch (buff.attribute)
        {
            case Attributes.PhysicalDefense: physicalDefense += buff.value; break;
            case Attributes.ExplosionDefense: explosionDefense += buff.value; break;
            case Attributes.EnergyDefense: energyDefense += buff.value; break;
            case Attributes.ColdDefense: coldDefense += buff.value; break;

            case Attributes.MaxEnergy: maxEnergy += buff.value; break;
            case Attributes.EnergyRegen: energyRegen += buff.value; break;
            case Attributes.DashEnergyCost: dashEnergyCost += buff.value; break;
            case Attributes.FlyEnergyCost: flyEnergyCost += buff.value; break;

            case Attributes.CriticalChance: criticalChance += buff.value; break;
            case Attributes.CriticalMultiplier: criticalMultiplier += buff.value; break;

            case Attributes.SprintSpeed: sprintSpeed += buff.value; break;
            case Attributes.AccelerationSpeed: accelerationSpeed += buff.value; break;
            case Attributes.DecelerationSpeed: decelerationSpeed += buff.value; break;
            case Attributes.DashSpeed: dashSpeed += buff.value; break;

            case Attributes.JumpHeight: jumpHeight += buff.value; break;
            case Attributes.FlySpeed: flySpeed += buff.value; break;
            case Attributes.FlyAcceleration: flyAcceleration += buff.value; break;

            case Attributes.MaxHealth: maxHealth += buff.value; break;
            case Attributes.AimingDistance: aimingDistance += buff.value; break;
        }
    }
    void ApplyMultiplierToGlobal(EquipmentBuff buff)
    {
        float m = 1f + buff.value; // 0.2 => 1.2倍；-0.1 => 0.9倍
        switch (buff.attribute)
        {
            case Attributes.PhysicalDefense: physicalDefense *= m; break;
            case Attributes.ExplosionDefense: explosionDefense *= m; break;
            case Attributes.EnergyDefense: energyDefense *= m; break;
            case Attributes.ColdDefense: coldDefense *= m; break;

            case Attributes.MaxEnergy: maxEnergy *= m; break;
            case Attributes.EnergyRegen: energyRegen *= m; break;
            case Attributes.DashEnergyCost: dashEnergyCost *= m; break;
            case Attributes.FlyEnergyCost: flyEnergyCost *= m; break;

            case Attributes.CriticalChance: criticalChance *= m; break;
            case Attributes.CriticalMultiplier: criticalMultiplier *= m; break;

            case Attributes.SprintSpeed: sprintSpeed *= m; break;
            case Attributes.AccelerationSpeed: accelerationSpeed *= m; break;
            case Attributes.DecelerationSpeed: decelerationSpeed *= m; break;
            case Attributes.DashSpeed: dashSpeed *= m; break;

            case Attributes.JumpHeight: jumpHeight *= m; break;
            case Attributes.FlySpeed: flySpeed *= m; break;
            case Attributes.FlyAcceleration: flyAcceleration *= m; break;

            case Attributes.MaxHealth: maxHealth *= m; break;
            case Attributes.AimingDistance: aimingDistance *= m; break;
        }
    }

    // ======= Equipment Stat Block（計算用：先把數值算穩） =======
    public int GetDisplayHealth() => Mathf.RoundToInt(maxHealth);

    public int GetDisplayDefenceAverage()
    {
        float avg = (physicalDefense + explosionDefense + energyDefense + coldDefense) / 4f;
        return Mathf.RoundToInt(avg);
    }

    public int GetDisplaySpeed() => Mathf.RoundToInt(sprintSpeed);

    /// <summary>
    /// Energy Efficiency：同時考慮 Fly（每秒）與 Dash（每次）兩種成本。
    /// - Fly：以「可連續飛行的秒數」呈現（若 regen >= cost 則為無限）
    /// - Dash：
    ///   1) 滿能量可 dash 幾次
    ///   2) 以 regen 可支撐的 dash/秒
    /// </summary>
    public EnergyEfficiencyInfo GetEnergyEfficiencyInfo()
    {
        var info = new EnergyEfficiencyInfo();

        // Fly（每秒）
        float netFlyDrain = flyEnergyCost - energyRegen;
        if (netFlyDrain <= 0f)
        {
            info.flyInfinite = true;
            info.flySustainSeconds = float.PositiveInfinity;
        }
        else
        {
            info.flyInfinite = false;
            info.flySustainSeconds = (maxEnergy <= 0f) ? 0f : (maxEnergy / netFlyDrain);
        }

        // Dash（每次）
        if (dashEnergyCost <= 0f)
        {
            info.dashCountFromFull = int.MaxValue;
            info.sustainableDashPerSecond = float.PositiveInfinity;
        }
        else
        {
            info.dashCountFromFull = Mathf.Max(0, Mathf.FloorToInt(maxEnergy / dashEnergyCost));
            info.sustainableDashPerSecond = Mathf.Max(0f, energyRegen / dashEnergyCost);
        }

        return info;
    }

    public int GetDisplayLhAttack() => Mathf.RoundToInt(GetHandExpectedDps(isLeftHand: true));
    public int GetDisplayRhAttack() => Mathf.RoundToInt(GetHandExpectedDps(isLeftHand: false));

    public float GetHandExpectedDps(bool isLeftHand)
    {
        var hand = isLeftHand ? leftHand : rightHand;
        if (hand == null || hand.weapon == null) return 0f;

        float baseDamage =
            hand.GetAttribute(Attributes.PhysicalDamage) +
            hand.GetAttribute(Attributes.ExplosionDamage) +
            hand.GetAttribute(Attributes.EnergyDamage) +
            hand.GetAttribute(Attributes.ColdDamage);

        if (baseDamage <= 0f) return 0f;

        float mergedCritChance = Mathf.Clamp01(ApplyHandBuffsToBase(criticalChance, hand, Attributes.CriticalChance));
        float mergedCritMultiplier = ApplyHandBuffsToBase(criticalMultiplier, hand, Attributes.CriticalMultiplier);
        if (mergedCritMultiplier < 1f) mergedCritMultiplier = 1f;

        // 注意：BulletPerShot / RoundPerPull 若算出 0，顯示與計算都要視為 x1
        int bulletPerShotInt = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.BulletPerShot)));
        int roundPerPullInt = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.RoundPerPull)));

        float timeBetweenShootingFinal = hand.GetAttribute(Attributes.TimeBetweenShooting);
        if (timeBetweenShootingFinal <= 0.0001f) return 0f;

        float expectedPerTrigger =
            (1f - mergedCritChance) * baseDamage +
            mergedCritChance * (mergedCritMultiplier * baseDamage);

        float shotsPerSecond = 1f / timeBetweenShootingFinal;
        return expectedPerTrigger * bulletPerShotInt * roundPerPullInt * shotsPerSecond;
    }

    private static float ApplyHandBuffsToBase(float baseValue, WeaponStats hand, Attributes attr)
    {
        if (hand == null || hand.buffs == null || hand.buffs.Count == 0) return baseValue;

        float add = 0f;
        float mul = 1f;

        for (int i = 0; i < hand.buffs.Count; i++)
        {
            var b = hand.buffs[i];
            if (b.attribute != attr) continue;

            if (b.mode == BuffApplyMode.Add) add += b.value;
            else if (b.mode == BuffApplyMode.Multiplier) mul *= (1f + b.value);
        }

        return (baseValue + add) * mul;
    }

    public VisualChange CurrentLegVisual { get; private set; } = new VisualChange()
    {
        heightOffset = 0f,
        animationType = default // 依你 AnimationType 的預設值
    };

    private void AddWeaponAndAttachmentBuffs(WeaponStats target, RangeWeaponInstance weaponInst)
    {
        if (weaponInst == null) return;

        // 武器本體 buffs
        AddBuffListToWeapon(target, weaponInst.buffs);

        // 零件 buffs（attachment 裡每個 PartInstance）
        if (weaponInst.attachment == null) return;

        foreach (var part in weaponInst.attachment)
        {
            // 通常 item == null 代表該槽沒裝零件；也避免殘留 buffs 造成幽靈加成
            if (part == null || part.item == null) continue;

            AddBuffListToWeapon(target, part.buffs);
        }
    }
}
