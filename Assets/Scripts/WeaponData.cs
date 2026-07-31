using UnityEngine;

[System.Serializable]
public class WeaponDamage
{
    public float physicalDamage;
    public float explosionDamage;
    public float energyDamage;
    public float coldDamage;
}

[System.Serializable]
public class RangeWeaponSettings
{
    public float reloadTime;
    public int bulletPerShot;
    public int roundPerTap;
    public float timeBetweenShooting;
    public float timeBetweenShots;
    public float spread;
    public int magazineSize;
    public float bulletSpeed;
    public int firingMode; // 0:Single, 1:Auto, 2:Charge
}

[System.Serializable]
public class RangeWeaponRuntimeState
{
    public int bulletsLeft;
    public bool shooting;
    public bool reloading;
    public bool readyToShoot;
    public bool allowInvoke;
}

[System.Serializable]
public class MeleeWeaponSettings
{
    public MeleeWeaponClass weaponClass; // 由 blade × handle 經 MeleeStanceRules 推導
    public MeleeGrip grip;               // 由另一隻手是否空著推導（runtime）

    public float meleeOutput = 1f;  // 傷害倍率（已含 buff）
    public float meleeSpeed = 1f;   // 動畫 / 連段速度倍率（已含 buff）
    public float dashDistance;      // 單段突進基礎距離（已含 buff）
    public float reloadTime;        // 連段結束後的硬直冷卻（已含 buff）

    public GameObject slashVfx;     // 來自 MeleeWeapon.swordSlash

    public MeleeHitbox hitbox;      // 裝備時由 EquipmentManager 註冊到 instance 上
}

[System.Serializable]
public class MeleeWeaponRuntimeState
{
    public bool reloading;              // 冷卻中
    public bool attacking;              // 揮擊中
    public int comboIndex = -1;        // 目前第幾段，-1 = 不在連段
    public float cooldownNormalized;    // 0~1，給 UI 用
}

[System.Serializable]
public class Weapon
{

    public Transform muzzle;
    public GameObject bullet;

    // 目前這個槽位是遠程還是近戰（給 PlayerMovement / UI 分流用）
    public HandWeaponKind kind = HandWeaponKind.None;

    // Damage Type
    public WeaponDamage damage = new WeaponDamage();

    // Melee Weapon Specific (Foldout)
    public MeleeWeaponSettings melee = new MeleeWeaponSettings();
    // Runtime State (Foldout)
    public MeleeWeaponRuntimeState meleeRuntime = new MeleeWeaponRuntimeState();

    // Range Weapon Specific (Foldout)
    public RangeWeaponSettings range = new RangeWeaponSettings();

    // Runtime State (Foldout)
    public RangeWeaponRuntimeState rangeRuntime = new RangeWeaponRuntimeState();

    // Reload UI runtime (0~1). When reloading, ammo bar shows this value.
    [HideInInspector] public float reloadNormalized;
}