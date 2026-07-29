using MoreMountains.Feedbacks;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

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
    public MeleeWeaponStance stance;

    public float meleeOutput = 1f;  // 傷害倍率（已含 buff）
    public float meleeSpeed = 1f;   // 動畫 / 連段速度倍率（已含 buff）
    public float dashDistance;      // 單段突進基礎距離（已含 buff）
    public float reloadTime;        // 連段結束後的硬直冷卻（已含 buff）

    public GameObject slashVfx;     // 來自 MeleeWeapon.swordSlash
}

[System.Serializable]
public class MeleeWeaponRuntimeState
{
    public bool reloading;           // 冷卻中
    public bool attacking;           // 揮擊中
    public int comboIndex = -1;      // 目前第幾段，-1 = 不在連段
    public float cooldownNormalized; // 0~1，給 UI 用
}

[System.Serializable]
public class Weapon
{
    public Transform muzzle;
    public GameObject bullet;

    // 目前這個槽位是遠程還是近戰（給 PlayerMovement / UI 分流用）
    public HandWeaponKind kind = HandWeaponKind.None;

    // Damage Type（遠程近戰共用）
    public WeaponDamage damage = new WeaponDamage();

    // Range Weapon (Foldout)
    public RangeWeaponSettings range = new RangeWeaponSettings();
    public RangeWeaponRuntimeState rangeRuntime = new RangeWeaponRuntimeState();

    // Melee Weapon (Foldout)
    public MeleeWeaponSettings melee = new MeleeWeaponSettings();
    public MeleeWeaponRuntimeState meleeRuntime = new MeleeWeaponRuntimeState();

    // Reload UI runtime (0~1). When reloading, ammo bar shows this value.
    [HideInInspector] public float reloadNormalized;
}

public class AttackManager : MonoBehaviour
{
    public Rigidbody shipRB;
    public bool onShip;

    public PlayerAnimation playerAnimation;

    public Weapon leftHandWeapon;
    public Weapon rightHandWeapon;
    public Weapon leftShoulderWeapon;
    public Weapon rightShoulderWeapon;

    // Just for testing
    public GameObject testBulletPrefab;

    // 用來判斷「是否換了一把武器」，以便重置子彈等 runtime state
    private RangeWeaponInstance _leftRangeWeaponSource;
    private RangeWeaponInstance _rightRangeWeaponSource;

    // Melee Weapon Source cache
    private MeleeWeaponInstance _leftMeleeWeaponSource;
    private MeleeWeaponInstance _rightMeleeWeaponSource;

    // Shoulder Weapon Scource cache
    private ShoulderWeaponInstance _leftShoulderWeaponSource;
    private ShoulderWeaponInstance _rightShoulderWeaponSource;

    [Header("Optional: used to add player velocity to sword slash")]
    [SerializeField] public Rigidbody playerRb;

    [Header("Debug")]
    [Tooltip("開火時在 Scene 視圖畫出：黃 = 準星 ray（相機出發）、紅 = 子彈實際方向（槍口出發），持續 1 秒。")]
    [SerializeField] private bool debugDrawFireRay = false;


    private void Awake()
    {
        if (playerRb == null)
            playerRb = GetComponentInParent<Rigidbody>();
    }
    private void OnEnable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHandWeaponDataChanged += SyncFromPlayerStats;

