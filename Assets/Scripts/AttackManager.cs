using System.Collections;
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
public class MeleeWeaponSettings
{
    public float reloadTime;
    public MeleeWeapon item;
    public Transform weaponObject;
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
public class MeleeWeaponRuntimeState
{
    public bool reloading;
}

[System.Serializable]
public class Weapon
{
    public Transform muzzle;
    public GameObject bullet;

    // Damage Type
    public WeaponDamage damage = new WeaponDamage();

    // Range Weapon Specific (Foldout)
    public RangeWeaponSettings range = new RangeWeaponSettings();

    // Melee Weapon Specific (Foldout)
    public MeleeWeaponSettings melee = new MeleeWeaponSettings();

    // Runtime State (Foldout)
    public RangeWeaponRuntimeState runtime = new RangeWeaponRuntimeState();

    // Melee Runtime State (Foldout)
    public MeleeWeaponRuntimeState meleeRuntime = new MeleeWeaponRuntimeState();

    // Reload UI runtime (0~1). When reloading, ammo bar shows this value.
    [HideInInspector] public float reloadNormalized;

    // Melee reload UI runtime (0~1). When melee reloading, ammo bar shows this value.
    [HideInInspector] public float meleeReloadNormalized;
}

public class AttackManager : MonoBehaviour
{
    public Weapon leftWeapon;
    public Weapon rightWeapon;

    // Just for testing
    public GameObject testBulletPrefab;

    // 用來判斷「是否換了一把武器」，以便重置子彈等 runtime state
    private RangeWeaponInstance _leftSource;
    private RangeWeaponInstance _rightSource;

    // Melee source cache
    private MeleeWeaponInstance _leftMeleeSource;
    private MeleeWeaponInstance _rightMeleeSource;

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

        ApplyHand(stats.leftHand, leftWeapon, ref _leftSource, ref _leftMeleeSource);
        ApplyHand(stats.rightHand, rightWeapon, ref _rightSource, ref _rightMeleeSource);
        PushAmmoUI();
    }

    private static void ClearWeaponOutput(Weapon outWeapon, ref RangeWeaponInstance cachedSource, ref MeleeWeaponInstance cachedMeleeSource)
    {
        cachedSource = null;
        cachedMeleeSource = null;

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

        // melee settings
        outWeapon.melee.reloadTime = 0f;
        outWeapon.melee.item = null;
        outWeapon.melee.weaponObject = null;

        outWeapon.reloadNormalized = 0f;
        outWeapon.meleeReloadNormalized = 0f;

        outWeapon.meleeRuntime.reloading = false;

        // runtime 不一定要清，但清咗 Inspector 會更直觀
        outWeapon.runtime.bulletsLeft = 0;
        outWeapon.runtime.shooting = false;
        outWeapon.runtime.reloading = false;
        outWeapon.runtime.readyToShoot = false;
        outWeapon.runtime.allowInvoke = false;
    }

    private static void ApplyHand(WeaponStats hand, Weapon outWeapon, ref RangeWeaponInstance cachedSource, ref MeleeWeaponInstance cachedMeleeSource)
    {
        if (outWeapon == null) return;

        // 沒武器：清空輸出（也可以改成 disable 該手射擊）
        if (hand == null || hand.weaponKind == HandWeaponKind.None || (!hand.HasWeapon))
        {
            ClearWeaponOutput(outWeapon, ref cachedSource, ref cachedMeleeSource);
            return;
        }

        // 分流：遠程 / 近戰
        if (hand.weaponKind == HandWeaponKind.Range)
        {
            if (hand.rangeweapon == null)
            {
                ClearWeaponOutput(outWeapon, ref cachedSource, ref cachedMeleeSource);
                return;
            }

            // 如果換武器：重置 runtime state（子彈/裝填狀態等）
            bool changedWeapon = cachedSource != hand.rangeweapon;
            cachedSource = hand.rangeweapon;
            cachedMeleeSource = null;

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

            // melee 清空
            outWeapon.melee.reloadTime = 0f;
            outWeapon.melee.item = null;
            outWeapon.melee.weaponObject = null;
            outWeapon.meleeRuntime.reloading = false;
            outWeapon.meleeReloadNormalized = 0f;

            if (changedWeapon)
            {
                outWeapon.runtime.bulletsLeft = outWeapon.range.magazineSize;
                outWeapon.runtime.shooting = false;
                outWeapon.runtime.reloading = false;
                outWeapon.runtime.readyToShoot = true;
                outWeapon.runtime.allowInvoke = true;
                outWeapon.reloadNormalized = 0f;
            }
            return;
        }
        else if (hand.weaponKind == HandWeaponKind.Melee)
        {
            if (hand.meleeWeapon == null)
            {
                ClearWeaponOutput(outWeapon, ref cachedSource, ref cachedMeleeSource);
                return;
            }

            bool changedMelee = cachedMeleeSource != hand.meleeWeapon;
            cachedMeleeSource = hand.meleeWeapon;
            cachedSource = null;

            // range 清空（避免錯用）
            outWeapon.bullet = null;
            outWeapon.muzzle = null;
            outWeapon.range.reloadTime = 0f;
            outWeapon.range.bulletPerShot = 0;
            outWeapon.range.roundPerTap = 0;
            outWeapon.range.timeBetweenShooting = 0f;
            outWeapon.range.timeBetweenShots = 0f;
            outWeapon.range.spread = 0f;
            outWeapon.range.magazineSize = 0;
            outWeapon.range.bulletSpeed = 0f;
            outWeapon.range.firingMode = 0;
            outWeapon.runtime.bulletsLeft = 0;
            outWeapon.runtime.shooting = false;
            outWeapon.runtime.reloading = false;
            outWeapon.runtime.readyToShoot = false;
            outWeapon.runtime.allowInvoke = false;
            outWeapon.reloadNormalized = 0f;

            // melee 設定：暫時用 hand 的 ReloadTime 當作近戰 cooldown（可以被裝備/零件 buff）
            outWeapon.melee.reloadTime = hand.GetAttribute(Attributes.ReloadTime);
            outWeapon.melee.item = hand.meleeWeapon.item as MeleeWeapon;
            // weaponObject：MeleeWeaponInstance 目前沒有提供 Transform，這裡保持 null（需要的話之後再接）
            outWeapon.melee.weaponObject = null;

            if (changedMelee)
            {
                outWeapon.meleeRuntime.reloading = false;
                outWeapon.meleeReloadNormalized = 0f;
            }
            return;
        }


    }

