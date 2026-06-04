using System.Collections.Generic;
using UnityEngine;
using System;

public enum BuffApplyMode
{
    Add = 0,         // 加法：+value
    Multiplier = 1   // 倍率：(1 + value) 倍；例如 0.2 = +20%
}

public enum FiringMode
{
    Salvo,
    ShootingInTurn
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
    LockOnRange,
    AimingDistance,
    //Weight,
    Weight,
    //Melee Weapon Specific
    MeleeOutput,
    MeleeSpeed,
    MeleeDashDistance,
    MeleeReloadTime,
    //Auto Aim Speed
    AutoAimSpeed,// The speed of auto-aiming towards the target, degrees per second
}

public enum AnimationType
{
    Bipedal,
    Hover,
}

public enum HandWeaponKind
{
    None = 0,
    Range = 1,
    Melee = 2
}
public enum ShoulderWeaponKind
{
    None = 0,
    Range = 1,
}


[System.Serializable]
public class WeaponStats
{
    // 目前這隻手拿的武器種類（None / Range / Melee）
    public HandWeaponKind weaponKind = HandWeaponKind.None;

    // 目前這隻手拿的遠程武器（如果沒有就 null）
    // 注意：目前為了保持與 AttackManager 的相容性，近戰武器時會保持 weapon == null
    public RangeWeaponInstance rangeweapon;

    // 目前這隻手拿的近戰武器（如果沒有就 null）
    public MeleeWeaponInstance meleeWeapon;

    // 目前這隻手穿的手甲（LeftHandArmor / RightHandArmor）
    public ArmorInstance handArmor;

    // 這隻手「總共」吃到的 Buff（武器本體 + 武器零件 + 手甲）
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();


    public bool HasWeapon => rangeweapon != null || meleeWeapon != null;

