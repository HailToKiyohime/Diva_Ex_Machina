using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第 3 步的臨時測試工具：按一個鍵，手動 Configure 並開關一次 hitbox。
///
/// 存在的理由是第 4 步的 MeleeAttackController 還沒寫，沒有東西會驅動 hitbox。
/// 控制器完成之後這個腳本就可以刪掉。
///
/// 用法：掛在玩家身上，指派 AttackManager 與 attacker（通常是玩家根物件），
/// hittableLayers 設成敵人層。
/// </summary>
public class MeleeHitboxDebugDriver : MonoBehaviour
{
    [SerializeField] private AttackManager attackManager;

    [Tooltip("傷害的歸屬者，通常是玩家根物件。會用來排除打到自己。")]
    [SerializeField] private GameObject attacker;

    [Tooltip("允許打到的層，設成敵人層")]
    [SerializeField] private LayerMask hittableLayers;

    [Header("Test Swing")]
    [SerializeField] private bool useLeftHand = true;
    [SerializeField] private Key testKey = Key.K;

    [Tooltip("模擬 MeleeAttackStep.damageMultiplier")]
    [SerializeField] private float damageMultiplier = 1f;

    [Tooltip("模擬 MeleeAttackStep.knockback")]
    [SerializeField] private float knockback = 0f;

    [Tooltip("hitbox 開啟時長（秒），模擬動畫上兩個 Animation Event 的間隔")]
    [SerializeField] private float openDuration = 0.3f;

    private Coroutine _running;

    private void Reset()
    {
        attackManager = GetComponentInChildren<AttackManager>();
        attacker = gameObject;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current[testKey].wasPressedThisFrame) return;

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(TestSwing());
    }

    private IEnumerator TestSwing()
    {
        if (attackManager == null)
        {
            Debug.LogWarning("[MeleeHitboxDebugDriver] 沒有指派 AttackManager。", this);
            yield break;
        }

        var w = useLeftHand ? attackManager.leftHandWeapon : attackManager.rightHandWeapon;

        if (w == null || w.kind != HandWeaponKind.Melee)
        {
            Debug.LogWarning($"[MeleeHitboxDebugDriver] {(useLeftHand ? "左" : "右")}手不是近戰武器" +
                             $"（kind = {(w != null ? w.kind.ToString() : "null")}）。", this);
            yield break;
        }

        var hitbox = w.melee.hitbox;
        if (hitbox == null)
        {
            Debug.LogWarning("[MeleeHitboxDebugDriver] 這把武器沒有 hitbox。" +
                             "檢查武器 prefab 的刀刃上有沒有掛 MeleeHitbox。", this);
            yield break;
        }

        // 這幾行就是第 4 步 MeleeAttackController 會做的事
        float mul = w.melee.meleeOutput * damageMultiplier;
        var ps = PlayerStats.Instance;

        hitbox.Configure(new MeleeHitData
        {
            attacker = (attacker != null) ? attacker : gameObject,
            baseDamage = new DamageInfo(
                w.damage.physicalDamage * mul,
                w.damage.explosionDamage * mul,
                w.damage.energyDamage * mul,
                w.damage.coldDamage * mul),
            criticalChance = (ps != null) ? ps.criticalChance : 0f,
            criticalMultiplier = (ps != null) ? ps.criticalMultiplier : 1f,
            knockback = knockback,
            hittableLayers = hittableLayers,
        });

        hitbox.Open();
        yield return new WaitForSeconds(openDuration);
        hitbox.Close();

        _running = null;
    }
}