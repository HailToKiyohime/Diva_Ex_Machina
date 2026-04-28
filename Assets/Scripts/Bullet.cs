using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
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
    public int ricochet = 0;
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

        // Enemy?
        var enemy = other.GetComponentInParent<EnemyStats>();
        bool isEnemy = (enemy != null) && IsInEnemyLayer(other);

        if (isEnemy)
        {
            // Avoid double hits on same enemy (multiple colliders)
            int id = enemy.GetInstanceID();
            if (_hitEnemyIds.Contains(id)) return;
            _hitEnemyIds.Add(id);

            // Apply damage FIRST
            float final =
                physicalDamage * enemy.GetDefenseMultiplier(enemy.physicalDefense) +
                explosionDamage * enemy.GetDefenseMultiplier(enemy.explosionDefense) +
                energyDamage * enemy.GetDefenseMultiplier(enemy.energyDefense) +
                coldDamage * enemy.GetDefenseMultiplier(enemy.coldDefense);

            bool isCrit = (Random.value < Mathf.Clamp01(criticalChance));
            if (isCrit)
                final *= Mathf.Max(1f, criticalMultiplier);

            enemy.TakeDamage(final);

            // melee impact feedback
            if (isMelee)
                meleeImpactOwnerAnim?.AnimEvent_MeleeImpact();

            // Prevent repeated trigger spam while passing through this collider
            if (_selfCol != null && other != null)
                Physics.IgnoreCollision(_selfCol, other, true);

            // THEN handle penetration (your semantics)
            if (penetration == -1)
            {
                return; // infinite
            }
            else if (penetration > 0)
            {
                penetration--; // consume one pass
                return;        // keep flying
            }
            else // penetration == 0
            {
                DestroyBullet(); // destroy AFTER this impact
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
        return (ignoreLayer.value & (1 << col.gameObject.layer)) != 0;
    }
}