    public void Reset()
    {
        weaponKind = HandWeaponKind.None;
        rangeweapon = null;
        meleeWeapon = null;
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
public class ShoulderWeaponStats
{
    // 目前這隻手拿的武器種類（None / Range / Melee）
    public ShoulderWeaponKind weaponKind = ShoulderWeaponKind.None;

    // 目前這隻手拿的遠程武器（如果沒有就 null）
    public ShoulderWeaponInstance shoulderweapon;

    // 總共吃到的 Buff
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();


    public bool HasWeapon => shoulderweapon != null;

    public void Reset()
    {
        weaponKind = ShoulderWeaponKind.None;
        shoulderweapon = null;
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

    [Header("Base Melee")]
    public float meleeOutput = 1f; //The final damage is calculated by multiplying this value with weapon damage
    public float meleeSpeed = 1f;//The final attack speed is calculated by multiplying this value with weapon attack speed
    public float meleeDashDistance = 5f; //The distance covered during a melee dash attack
    public float meleeReloadTime = 0.75f; //Cooldown after melee dash/attack
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
    public float lockOnRange = 300f;
    public float aimingDistance = 50f;
    public float autoAimSpeed = 60f; // The speed of auto-aiming towards the target, degrees per second
}


public class PlayerStats : MonoBehaviour
{
    public event Action<VisualChange> OnLegVisualChanged;
    public event Action<Vector3> OnThrusterFlameOffsetChanged;

    public event Action<Thruster> OnThrusterVisualChanged;
    public Thruster CurrentThruster { get; private set; }
    public Vector3 CurrentThrusterFlameOffset { get; private set; } = Vector3.zero;
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
    [Header("MeleeWeapon")]
    public float meleeOutput;
    public float meleeSpeed;
    public float meleeDashDistance;
    public float meleeReloadTime;
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
    public float currentHealth;
    [Header("Aiming")]
    public float lockOnRange;
    public float aimingDistance;
    public float autoAimSpeed;
    [Header("手部武器狀態（只在執行時使用）")]
    public WeaponStats leftHand = new WeaponStats();
    public WeaponStats rightHand = new WeaponStats();
    [Header("肩膀武器狀態（只在執行時使用）")]
    public ShoulderWeaponStats leftShoulder = new ShoulderWeaponStats();
    public ShoulderWeaponStats rightShoulder = new ShoulderWeaponStats();


    [Header("equipmentSlots 中左右手武器槽的 index")]
    // 請在 Inspector 裡對應到 EquipmentManager.equipmentSlots 的順序
    public int leftWeaponSlotIndex = -1;
    public int rightWeaponSlotIndex = -1;
    [Header("equipmentSlots 中左右肩膀武器槽的 index")]
    public int leftShoulderWeaponSlotIndex = -1;
    public int rightShoulderWeaponSlotIndex = -1;

    // =============================
    // Equipment Stat Block (UI) API
    // =============================
    [Serializable]
    public struct EnergyEfficiencyInfo
    {
        public bool flyInfinite;
        public float flySustainSeconds;
        public int dashCountFromFull;
        public float sustainableDashPerSecond;
    }
    public int GetDisplayHealth() => Mathf.RoundToInt(maxHealth);

    public int GetDisplayDefenceAverage()
    {
        float avg = (physicalDefense + explosionDefense + energyDefense + coldDefense) / 4f;
        return Mathf.RoundToInt(avg);
    }

    public float GetDisplayJumpHeight() => jumpHeight;
    public int GetDisplayMaxEnergy() => Mathf.RoundToInt(maxEnergy);
    public float GetDisplayEnergyRegen() => energyRegen;
    public float GetDisplayFlySpeed() => flySpeed;
    public float GetDisplayDashSpeed() => dashSpeed;
    public float GetDisplaySprintSpeed() => sprintSpeed;

    /// <summary>
    /// 主人定義的期望 DPS（不含 Spread/命中率）。
    /// critical = global(base+全身裝備) + weapon-side(武器/零件/手甲) 合併。
    /// </summary>
    public float GetHandExpectedDps(bool isLeftHand)
    {
        WeaponStats hand = isLeftHand ? leftHand : rightHand;
        if (hand == null || hand.rangeweapon == null) return 0f;

        float D =
            hand.GetAttribute(Attributes.PhysicalDamage) +
            hand.GetAttribute(Attributes.ExplosionDamage) +
            hand.GetAttribute(Attributes.EnergyDamage) +
            hand.GetAttribute(Attributes.ColdDamage);

        // 顆數 / 次數：至少 1（語意上不能是 0）
        int bulletPerShotVal = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.BulletPerShot)));
        int roundPerPullVal = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.RoundPerPull)));

        float tbs = hand.GetAttribute(Attributes.TimeBetweenShooting);
        if (tbs <= 0.00001f) return 0f;
        float shotsPerSecond = 1f / tbs;

        float cc = GetMergedCriticalChance(hand);
        float cm = GetMergedCriticalMultiplier(hand);

        float expectedShot = (1f - cc) * D + cc * (cm * D);
        return expectedShot * bulletPerShotVal * roundPerPullVal * shotsPerSecond;
    }

    public int GetDisplayLhAttack() => Mathf.RoundToInt(GetHandExpectedDps(true));
    public int GetDisplayRhAttack() => Mathf.RoundToInt(GetHandExpectedDps(false));

    public float GetLockedOnRange() => lockOnRange / 2;

    public float GetAimingDistance() => aimingDistance;

    public float GetAutoAimSpeed() => autoAimSpeed;
    /// <summary>
    /// 主人先前定義的 Energy Efficiency（保留 API，以後若要用可直接接 UI）。
    /// FlyCost: 每秒；DashCost: 每次。
    /// </summary>
    public EnergyEfficiencyInfo GetEnergyEfficiencyInfo()
    {
        EnergyEfficiencyInfo info = new EnergyEfficiencyInfo();

        // Fly：每秒成本
        float netFlyDrain = flyEnergyCost - energyRegen;
        if (netFlyDrain <= 0f)
        {
            info.flyInfinite = true;
            info.flySustainSeconds = float.PositiveInfinity;
        }
        else
        {
            info.flyInfinite = false;
            info.flySustainSeconds = maxEnergy / netFlyDrain;
        }

        // Dash：每次成本
        if (dashEnergyCost <= 0f)
        {
            info.dashCountFromFull = int.MaxValue;
            info.sustainableDashPerSecond = float.PositiveInfinity;
        }
        else
        {
            info.dashCountFromFull = Mathf.FloorToInt(maxEnergy / dashEnergyCost);
            info.sustainableDashPerSecond = energyRegen / dashEnergyCost;
        }

        return info;
    }

    private float GetMergedCriticalChance(WeaponStats hand)
    {
        float baseCc = criticalChance;
        float add = 0f;
        float mul = 1f;
        if (hand?.buffs != null)
        {
            foreach (var b in hand.buffs)
            {
                if (b.attribute != Attributes.CriticalChance) continue;
                if (b.mode == BuffApplyMode.Add) add += b.value;
                else if (b.mode == BuffApplyMode.Multiplier) mul *= (1f + b.value);
            }
        }
        float cc = (baseCc + add) * mul;
        return Mathf.Clamp01(cc);
    }

    private float GetMergedCriticalMultiplier(WeaponStats hand)
    {
        float baseCm = criticalMultiplier;
        float add = 0f;
        float mul = 1f;
        if (hand?.buffs != null)
        {
            foreach (var b in hand.buffs)
            {
                if (b.attribute != Attributes.CriticalMultiplier) continue;
                if (b.mode == BuffApplyMode.Add) add += b.value;
                else if (b.mode == BuffApplyMode.Multiplier) mul *= (1f + b.value);
            }
        }
        float cm = (baseCm + add) * mul;
        if (cm < 1f) cm = 1f;
        return cm;
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
        currentEnergy = maxEnergy; // 開場補滿能量
        currentHealth = maxHealth;   // 開場補滿血量  
        // ✅ 開場保證清空左右手和肩膀狀態（避免殘留引用）
        leftHand.Reset();
        rightHand.Reset();
        leftShoulder.Reset();
        rightShoulder.Reset();

        // (可選) 讓監聽者立刻刷新一次
        OnHandWeaponDataChanged?.Invoke();

        PlayerAiming.Instance?.SetAimAreaSize(lockOnRange);
        PlayerAiming.Instance?.SetLockOnDistance(aimingDistance);
        UIManager.Instance?.SetAmmoBarSize(lockOnRange);
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
        meleeDashDistance = baseStats.meleeDashDistance;
        meleeReloadTime = baseStats.meleeReloadTime;

        jumpHeight = baseStats.jumpHeight;
        flySpeed = baseStats.flySpeed;
        flyAcceleration = baseStats.flyAcceleration;
        maxHealth = baseStats.maxHealth;
        lockOnRange = baseStats.lockOnRange;
        autoAimSpeed = baseStats.autoAimSpeed;
        aimingDistance = baseStats.aimingDistance;
    }
    public void RecalculateFromEquipment()
    {
        ResetState();
        leftHand.Reset();
        rightHand.Reset();
        leftShoulder.Reset();
        rightShoulder.Reset();

        CurrentLegVisual.heightOffset = 0f;
        CurrentLegVisual.animationType = AnimationType.Bipedal;

        if (EquipmentManager.Instance == null ||
            EquipmentManager.Instance.equipmentSlots == null)
            return;

        // Buffs 分流：
        // - globalBuffs：影響全身（左欄）
        // - sharedWeaponBuffs：由「非手甲裝備」提供，影響左右手武器最終屬性（如 HeadArmor 的 spread/crit 等）
        // - left/rightHandArmorWeaponBuffs：由手甲提供，影響該手武器最終屬性（單持時兩手疊加到唯一武器）
        List<EquipmentBuff> globalBuffs = new List<EquipmentBuff>();
        List<EquipmentBuff> sharedWeaponBuffs = new List<EquipmentBuff>();
        List<EquipmentBuff> leftHandArmorWeaponBuffs = new List<EquipmentBuff>();
        List<EquipmentBuff> rightHandArmorWeaponBuffs = new List<EquipmentBuff>();
        List<EquipmentBuff> leftShoulderWeaponBuffs = new List<EquipmentBuff>();
        List<EquipmentBuff> rightShoulderWeaponBuffs = new List<EquipmentBuff>();

        Vector3 thrusterOffset = Vector3.zero;
        bool hasThruster = false;

        List<EquipmentSlot> slots = EquipmentManager.Instance.equipmentSlots;
        Thruster foundThruster = null;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.item == null)
                continue;

            var inst = slot.item;

            if (inst is ArmorInstance armorInst)
            {
                if (armorInst.item is Thruster thr)
                {
                    foundThruster = thr;                 // ★記住這顆 Thruster（prefab 也在裡面）
                    thrusterOffset = thr.thrusterFlameOffset;
                    hasThruster = true;
                }

                if (slot.equipmentType == ItemType.LegsArmor && armorInst.item is LegArmor leg && leg.visualChange != null)
                {
                    CurrentLegVisual.heightOffset = leg.visualChange.heightOffset;
                    CurrentLegVisual.animationType = leg.visualChange.animationType;
                }

                // 依裝備種類分流 buffs
                if (armorInst.buffs != null)
                {
                    if (slot.equipmentType == ItemType.LeftHandArmor)
                    {
                        leftHand.handArmor = armorInst;
                        SplitArmorBuffsToGlobalAndWeapon(armorInst.buffs, globalBuffs, leftHandArmorWeaponBuffs);
                    }
                    else if (slot.equipmentType == ItemType.RightHandArmor)
                    {
                        rightHand.handArmor = armorInst;
                        SplitArmorBuffsToGlobalAndWeapon(armorInst.buffs, globalBuffs, rightHandArmorWeaponBuffs);
                    }
                    else
                    {
                        // 非手甲裝備：global 既影響左欄，也要參與左右手武器最終屬性
                        // 我們以「武器側屬性」分流到 sharedWeaponBuffs，其餘進 globalBuffs
                        SplitArmorBuffsToGlobalAndWeapon(armorInst.buffs, globalBuffs, sharedWeaponBuffs);
                    }
                }
            }
            else if (i == leftWeaponSlotIndex)
            {
                if (inst is RangeWeaponInstance rangeWeaponInst)
                {
                    leftHand.weaponKind = HandWeaponKind.Range;
                    leftHand.rangeweapon = rangeWeaponInst;
                    leftHand.meleeWeapon = null;
                    AddWeaponAndAttachmentBuffs(leftHand, rangeWeaponInst);
                }
                else if (inst is MeleeWeaponInstance meleeWeaponInst)
                {
                    leftHand.weaponKind = HandWeaponKind.Melee;
                    leftHand.meleeWeapon = meleeWeaponInst;
                    leftHand.rangeweapon = null; // 保持與 AttackManager 相容：近戰時讓 ranged weapon 為 null
                    AddWeaponAndAttachmentBuffs(leftHand, meleeWeaponInst);
                }
            }
            else if (i == rightWeaponSlotIndex)
            {
                if (inst is RangeWeaponInstance rangeWeaponInst2)
                {
                    rightHand.weaponKind = HandWeaponKind.Range;
                    rightHand.rangeweapon = rangeWeaponInst2;
                    rightHand.meleeWeapon = null;
                    AddWeaponAndAttachmentBuffs(rightHand, rangeWeaponInst2);
                }
                else if (inst is MeleeWeaponInstance meleeWeaponInst2)
                {
                    rightHand.weaponKind = HandWeaponKind.Melee;
                    rightHand.meleeWeapon = meleeWeaponInst2;
                    rightHand.rangeweapon = null; // 保持與 AttackManager 相容：近戰時讓 ranged weapon 為 null
                    AddWeaponAndAttachmentBuffs(rightHand, meleeWeaponInst2);
                }
            }
            else if (i == leftShoulderWeaponSlotIndex)
            {
                if (inst is ShoulderWeaponInstance shoulderWeaponInst)
                {
                    leftShoulder.weaponKind = ShoulderWeaponKind.Range;
                    leftShoulder.shoulderweapon = shoulderWeaponInst;
                    AddShoulderWeaponAndAttachmentBuffs(leftShoulder, shoulderWeaponInst);
                }
            }
            else if (i == rightShoulderWeaponSlotIndex)
            {
                if (inst is ShoulderWeaponInstance shoulderWeaponInst)
                {
                    rightShoulder.weaponKind = ShoulderWeaponKind.Range;
                    rightShoulder.shoulderweapon = shoulderWeaponInst;
                    AddShoulderWeaponAndAttachmentBuffs(rightShoulder, shoulderWeaponInst);
                }
            }
        }

        // ★最後統一套用（先加後乘）— 全身屬性
        ApplyBuffListToGlobal(globalBuffs);

        // ===== 武器側 buffs 組裝（先把武器/零件的 buffs 留在 leftHand/rightHand 內） =====
        // 1) sharedWeaponBuffs（如 HeadArmor 的 spread/crit）永遠套用到左右手武器
        AddBuffListToWeapon(leftHand, sharedWeaponBuffs);
        AddBuffListToWeapon(rightHand, sharedWeaponBuffs);

        // 2) 手甲武器側 buffs：各手各吃一份
        AddBuffListToWeapon(leftHand, leftHandArmorWeaponBuffs);
        AddBuffListToWeapon(rightHand, rightHandArmorWeaponBuffs);

        // 3) 單持規則：若只有一把武器，另一手手甲的武器側 buffs 也要疊加到唯一武器
        bool hasLeftWeapon = leftHand.HasWeapon;
        bool hasRightWeapon = rightHand.HasWeapon;
        if (hasLeftWeapon && !hasRightWeapon)
        {
            AddBuffListToWeapon(leftHand, rightHandArmorWeaponBuffs);
        }
        else if (!hasLeftWeapon && hasRightWeapon)
        {
            AddBuffListToWeapon(rightHand, leftHandArmorWeaponBuffs);
        }

        CurrentThrusterFlameOffset = hasThruster ? thrusterOffset : Vector3.zero;
        OnThrusterFlameOffsetChanged?.Invoke(CurrentThrusterFlameOffset);
        if (CurrentThruster != foundThruster)
        {
            CurrentThruster = foundThruster; // null 代表沒裝（卸下）
            OnThrusterVisualChanged?.Invoke(CurrentThruster);
        }

        OnLegVisualChanged?.Invoke(CurrentLegVisual);
        OnHandWeaponDataChanged?.Invoke();
        PlayerAiming.Instance?.SetAimAreaSize(lockOnRange);
        PlayerAiming.Instance?.SetLockOnDistance(aimingDistance);
        UIManager.Instance?.SetAmmoBarSize(lockOnRange);
    }

    // ======= helpers: buff classification / splitting =======
    private static void SplitArmorBuffsToGlobalAndWeapon(List<EquipmentBuff> src, List<EquipmentBuff> toGlobal, List<EquipmentBuff> toWeapon)
    {
        if (src == null) return;
        foreach (var b in src)
        {
            if (IsWeaponSideAttribute(b.attribute))
                toWeapon.Add(b);
            else
                toGlobal.Add(b);
        }
    }

    private static bool IsWeaponSideAttribute(Attributes attr)
    {
        // 這些屬性屬於「武器最終屬性」：由武器/零件/手甲/頭盔等影響，並且不應污染左欄全身 stats。
        switch (attr)
        {
            case Attributes.PhysicalDamage:
            case Attributes.ExplosionDamage:
            case Attributes.EnergyDamage:
            case Attributes.ColdDamage:
            case Attributes.MeleeOutput:
            case Attributes.MeleeSpeed:
            case Attributes.ReloadTime:
            case Attributes.BulletPerShot:
            case Attributes.RoundPerPull:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
            case Attributes.Spread:
            case Attributes.MagazineSize:
            case Attributes.BulletSpeed:
            case Attributes.FiringMode:
            case Attributes.CriticalChance:
            case Attributes.CriticalMultiplier:
            case Attributes.MeleeDashDistance:
            case Attributes.MeleeReloadTime:
                return true;
            default:
                return false;
        }
    }
    void AddBuffListToWeapon(WeaponStats target, List<EquipmentBuff> buffs)
    {
        if (buffs == null) return;
        target.buffs.AddRange(buffs);
    }
    void AddBuffListToWeapon(ShoulderWeaponStats target, List<EquipmentBuff> buffs)
    {
        if (buffs == null) return;
        target.buffs.AddRange(buffs);
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

    // ======= helpers: apply buffs to a single scalar (keep buff math inside PlayerStats) =======
    private static float ApplyBuffListToValue(List<EquipmentBuff> buffs, Attributes attr, float baseValue)
    {
        if (buffs == null) return baseValue;

        float v = baseValue;

        // Pass 1: Add
        for (int i = 0; i < buffs.Count; i++)
        {
            var b = buffs[i];
            if (b.attribute != attr) continue;
            if (b.mode == BuffApplyMode.Add) v += b.value;
        }

        // Pass 2: Multiplier
        for (int i = 0; i < buffs.Count; i++)
        {
            var b = buffs[i];
            if (b.attribute != attr) continue;
            if (b.mode == BuffApplyMode.Multiplier) v *= (1f + b.value);
        }

        return v;
    }

    // The effective melee dash distance for the specified hand:
    // base PlayerStats.meleeDashDistance, then apply that hand's weapon-side buffs (weapon + attachments + armor weapon-side buffs).
    public float GetMeleeDashDistanceForHand(bool isLeftHand)
    {
        var hand = isLeftHand ? leftHand : rightHand;
        return ApplyBuffListToValue(hand.buffs, Attributes.MeleeDashDistance, meleeDashDistance);
    }

    // The effective melee reload/cooldown time for the specified hand:
    public float GetMeleeReloadTimeForHand(bool isLeftHand, float baseReloadTime)
    {
        var hand = isLeftHand ? leftHand : rightHand;
        return ApplyBuffListToValue(hand.buffs, Attributes.MeleeReloadTime, baseReloadTime);
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
            case Attributes.LockOnRange: lockOnRange += buff.value; break;
            case Attributes.AutoAimSpeed: autoAimSpeed += buff.value; break;

            case Attributes.MeleeDashDistance: meleeDashDistance += buff.value; break;
            case Attributes.MeleeReloadTime: meleeReloadTime += buff.value; break;
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
            case Attributes.LockOnRange: lockOnRange *= m; break;
            case Attributes.AutoAimSpeed: autoAimSpeed *= m; break;

            case Attributes.MeleeDashDistance: meleeDashDistance *= m; break;
            case Attributes.MeleeReloadTime: meleeReloadTime *= m; break;
        }
    }

    public VisualChange CurrentLegVisual { get; private set; } = new VisualChange()
    {
        heightOffset = 0f,
        animationType = default // 依你 AnimationType 的預設值
    };

    private void AddWeaponAndAttachmentBuffs(WeaponStats target, MeleeWeaponInstance weaponInst)
    {
        if (weaponInst == null) return;

        // 武器本體 buffs
        AddBuffListToWeapon(target, weaponInst.buffs);

        // 零件 buffs（attachment 裡每個 PartInstance）
        if (weaponInst.attachment == null) return;

        foreach (var part in weaponInst.attachment)
        {
            if (part == null || part.item == null) continue;
            AddBuffListToWeapon(target, part.buffs);
        }
    }

    void AddWeaponAndAttachmentBuffs(WeaponStats target, RangeWeaponInstance weaponInst)
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

    void AddShoulderWeaponAndAttachmentBuffs(ShoulderWeaponStats target, ShoulderWeaponInstance weaponInst)
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
