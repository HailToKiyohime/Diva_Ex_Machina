using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon
{
    public Transform muzzle;
    public GameObject bullet;
    // Damage Type
    public float physicalDamage;
    public float explosionDamage;
    public float energyDamage;
    public float coldDamage;

    // Range Weapon Specific
    public float reloadTime;
    public int bulletPerShot;
    public int roundPerTap;
    public float timeBetweenShooting;
    public float timeBetweenShots;
    public float spread;
    public int magazineSize;
    public float bulletSpeed;
    public int firingMode; //0:Single, 1:Auto, 2:Charge

    // Runtime State
    public int bulletsLeft;
    public bool shooting;
    public bool reloading;
    public bool readyToShoot;
    public bool allowInvoke;

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

    private static void ApplyHand(WeaponStats hand, Weapon outWeapon, ref RangeWeaponInstance cachedSource)
    {
        if (outWeapon == null) return;

        // 沒武器：清空輸出（也可以改成 disable 該手射擊）
        if (hand == null || hand.weapon == null)
        {
            cachedSource = null;
            outWeapon.bullet = null;
            outWeapon.physicalDamage = 0f;
            outWeapon.explosionDamage = 0f;
            outWeapon.energyDamage = 0f;
            outWeapon.coldDamage = 0f;
            outWeapon.reloadTime = 0f;
            outWeapon.bulletPerShot = 0;
            outWeapon.roundPerTap = 0;
            outWeapon.timeBetweenShooting = 0f;
            outWeapon.timeBetweenShots = 0f;
            outWeapon.spread = 0f;
            outWeapon.magazineSize = 0;
            outWeapon.bulletSpeed = 0f;
            outWeapon.firingMode = 0;
            outWeapon.reloadNormalized = 0f;
            return;
        }

        // 如果換武器：重置 runtime state（子彈/裝填狀態等）
        bool changedWeapon = cachedSource != hand.weapon;
        cachedSource = hand.weapon;

        // 1) 子彈 prefab / muzzle
        var rw = hand.weapon.item as RangeWeapon;
        outWeapon.bullet = (rw != null) ? rw.bullet : null;
        outWeapon.muzzle = hand.weapon.muzzlePoint;

        // 2) 把該手「總 Buff」轉成射擊數值
        outWeapon.physicalDamage = hand.GetAttribute(Attributes.PhysicalDamage);
        outWeapon.explosionDamage = hand.GetAttribute(Attributes.ExplosionDamage);
        outWeapon.energyDamage = hand.GetAttribute(Attributes.EnergyDamage);
        outWeapon.coldDamage = hand.GetAttribute(Attributes.ColdDamage);

        outWeapon.reloadTime = hand.GetAttribute(Attributes.ReloadTime);
        outWeapon.bulletPerShot = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.BulletPerShot)));
        outWeapon.roundPerTap = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.RoundPerPull)));
        outWeapon.timeBetweenShooting = hand.GetAttribute(Attributes.TimeBetweenShooting);
        outWeapon.timeBetweenShots = hand.GetAttribute(Attributes.TimeBetweenShots);
        outWeapon.spread = hand.GetAttribute(Attributes.Spread);
        outWeapon.magazineSize = Mathf.Max(1, Mathf.RoundToInt(hand.GetAttribute(Attributes.MagazineSize)));
        outWeapon.bulletSpeed = hand.GetAttribute(Attributes.BulletSpeed);
        outWeapon.firingMode = Mathf.RoundToInt(hand.GetAttribute(Attributes.FiringMode));

        if (changedWeapon)
        {
            outWeapon.bulletsLeft = outWeapon.magazineSize;
            outWeapon.shooting = false;
            outWeapon.reloading = false;
            outWeapon.readyToShoot = true;
            outWeapon.allowInvoke = true;
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
            leftFill, leftWeapon != null && leftWeapon.reloading,
            rightFill, rightWeapon != null && rightWeapon.reloading
        );
    }

    private static float CalcAmmoBarFill(Weapon w)
    {
        if (w == null) return 0f;

        // Reload mode: fillAmount means reload progress
        if (w.reloading)
            return Mathf.Clamp01(w.reloadNormalized);

        // Ammo mode: fillAmount means bullets / magazine
        if (w.magazineSize <= 0) return 0f;
        return Mathf.Clamp01((float)w.bulletsLeft / w.magazineSize);
    }

    public bool HandleAttack(Weapon w, UnityEngine.InputSystem.InputAction attackInput)
    {
        if (w == null)
            return false;

        if (w.firingMode == 0)
        {
            w.shooting = attackInput.WasPressedThisFrame();
        }
        else
        {
            w.shooting = attackInput.IsPressed();
        }

        if (w.readyToShoot && w.shooting && w.bulletsLeft <= 0)
            StartReload(w);

        if (w.readyToShoot && w.shooting && !w.reloading && w.bulletsLeft > 0)
        {
            w.bulletsLeft = Mathf.Max(0, w.bulletsLeft);
            w.readyToShoot = false;
            StartCoroutine(Shoot(w, PlayerAiming.Instance.GetRay()));
            return true;
        }

        return false;
    }

    public bool TryStartShoot(Weapon w)
    {
        if (w == null) return false;

        // 沒子彈就嘗試換彈（跟 HandleAttack 同邏輯）
        if (w.readyToShoot && !w.reloading && w.bulletsLeft <= 0)
            StartReload(w);

        // 可以射擊才真正開火
        if (w.readyToShoot && !w.reloading && w.bulletsLeft > 0)
        {
            w.bulletsLeft = Mathf.Max(0, w.bulletsLeft);
            w.readyToShoot = false;
            StartCoroutine(Shoot(w, PlayerAiming.Instance.GetRay()));
            return true;
        }

        return false;
    }

    private IEnumerator Shoot(Weapon w, Ray ray)
    {
        w.readyToShoot = false;
        int shotsToFire = Mathf.Min(w.roundPerTap, w.bulletsLeft);

        var bulletPrefab = (w.bullet != null) ? w.bullet : testBulletPrefab;
        var muzzle = w.muzzle;
        if (bulletPrefab == null || muzzle == null)
            yield break;

        for (int i = 0; i < shotsToFire; i++)
        {
            for (int x = 0; x < w.bulletPerShot; x++)
            {
                // Spawn projectile
                var currentBullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);

                Vector3 targetPoint;

                // if in constrained-lock, do NOT chase the target; shoot along the crosshair ray
                bool constrained = PlayerAiming.Instance.lockOn;
                if (constrained)
                {
                    var targetRb = PlayerAiming.Instance.GetTargetRigidbody();
                    if (TrySampleTarget(targetRb, out Vector3 tgtPos, out Vector3 tgtVel))
                    {
                        // Predict if we have good data; otherwise fall back to current pos
                        if (Math.InterceptionPoint(tgtPos, muzzle.position, tgtVel, w.bulletSpeed, out var predicted))
                            targetPoint = predicted;
                        else
                            targetPoint = tgtPos;
                    }
                    else
                    {
                        // Lock was lost (enemy died or despawned) → use free-aim fallback
                        targetPoint = ray.GetPoint(100f);
                    }
                }
                else
                {
                    // Not locked → use the crosshair ray
                    targetPoint = ray.GetPoint(100f);
                }

                // Direction + spread
                Vector3 dirNoSpread = (targetPoint - muzzle.position).normalized;

                // Build an orthonormal basis around dirNoSpread
                Vector3 right = Vector3.Cross(dirNoSpread, Vector3.up);
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.Cross(dirNoSpread, Vector3.right);
                right.Normalize();
                Vector3 up = Vector3.Cross(right, dirNoSpread);

                // Sample a random direction inside a cone with half-angle = w.spread degrees
                float maxRad = w.spread * Mathf.Deg2Rad;
                float cosMax = Mathf.Cos(maxRad);

                // Uniform over cone area: cos(alpha) is uniform in [cosMax, 1]
                float cosAlpha = Mathf.Lerp(1f, cosMax, Random.value);
                float sinAlpha = Mathf.Sqrt(1f - cosAlpha * cosAlpha);
                float phi = Random.Range(0f, Mathf.PI * 2f);

                Vector3 lateral = right * Mathf.Cos(phi) + up * Mathf.Sin(phi);
                Vector3 dirWithSpread = dirNoSpread * cosAlpha + lateral * sinAlpha;

                var rb = currentBullet.GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity = dirWithSpread * w.bulletSpeed;
            }

            w.bulletsLeft--;
            PushAmmoUI();

            if (i < shotsToFire - 1)
                yield return new WaitForSeconds(w.timeBetweenShots);
        }

        if (w.allowInvoke)
        {
            w.allowInvoke = false;
            StartCoroutine(ResetShotCooldown(w));
        }
    }

    public void StartReload(Weapon w)
    {
        if (w == null) return;
        if (w.reloading) return;

        // Enter reload mode immediately: clear bar to bottom (0)
        w.reloading = true;
        w.reloadNormalized = 0f;
        PushAmmoUI();

        // Defensive: instant reload if reloadTime is 0 or negative
        if (w.reloadTime <= 0f)
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

        // Smoothly refill ammo bar from 0 -> 1 over reloadTime
        while (elapsed < w.reloadTime)
        {
            elapsed += Time.deltaTime;
            w.reloadNormalized = Mathf.Clamp01(elapsed / w.reloadTime);
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
        w.bulletsLeft = w.magazineSize;
        w.reloading = false;
        w.reloadNormalized = 0f;
    }

    private IEnumerator ResetShotCooldown(Weapon w)
    {
        yield return new WaitForSeconds(w.timeBetweenShooting);
        w.readyToShoot = true;
        w.allowInvoke = true;
    }

    // Safely reads target position/velocity. Handles destroyed (“fake null”) rigidbodies.
    private bool TrySampleTarget(Rigidbody rb, out Vector3 pos, out Vector3 vel)
    {
        pos = default;
        vel = default;

        // Unity’s overloaded == handles “fake null”
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