    private void PushAmmoUI()
    {
        var ui = UIManager.Instance;
        if (ui == null) return;

        var stats = PlayerStats.Instance;
        bool leftIsMelee = stats != null && stats.leftHand != null && stats.leftHand.weaponKind == HandWeaponKind.Melee && stats.leftHand.meleeWeapon != null;
        bool rightIsMelee = stats != null && stats.rightHand != null && stats.rightHand.weaponKind == HandWeaponKind.Melee && stats.rightHand.meleeWeapon != null;

        float leftFill = CalcAmmoBarFill(leftWeapon, leftIsMelee);
        float rightFill = CalcAmmoBarFill(rightWeapon, rightIsMelee);

        bool leftReloading = leftIsMelee ? (leftWeapon != null && leftWeapon.meleeRuntime.reloading) : (leftWeapon != null && leftWeapon.runtime.reloading);
        bool rightReloading = rightIsMelee ? (rightWeapon != null && rightWeapon.meleeRuntime.reloading) : (rightWeapon != null && rightWeapon.runtime.reloading);

        // Also drive reload color + flashing (UIManager stores the colors)
        ui.SetAmmoState(leftFill, leftReloading, rightFill, rightReloading);
    }

    private static float CalcAmmoBarFill(Weapon w, bool isMelee)
    {
        if (w == null) return 0f;

        if (isMelee)
        {
            // Melee: bar shows cooldown progress (0~1). When not reloading, show full.
            if (w.meleeRuntime.reloading)
                return Mathf.Clamp01(w.meleeReloadNormalized);
            return 1f;
        }

        // Reload mode: fillAmount means reload progress
        if (w.runtime.reloading)
            return Mathf.Clamp01(w.reloadNormalized);

        // Ammo mode: fillAmount means bullets / magazine
        if (w.range.magazineSize <= 0) return 0f;
        return Mathf.Clamp01((float)w.runtime.bulletsLeft / w.range.magazineSize);
    }

    public bool HandleAttack(Weapon w, UnityEngine.InputSystem.InputAction attackInput)
    {
        if (w == null)
            return false;

        if (w.range.firingMode == 0)
            w.runtime.shooting = attackInput.WasPressedThisFrame();
        else
            w.runtime.shooting = attackInput.IsPressed();

        if (w.runtime.readyToShoot && w.runtime.shooting && w.runtime.bulletsLeft <= 0)
            StartReload(w);

        if (w.runtime.readyToShoot && w.runtime.shooting && !w.runtime.reloading && w.runtime.bulletsLeft > 0)
        {
            w.runtime.bulletsLeft = Mathf.Max(0, w.runtime.bulletsLeft);
            w.runtime.readyToShoot = false;
            StartCoroutine(Shoot(w, PlayerAiming.Instance.GetRay()));
            return true;
        }

        return false;
    }

    public bool TryStartShoot(Weapon w)
    {
        if (w == null) return false;

        // 沒子彈就嘗試換彈（跟 HandleAttack 同邏輯）
        if (w.runtime.readyToShoot && !w.runtime.reloading && w.runtime.bulletsLeft <= 0)
            StartReload(w);

        // 可以射擊才真正開火
        if (w.runtime.readyToShoot && !w.runtime.reloading && w.runtime.bulletsLeft > 0)
        {
            w.runtime.bulletsLeft = Mathf.Max(0, w.runtime.bulletsLeft);
            w.runtime.readyToShoot = false;
            StartCoroutine(Shoot(w, PlayerAiming.Instance.GetRay()));
            return true;
        }

        return false;
    }