        SyncFromPlayerStats();
        PushAmmoUI();
    }

    private void OnDisable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHandWeaponDataChanged -= SyncFromPlayerStats;
    }

    private void SyncFromPlayerStats()
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return;

        ApplyHand(stats.leftHand, leftHandWeapon, true, ref _leftRangeWeaponSource, ref _leftMeleeWeaponSource);
        ApplyHand(stats.rightHand, rightHandWeapon, false, ref _rightRangeWeaponSource, ref _rightMeleeWeaponSource);


        ApplyShoulder(stats.leftShoulder, leftShoulderWeapon, ref _leftShoulderWeaponSource);
        ApplyShoulder(stats.rightShoulder, rightShoulderWeapon, ref _rightShoulderWeaponSource);

        PushAmmoUI();
    }

    private static void ClearRangeOutput(Weapon outWeapon, ref RangeWeaponInstance cachedSource)
    {
        cachedSource = null;

        outWeapon.bullet = null;
        outWeapon.muzzle = null;

        outWeapon.damage.physicalDamage = 0f;
        outWeapon.damage.explosionDamage = 0f;
        outWeapon.damage.energyDamage = 0f;
        outWeapon.damage.coldDamage = 0f;

        outWeapon.range.reloadTime = 0f;
        outWeapon.range.bulletPerShot = 0;
        outWeapon.range.roundPerTap = 0;
        outWeapon.range.timeBetweenShooting = 0f;
        outWeapon.range.timeBetweenShots = 0f;
        outWeapon.range.spread = 0f;
        outWeapon.range.magazineSize = 0;
        outWeapon.range.bulletSpeed = 0f;
        outWeapon.range.firingMode = 0;

        // runtime 不一定要清，但清咗 Inspector 會更直觀
        outWeapon.rangeRuntime.bulletsLeft = 0;
        outWeapon.rangeRuntime.shooting = false;
        outWeapon.rangeRuntime.reloading = false;
        outWeapon.rangeRuntime.readyToShoot = false;
        outWeapon.rangeRuntime.allowInvoke = false;
    }

    private static void ClearMeleeOutput(Weapon outWeapon, ref MeleeWeaponInstance cachedSource)
    {
        cachedSource = null;

        // 注意：default(MeleeWeaponStance) == OneHandedPolearm，不是「無」。
        // 判斷有沒有近戰武器一律用 outWeapon.kind，不要用 stance。
        outWeapon.melee.stance = default;
        outWeapon.melee.meleeOutput = 1f;
        outWeapon.melee.meleeSpeed = 1f;
        outWeapon.melee.dashDistance = 0f;
        outWeapon.melee.reloadTime = 0f;
        outWeapon.melee.slashVfx = null;

        outWeapon.meleeRuntime.reloading = false;
        outWeapon.meleeRuntime.attacking = false;
        outWeapon.meleeRuntime.comboIndex = -1;
        outWeapon.meleeRuntime.cooldownNormalized = 0f;
    }

    private static void ClearWeaponOutput(Weapon outWeapon, ref ShoulderWeaponInstance cachedSource)
    {
        cachedSource = null;

        outWeapon.bullet = null;
        outWeapon.muzzle = null;

        outWeapon.damage.physicalDamage = 0f;
        outWeapon.damage.explosionDamage = 0f;
        outWeapon.damage.energyDamage = 0f;
        outWeapon.damage.coldDamage = 0f;

        outWeapon.range.reloadTime = 0f;
        outWeapon.range.bulletPerShot = 0;
        outWeapon.range.roundPerTap = 0;
        outWeapon.range.timeBetweenShooting = 0f;
        outWeapon.range.timeBetweenShots = 0f;
        outWeapon.range.spread = 0f;
        outWeapon.range.magazineSize = 0;
        outWeapon.range.bulletSpeed = 0f;
        outWeapon.range.firingMode = 0;

        // runtime 不一定要清，但清咗 Inspector 會更直觀
        outWeapon.rangeRuntime.bulletsLeft = 0;
        outWeapon.rangeRuntime.shooting = false;
        outWeapon.rangeRuntime.reloading = false;
        outWeapon.rangeRuntime.readyToShoot = false;
        outWeapon.rangeRuntime.allowInvoke = false;
    }
    private static void ApplyHand(WeaponStats hand, Weapon outWeapon, bool isLeftHand,
                                  ref RangeWeaponInstance cachedRange,
                                  ref MeleeWeaponInstance cachedMelee)
    {
        if (outWeapon == null) return;

        // 沒武器：兩邊都清
        if (hand == null || hand.weaponKind == HandWeaponKind.None || !hand.HasWeapon)
        {
            outWeapon.kind = HandWeaponKind.None;
            ClearRangeOutput(outWeapon, ref cachedRange);
            ClearMeleeOutput(outWeapon, ref cachedMelee);
            return;
        }

        outWeapon.kind = hand.weaponKind;

        // 分流：遠程 / 近戰
        if (hand.weaponKind == HandWeaponKind.Range)
        {
            ClearMeleeOutput(outWeapon, ref cachedMelee);   // 從刀換回槍，清掉近戰殘留

            if (hand.rangeweapon == null)
            {
                ClearRangeOutput(outWeapon, ref cachedRange);
                return;
            }

            // 如果換武器：重置 runtime state（子彈/裝填狀態等）
            bool changedWeapon = cachedRange != hand.rangeweapon;
            cachedRange = hand.rangeweapon;

            // 1) 子彈 prefab / muzzle
            var rw = hand.rangeweapon.item as RangeWeapon;
            outWeapon.bullet = (rw != null) ? rw.bullet : null;
            outWeapon.muzzle = hand.rangeweapon.muzzlePoint;

            // 2) 把該手「總 Buff」轉成射擊數值
            outWeapon.damage.physicalDamage = hand.GetAttribute(Attributes.PhysicalDamage);
            outWeapon.damage.explosionDamage = hand.GetAttribute(Attributes.ExplosionDamage);
            outWeapon.damage.energyDamage = hand.GetAttribute(Attributes.EnergyDamage);
            outWeapon.damage.coldDamage = hand.GetAttribute(Attributes.ColdDamage);

            outWeapon.range.reloadTime = hand.GetAttribute(Attributes.ReloadTime);
            outWeapon.range.bulletPerShot = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.BulletPerShot)));
            outWeapon.range.roundPerTap = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.RoundPerPull)));
            outWeapon.range.timeBetweenShooting = hand.GetAttribute(Attributes.TimeBetweenShooting);
            outWeapon.range.timeBetweenShots = hand.GetAttribute(Attributes.TimeBetweenShots);
            outWeapon.range.spread = hand.GetAttribute(Attributes.Spread);
            outWeapon.range.magazineSize = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.MagazineSize)));
            outWeapon.range.bulletSpeed = hand.GetAttribute(Attributes.BulletSpeed);
            outWeapon.range.firingMode = Mathf.RoundToInt(hand.GetAttribute(Attributes.FiringMode));

            if (changedWeapon)
            {
                outWeapon.rangeRuntime.bulletsLeft = outWeapon.range.magazineSize;
                outWeapon.rangeRuntime.shooting = false;
                outWeapon.rangeRuntime.reloading = false;
                outWeapon.rangeRuntime.readyToShoot = true;
                outWeapon.rangeRuntime.allowInvoke = true;
                outWeapon.reloadNormalized = 0f;
            }
            return;
        }
        // ───── 近戰 ─────
        if (hand.weaponKind == HandWeaponKind.Melee)
        {
            // ★ 原本的 bug：換成刀之後舊槍的 damage / rangeRuntime 會殘留
            ClearRangeOutput(outWeapon, ref cachedRange);

            if (hand.meleeWeapon == null || hand.meleeWeapon.item is not MeleeWeapon mw)
            {
                ClearMeleeOutput(outWeapon, ref cachedMelee);
                return;
            }

            bool changedWeapon = cachedMelee != hand.meleeWeapon;
            cachedMelee = hand.meleeWeapon;

            // 1) 傷害：跟遠程走同一條 buff 管線
            outWeapon.damage.physicalDamage = hand.GetAttribute(Attributes.PhysicalDamage);
            outWeapon.damage.explosionDamage = hand.GetAttribute(Attributes.ExplosionDamage);
            outWeapon.damage.energyDamage = hand.GetAttribute(Attributes.EnergyDamage);
            outWeapon.damage.coldDamage = hand.GetAttribute(Attributes.ColdDamage);

            // 2) 近戰專屬
            var ps = PlayerStats.Instance;
            outWeapon.melee.stance = mw.meleeWeaponStance;
            outWeapon.melee.slashVfx = mw.swordSlash;

            if (ps != null)
            {
                outWeapon.melee.meleeOutput = ps.GetMeleeOutputForHand(isLeftHand);
                outWeapon.melee.meleeSpeed = Mathf.Max(0.01f, ps.GetMeleeSpeedForHand(isLeftHand));
                outWeapon.melee.dashDistance = Mathf.Max(0f, ps.GetMeleeDashDistanceForHand(isLeftHand));
                outWeapon.melee.reloadTime = Mathf.Max(0f, ps.GetMeleeReloadTimeForHand(isLeftHand));
            }

            if (changedWeapon)
            {
                outWeapon.meleeRuntime.reloading = false;
                outWeapon.meleeRuntime.attacking = false;
                outWeapon.meleeRuntime.comboIndex = -1;
                outWeapon.meleeRuntime.cooldownNormalized = 0f;
            }

            return;
        }
    }
    private static void ApplyShoulder(ShoulderWeaponStats shoulder, Weapon outWeapon, ref ShoulderWeaponInstance cachedSource)
    {
        if (outWeapon == null) return;
        // 沒武器：清空輸出（也可以改成 disable 該手射擊）
        if (shoulder == null || shoulder.weaponKind == ShoulderWeaponKind.None || (!shoulder.HasWeapon))
        {
            ClearWeaponOutput(outWeapon, ref cachedSource);
            return;
        }
        // 分流：遠程 
        if (shoulder.weaponKind == ShoulderWeaponKind.Range)
        {
            if (shoulder.shoulderweapon == null)
            {
                ClearWeaponOutput(outWeapon, ref cachedSource);
                return;
            }

            // 如果換武器：重置 runtime state（子彈/裝填狀態等）
            bool changedWeapon = cachedSource != shoulder.shoulderweapon;
            cachedSource = shoulder.shoulderweapon;

            // 1) 子彈 prefab / muzzle
            var rw = shoulder.shoulderweapon.item as ShoulderWeapon;
            outWeapon.bullet = (rw != null) ? rw.bullet : null;
            outWeapon.muzzle = shoulder.shoulderweapon.muzzlePoint;

            // 2) 把該手「總 Buff」轉成射擊數值
            outWeapon.damage.physicalDamage = shoulder.GetAttribute(Attributes.PhysicalDamage);
            outWeapon.damage.explosionDamage = shoulder.GetAttribute(Attributes.ExplosionDamage);
            outWeapon.damage.energyDamage = shoulder.GetAttribute(Attributes.EnergyDamage);
            outWeapon.damage.coldDamage = shoulder.GetAttribute(Attributes.ColdDamage);

            outWeapon.range.reloadTime = shoulder.GetAttribute(Attributes.ReloadTime);
            outWeapon.range.bulletPerShot = Mathf.Max(1, Mathf.RoundToInt(shoulder.GetAttribute(Attributes.BulletPerShot)));
            outWeapon.range.roundPerTap = Mathf.Max(1, Mathf.RoundToInt(shoulder.GetAttribute(Attributes.RoundPerPull)));
            outWeapon.range.timeBetweenShooting = shoulder.GetAttribute(Attributes.TimeBetweenShooting);
            outWeapon.range.timeBetweenShots = shoulder.GetAttribute(Attributes.TimeBetweenShots);
            outWeapon.range.spread = shoulder.GetAttribute(Attributes.Spread);
            outWeapon.range.magazineSize = Mathf.Max(1, Mathf.RoundToInt(shoulder.GetAttribute(Attributes.MagazineSize)));
            outWeapon.range.bulletSpeed = shoulder.GetAttribute(Attributes.BulletSpeed);
            outWeapon.range.firingMode = Mathf.RoundToInt(shoulder.GetAttribute(Attributes.FiringMode));

            if (changedWeapon)
            {
                outWeapon.rangeRuntime.bulletsLeft = outWeapon.range.magazineSize;
                outWeapon.rangeRuntime.shooting = false;
                outWeapon.rangeRuntime.reloading = false;
                outWeapon.rangeRuntime.readyToShoot = true;
                outWeapon.rangeRuntime.allowInvoke = true;
                outWeapon.reloadNormalized = 0f;
            }
            return;
        }
    }

    private void PushAmmoUI()
    {
        var ui = UIManager.Instance;
        if (ui == null) return;

        var stats = PlayerStats.Instance;

        // Hand fill
        float leftFill = CalcAmmoBarFill(leftHandWeapon);
        float rightFill = CalcAmmoBarFill(rightHandWeapon);

        bool leftReloading =  leftHandWeapon != null && leftHandWeapon.rangeRuntime.reloading;

        bool rightReloading = rightHandWeapon != null && rightHandWeapon.rangeRuntime.reloading;

        // ✅ Shoulder fill (shoulder is range-only in current design)
        float leftShoulderFill = CalcAmmoBarFill(leftShoulderWeapon);
        float rightShoulderFill = CalcAmmoBarFill(rightShoulderWeapon);

        bool leftShoulderReloading = (leftShoulderWeapon != null && leftShoulderWeapon.rangeRuntime.reloading);
        bool rightShoulderReloading = (rightShoulderWeapon != null && rightShoulderWeapon.rangeRuntime.reloading);

        ui.SetAmmoState(
            leftFill, leftReloading,
            rightFill, rightReloading,
            leftShoulderFill, leftShoulderReloading,
            rightShoulderFill, rightShoulderReloading);
    }

    private static float CalcAmmoBarFill(Weapon w)
    {
        if (w == null) return 0f;

        if (w.kind == HandWeaponKind.Melee)
            return w.meleeRuntime.reloading ? Mathf.Clamp01(w.meleeRuntime.cooldownNormalized) : 1f;

        // Reload mode: fillAmount means reload progress
        if (w.rangeRuntime.reloading)
            return Mathf.Clamp01(w.reloadNormalized);

        // Ammo mode: fillAmount means bullets / magazine
        if (w.range.magazineSize <= 0) return 0f;
        return Mathf.Clamp01((float)w.rangeRuntime.bulletsLeft / w.range.magazineSize);
    }

    public bool TryStartShoot(Weapon w)
    {
        if (w == null) return false;
        if (w.kind == HandWeaponKind.Melee) return false;

        // 沒子彈就嘗試換彈（跟 HandleAttack 同邏輯）
        if (w.rangeRuntime.readyToShoot && !w.rangeRuntime.reloading && w.rangeRuntime.bulletsLeft <= 0)
            StartReload(w);

        // 可以射擊才真正開火
        if (w.rangeRuntime.readyToShoot && !w.rangeRuntime.reloading && w.rangeRuntime.bulletsLeft > 0)
        {
            w.rangeRuntime.bulletsLeft = Mathf.Max(0, w.rangeRuntime.bulletsLeft);
            w.rangeRuntime.readyToShoot = false;
            StartCoroutine(Shoot(w, PlayerAiming.Instance.GetRay()));
            return true;
        }

        return false;
    }

    private IEnumerator Shoot(Weapon w, Ray ray)
    {
        w.rangeRuntime.readyToShoot = false;
        int shotsToFire = Mathf.Min(w.range.roundPerTap, w.rangeRuntime.bulletsLeft);

        var bulletPrefab = (w.bullet != null) ? w.bullet : testBulletPrefab;
        var muzzle = w.muzzle;
        if (bulletPrefab == null || muzzle == null)
            yield break;

        for (int i = 0; i < shotsToFire; i++)
        {
            for (int x = 0; x < w.range.bulletPerShot; x++)
            {
                bool isLeft = (w == leftHandWeapon);
                bool isRight = (w == rightHandWeapon);

                if (isLeft)
                {
                    playerAnimation.LeftWeaponMuzzleFlash();
                }
                else if (isRight)
                {
                    playerAnimation.RightWeaponMuzzleFlash();
                }

                var currentBullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
                var bulletComp = currentBullet.GetComponent<Bullet>();

                bulletComp.attacker = playerRb.gameObject;

                if (bulletComp != null)
                {
                    // 1) 傷害（已由 ApplyHand / ApplyShoulder 同步到 w.damage）
                    bulletComp.physicalDamage = w.damage.physicalDamage;
                    bulletComp.explosionDamage = w.damage.explosionDamage;
                    bulletComp.energyDamage = w.damage.energyDamage;
                    bulletComp.coldDamage = w.damage.coldDamage;

                    // 2) 暴擊（先用 PlayerStats 的最終值；之後要做「武器/零件暴擊」再擴充）
                    var ps = PlayerStats.Instance;
                    if (ps != null)
                    {
                        bulletComp.criticalChance = ps.criticalChance;
                        bulletComp.criticalMultiplier = ps.criticalMultiplier;
                    }
                }

                Vector3 targetPoint;

                Vector3 movingPlatformOffset = Vector3.zero;

                // Burst（roundPerTap > 1）每一發都改用「當幀最新」的準星 ray；
                // 原本整個 burst 沿用扣扳機那一刻的舊 ray，相機一轉就會歪。
                Ray fireRay = (PlayerAiming.Instance != null) ? PlayerAiming.Instance.GetRay() : ray;

                bool constrained = PlayerAiming.Instance.lockOn;
                if (constrained)
                {
                    var targetRb = PlayerAiming.Instance.GetTargetRigidbody();
                    if (TrySampleTarget(targetRb, out Vector3 tgtPos, out Vector3 tgtVel))
                    {
                        if (MathToolKit.InterceptionPoint(tgtPos, muzzle.position, tgtVel, w.range.bulletSpeed, out var predicted))
                            targetPoint = predicted;
                        else
                            targetPoint = tgtPos;
                    }
                    else
                    {
                        targetPoint = fireRay.GetPoint(100f);
                        if (shipRB != null && onShip == true)
                        {
                            movingPlatformOffset = shipRB.linearVelocity;
                        }
                    }
                }
                else
                {
                    targetPoint = fireRay.GetPoint(100f);
                    if (shipRB != null && onShip == true)
                    {
                        movingPlatformOffset = shipRB.linearVelocity;
                    }
                }

                Vector3 dirNoSpread = (targetPoint - muzzle.position).normalized;

                Vector3 right = Vector3.Cross(dirNoSpread, Vector3.up);
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.Cross(dirNoSpread, Vector3.right);
                right.Normalize();
                Vector3 up = Vector3.Cross(right, dirNoSpread);

                float maxRad = w.range.spread * Mathf.Deg2Rad;
                float cosMax = Mathf.Cos(maxRad);

                float cosAlpha = Mathf.Lerp(1f, cosMax, Random.value);
                float sinAlpha = Mathf.Sqrt(1f - cosAlpha * cosAlpha);
                float phi = Random.Range(0f, Mathf.PI * 2f);

                Vector3 lateral = right * Mathf.Cos(phi) + up * Mathf.Sin(phi);
                Vector3 dirWithSpread = dirNoSpread * cosAlpha + lateral * sinAlpha;

                if (debugDrawFireRay)
                {
                    Debug.DrawRay(fireRay.origin, fireRay.direction * 100f, Color.yellow, 1f);
                    Debug.DrawRay(muzzle.position, dirNoSpread * 100f, Color.red, 1f);
                }

                var rb = currentBullet.GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity = (dirWithSpread * w.range.bulletSpeed) + movingPlatformOffset;
                //make bullet face the direction it's moving
                currentBullet.transform.forward = (dirWithSpread * w.range.bulletSpeed);
            }

            w.rangeRuntime.bulletsLeft--;
            PushAmmoUI();

            if (i < shotsToFire - 1)
                yield return new WaitForSeconds(w.range.timeBetweenShots);
        }

        if (w.rangeRuntime.allowInvoke)
        {
            w.rangeRuntime.allowInvoke = false;
            StartCoroutine(ResetShotCooldown(w));
        }
    }

    public void StartReload(Weapon w)
    {
        if (w == null) return;
        if (w.kind == HandWeaponKind.Melee) return;
        if (w.rangeRuntime.reloading) return;

        w.rangeRuntime.reloading = true;
        w.reloadNormalized = 0f;
        PushAmmoUI();
        playerAnimation.AnimEvent_Reload();

        if (w.range.reloadTime <= 0f)
        {
            w.reloadNormalized = 1f;
            PushAmmoUI();
            FinishReload(w);
            PushAmmoUI();
            return;
        }

        StartCoroutine(ReloadCoroutine(w));
    }

    private IEnumerator ReloadCoroutine(Weapon w)
    {
        float elapsed = 0f;

        while (elapsed < w.range.reloadTime)
        {
            elapsed += Time.deltaTime;
            w.reloadNormalized = Mathf.Clamp01(elapsed / w.range.reloadTime);
            PushAmmoUI();
            yield return null;
        }

        w.reloadNormalized = 1f;
        PushAmmoUI();

        FinishReload(w);
        PushAmmoUI();
    }

    private void FinishReload(Weapon w)
    {
        w.rangeRuntime.bulletsLeft = w.range.magazineSize;
        w.rangeRuntime.reloading = false;
        w.reloadNormalized = 0f;
    }

    private IEnumerator ResetShotCooldown(Weapon w)
    {
        yield return new WaitForSeconds(w.range.timeBetweenShooting);
        w.rangeRuntime.readyToShoot = true;
        w.rangeRuntime.allowInvoke = true;
    }

    private bool TrySampleTarget(Rigidbody rb, out Vector3 pos, out Vector3 vel)
    {
        pos = default;
        vel = default;

        if (!rb) return false;

        try
        {
            pos = rb.position;
            vel = rb.linearVelocity;
            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }


    public void SetOnShip(Rigidbody rb)
    {
        shipRB = rb;
        onShip = true;
    }

    public void SetOffShip()
    {
        shipRB = null;
        onShip = false;
    }
}