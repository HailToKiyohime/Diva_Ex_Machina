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
public class Weapon
{
    public Transform muzzle;
    public GameObject bullet;

    // Damage Type
    public WeaponDamage damage = new WeaponDamage();

    // Range Weapon Specific (Foldout)
    public RangeWeaponSettings range = new RangeWeaponSettings();

    // Runtime State (Foldout)
    public RangeWeaponRuntimeState runtime = new RangeWeaponRuntimeState();

    // Reload UI runtime (0~1). When reloading, ammo bar shows this value.
    [HideInInspector] public float reloadNormalized;
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

        ApplyHand(stats.leftHand, leftWeapon, ref _leftSource);
        ApplyHand(stats.rightHand, rightWeapon, ref _rightSource);
        PushAmmoUI();
    }

    private static void ClearWeaponOutput(Weapon outWeapon, ref RangeWeaponInstance cachedSource)
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

        outWeapon.reloadNormalized = 0f;

        // runtime 不一定要清，但清咗 Inspector 會更直觀
        outWeapon.runtime.bulletsLeft = 0;
        outWeapon.runtime.shooting = false;
        outWeapon.runtime.reloading = false;
        outWeapon.runtime.readyToShoot = false;
        outWeapon.runtime.allowInvoke = false;
    }

    private static void ApplyHand(WeaponStats hand, Weapon outWeapon, ref RangeWeaponInstance cachedSource)
    {
        if (outWeapon == null) return;

        // 沒武器：清空輸出（也可以改成 disable 該手射擊）
        if (hand == null || hand.rangeweapon == null)
        {
            ClearWeaponOutput(outWeapon, ref cachedSource);
            return;
        }

        // 如果換武器：重置 runtime state（子彈/裝填狀態等）
        bool changedWeapon = cachedSource != hand.rangeweapon;
        cachedSource = hand.rangeweapon;

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
            outWeapon.runtime.bulletsLeft = outWeapon.range.magazineSize;
            outWeapon.runtime.shooting = false;
            outWeapon.runtime.reloading = false;
            outWeapon.runtime.readyToShoot = true;
            outWeapon.runtime.allowInvoke = true;
            outWeapon.reloadNormalized = 0f;
        }
    }

    private void PushAmmoUI()
    {
        var ui = UIManager.Instance;
        if (ui == null) return;

        float leftFill = CalcAmmoBarFill(leftWeapon);
        float rightFill = CalcAmmoBarFill(rightWeapon);

        // Also drive reload color + flashing (UIManager stores the colors)
        ui.SetAmmoState(
            leftFill, leftWeapon != null && leftWeapon.runtime.reloading,
            rightFill, rightWeapon != null && rightWeapon.runtime.reloading
        );
    }

    private static float CalcAmmoBarFill(Weapon w)
    {
        if (w == null) return 0f;

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

        // Later: cooldown / attack speed / animation trigger / hit detection / stamina, etc.
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