    // Step 2 (simple): melee attack entry point.
    // For now this only validates the equipped melee weapon and returns true so PlayerMovement can dash.
    public bool TryStartMeleeAttack(Weapon w)
    {
        if (w == null) return false;

        var stats = PlayerStats.Instance;
        if (stats == null) return false;

        bool isLeft = (w == leftWeapon);
        bool isRight = (w == rightWeapon);

        if (!isLeft && !isRight) return false;

        var hand = isLeft ? stats.leftHand : stats.rightHand;

        if (hand.weaponKind != HandWeaponKind.Melee || hand.meleeWeapon == null)
            return false;

        // cooldown：近戰 reload 期間禁止再觸發
        if (w.meleeRuntime != null && w.meleeRuntime.reloading)
            return false;

        // Later: attack speed / animation trigger / hit detection / stamina, etc.
        Debug.Log($"[AttackManager] Melee attack triggered: {(isLeft ? "Left" : "Right")}");

        return true;
    }


    private IEnumerator Shoot(Weapon w, Ray ray)
    {
        w.runtime.readyToShoot = false;
        int shotsToFire = Mathf.Min(w.range.roundPerTap, w.runtime.bulletsLeft);

        var bulletPrefab = (w.bullet != null) ? w.bullet : testBulletPrefab;
        var muzzle = w.muzzle;
        if (bulletPrefab == null || muzzle == null)
            yield break;

        for (int i = 0; i < shotsToFire; i++)
        {
            for (int x = 0; x < w.range.bulletPerShot; x++)
            {
                var currentBullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);

                Vector3 targetPoint;

                bool constrained = PlayerAiming.Instance.lockOn;
                if (constrained)
                {
                    var targetRb = PlayerAiming.Instance.GetTargetRigidbody();
                    if (TrySampleTarget(targetRb, out Vector3 tgtPos, out Vector3 tgtVel))
                    {
                        if (Math.InterceptionPoint(tgtPos, muzzle.position, tgtVel, w.range.bulletSpeed, out var predicted))
                            targetPoint = predicted;
                        else
                            targetPoint = tgtPos;
                    }
                    else
                    {
                        targetPoint = ray.GetPoint(100f);
                    }
                }
                else
                {
                    targetPoint = ray.GetPoint(100f);
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

                var rb = currentBullet.GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity = dirWithSpread * w.range.bulletSpeed;
            }

            w.runtime.bulletsLeft--;
            PushAmmoUI();

            if (i < shotsToFire - 1)
                yield return new WaitForSeconds(w.range.timeBetweenShots);
        }

        if (w.runtime.allowInvoke)
        {
            w.runtime.allowInvoke = false;
            StartCoroutine(ResetShotCooldown(w));
        }
    }


    // Melee reload (cooldown) - after dash.
    public void StartMeleeReload(Weapon w)
    {
        if (w == null) return;

        // 只有真的裝備近戰時才允許進入 melee reload
        var stats = PlayerStats.Instance;
        if (stats == null) return;

        bool isLeft = (w == leftWeapon);
        bool isRight = (w == rightWeapon);
        if (!isLeft && !isRight) return;

        var hand = isLeft ? stats.leftHand : stats.rightHand;
        if (hand == null || hand.weaponKind != HandWeaponKind.Melee || hand.meleeWeapon == null)
            return;

        if (w.meleeRuntime.reloading) return;

        w.meleeRuntime.reloading = true;
        w.meleeReloadNormalized = 0f;
        PushAmmoUI();

        float rt = Mathf.Max(0f, w.melee.reloadTime);

        if (rt <= 0f)
        {
            w.meleeReloadNormalized = 1f;
            PushAmmoUI();
            FinishMeleeReload(w);
            PushAmmoUI();
            return;
        }

        StartCoroutine(MeleeReloadCoroutine(w, rt));
    }

    private IEnumerator MeleeReloadCoroutine(Weapon w, float reloadTime)
    {
        float elapsed = 0f;

        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            w.meleeReloadNormalized = Mathf.Clamp01(elapsed / reloadTime);
            PushAmmoUI();
            yield return null;
        }

        w.meleeReloadNormalized = 1f;
        PushAmmoUI();

        FinishMeleeReload(w);
        PushAmmoUI();
    }

    private void FinishMeleeReload(Weapon w)
    {
        w.meleeRuntime.reloading = false;
        w.meleeReloadNormalized = 0f;
    }


    public void StartReload(Weapon w)
    {
        if (w == null) return;
        if (w.runtime.reloading) return;

        w.runtime.reloading = true;
        w.reloadNormalized = 0f;
        PushAmmoUI();

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
        w.runtime.bulletsLeft = w.range.magazineSize;
        w.runtime.reloading = false;
        w.reloadNormalized = 0f;
    }

    private IEnumerator ResetShotCooldown(Weapon w)
    {
        yield return new WaitForSeconds(w.range.timeBetweenShooting);
        w.runtime.readyToShoot = true;
        w.runtime.allowInvoke = true;
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
}
