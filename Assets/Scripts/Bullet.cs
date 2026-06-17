using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject attacker;

    public float physicalDamage = 0;
    public float explosionDamage = 0;
    public float energyDamage = 0;
    public float coldDamage = 0;

    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;

    public float lifespan = 5f;
    public Rigidbody rb;

    public bool isMelee = false;

    public LayerMask ignoreLayer;
    public LayerMask enemyLayer;
    public bool ignoreObstacles = false;

    // Your semantics:
    // 0  = destroy after 1st enemy impact
    // 1  = pass 1 enemy, destroy after 2nd enemy impact
    // 2  = pass 2 enemies, destroy after 3rd enemy impact
    // -1 = infinite
    public int penetration = 0;
    [SerializeField] private PlayerAnimation meleeImpactOwnerAnim;

    private bool _destroyed;
    private Collider _selfCol;

    // Prevent multi-collider enemies from taking damage multiple times per bullet
    private readonly HashSet<int> _hitEnemyIds = new HashSet<int>();

    public void SetMeleeImpactOwner(PlayerAnimation ownerAnim)
    {
        meleeImpactOwnerAnim = ownerAnim;
    }

    void Start()
    {
        Destroy(gameObject, lifespan);
        rb = GetComponent<Rigidbody>();
        _selfCol = GetComponent<Collider>();
        StartCoroutine(Predict());
    }

    protected void FixedUpdate()
    {
        StartCoroutine(Predict());
    }

    private void OnTriggerEnter(Collider collider)
    {
        OnTriggerEnterFixed(collider);
    }

    private bool IsInEnemyLayer(Collider col)
    {
        int bit = 1 << col.gameObject.layer;
        return (enemyLayer.value & bit) != 0;
    }

    private int GetPredictMask()
    {
        int notBullet = ~LayerMask.GetMask("Bullet");
        int mask = ignoreObstacles ? enemyLayer.value : notBullet;
        return mask & notBullet;
    }

    protected IEnumerator Predict()
    {
        if (rb == null) yield break;

        Vector3 prediction = transform.position + rb.linearVelocity * Time.fixedDeltaTime;

        RaycastHit hit2;
        int layerMask = GetPredictMask() & ~ignoreLayer.value;
        if (Physics.Linecast(transform.position, prediction, out hit2, layerMask))
        {
            transform.position = hit2.point;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            yield return null;
            OnTriggerEnterFixed(hit2.collider);
        }
    }

    private void DestroyBullet()
    {
        if (_destroyed) return;
        _destroyed = true;

        if (isMelee) Destroy(gameObject, 0.1f);
        else Destroy(gameObject);
    }

    protected virtual void OnTriggerEnterFixed(Collider other)
    {
        //Debug.Log("Bullet hit: " + other.name);
        if (_destroyed) return;

        // Ignore specified layers
        if (IsInIgnoreLayer(other))
            return;

        // ignoreObstacles => only react to enemyLayer
        if (ignoreObstacles && !IsInEnemyLayer(other))
            return;

        // 可被傷害的目標？（不再綁死 EnemyStats，改認 IDamageable 介面）
        var target = other.GetComponentInParent<IDamageable>();
        // enemyLayer 現在代表「這顆子彈允許打到的層」：
        // 玩家開的子彈設成敵人層，敵人開的子彈設成玩家層，以此分敵我、避免友軍誤傷。
        bool isTarget = (target != null) && IsInEnemyLayer(other);

        if (isTarget)
        {
            // 用被打物件的 instance id 去重（避免同一目標多 collider 重複觸發）
            var targetObj = target as Component;
            int id = (targetObj != null) ? targetObj.GetInstanceID() : other.GetInstanceID();
            if (_hitEnemyIds.Contains(id)) return;
            _hitEnemyIds.Add(id);

            // 暴擊在「攻擊方」這邊結算（暴擊是攻擊者的屬性）
            float critMul = 1f;
            if (Random.value < Mathf.Clamp01(criticalChance))
                critMul = Mathf.Max(1f, criticalMultiplier);

            // 只交出「原始四種傷害」，防禦由被打的目標自己套用
            DamageInfo dmg = new DamageInfo(
                physicalDamage * critMul,
                explosionDamage * critMul,
                energyDamage * critMul,
                coldDamage * critMul
            );

            target.TakeDamage(dmg, attacker);

            // 近戰命中回饋
            if (isMelee)
            {
                meleeImpactOwnerAnim?.AnimEvent_MeleeImpact();
                var targetRb = (targetObj != null) ? targetObj.GetComponent<Rigidbody>() : null;
                if (targetRb != null)
                    targetRb.linearVelocity = Vector3.zero; // 命中時停住目標，回饋更明確（可選）
            }

            // 避免穿過同一 collider 時重複觸發
            if (_selfCol != null && other != null)
                Physics.IgnoreCollision(_selfCol, other, true);

            // 接著處理穿透（語意與原本相同）
            if (penetration == -1)
            {
                return; // 無限穿透
            }
            else if (penetration > 0)
            {
                penetration--; // 消耗一次穿透
                return;        // 繼續飛
            }
            else // penetration == 0
            {
                DestroyBullet(); // 命中後銷毀
                return;
            }
        }

        // Not enemy:
        // - if ignoreObstacles was true, we already returned above
        // - otherwise hit obstacle => destroy
        DestroyBullet();
    }

    private bool IsInIgnoreLayer(Collider col)
    {
        if (col == null)
        {
            return false;
        }
        return (ignoreLayer.value & (1 << col.gameObject.layer)) != 0;
    }
}