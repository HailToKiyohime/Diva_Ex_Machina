using System.Collections;
using UnityEngine;

/// <summary>
/// 遠程攻擊的所有行為：開火、彈道、擴散、換彈、射速冷卻。
///
/// 從 AttackManager 拆出來，職責邊界是「開火之後的事」。
/// 武器資料的同步（ApplyHand / ApplyShoulder）、彈藥 UI、船上狀態
/// 都還在 AttackManager，這裡透過 attackManager 參考存取。
///
/// 掛在跟 AttackManager 同一個 GameObject 上。
/// </summary>
[RequireComponent(typeof(AttackManager))]
public class RangeAttackController : MonoBehaviour
{
    [SerializeField] private AttackManager attackManager;

    [Header("Just for testing")]
    [Tooltip("武器沒有指定 bullet 時的後備彈藥 prefab")]
    [SerializeField] private GameObject testBulletPrefab;

    [Header("Debug")]
    [Tooltip("開火時在 Scene 視圖畫出：黃 = 準星 ray（相機出發）、紅 = 子彈實際方向（槍口出發），持續 1 秒。")]
    [SerializeField] private bool debugDrawFireRay = false;

    private void Awake()
    {
        if (attackManager == null)
            attackManager = GetComponent<AttackManager>();
    }

    private void Reset()
    {
        attackManager = GetComponent<AttackManager>();
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
                bool isLeft = (w == attackManager.leftHandWeapon);
                bool isRight = (w == attackManager.rightHandWeapon);

                if (isLeft)
                {
                    attackManager.playerAnimation.LeftWeaponMuzzleFlash();
                }
                else if (isRight)
                {
                    attackManager.playerAnimation.RightWeaponMuzzleFlash();
                }

                var currentBullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
                var bulletComp = currentBullet.GetComponent<Bullet>();

                bulletComp.attacker = attackManager.playerRb.gameObject;

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
                        if (attackManager.shipRB != null && attackManager.onShip == true)
                        {
                            movingPlatformOffset = attackManager.shipRB.linearVelocity;
                        }
                    }
                }
                else
                {
                    targetPoint = fireRay.GetPoint(100f);
                    if (attackManager.shipRB != null && attackManager.onShip == true)
                    {
                        movingPlatformOffset = attackManager.shipRB.linearVelocity;
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
            attackManager.PushAmmoUI();

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
        if (w.kind == HandWeaponKind.Melee) return;   // 近戰不走換彈流程
        if (w.rangeRuntime.reloading) return;

        w.rangeRuntime.reloading = true;
        w.reloadNormalized = 0f;
        attackManager.PushAmmoUI();
        attackManager.playerAnimation.AnimEvent_Reload();

        if (w.range.reloadTime <= 0f)
        {
            w.reloadNormalized = 1f;
            attackManager.PushAmmoUI();
            FinishReload(w);
            attackManager.PushAmmoUI();
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
            attackManager.PushAmmoUI();
            yield return null;
        }

        w.reloadNormalized = 1f;
        attackManager.PushAmmoUI();

        FinishReload(w);
        attackManager.PushAmmoUI();
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
}