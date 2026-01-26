using System.Collections;
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

    private bool _hasHit;

    void Start()
    {
        Destroy(gameObject, lifespan);
        rb = GetComponent<Rigidbody>();
        StartCoroutine(Predict());
    }

    protected void FixedUpdate()
    {
        StartCoroutine(Predict());
    }

    private void OnTriggerEnter(Collider collider)
    {
        // 統一走同一套命中處理，避免 Trigger/Linecast 兩邊各做一次
        OnTriggerEnterFixed(collider);
    }

    protected IEnumerator Predict()
    {
        if (rb == null) yield break;

        Vector3 prediction = transform.position + rb.linearVelocity * Time.fixedDeltaTime;

        RaycastHit hit2;
        int layerMask = ~LayerMask.GetMask("Bullet");
        if (Physics.Linecast(transform.position, prediction, out hit2, layerMask))
        {
            transform.position = hit2.point;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            yield return null;
            OnTriggerEnterFixed(hit2.collider);
        }
    }

    protected virtual void OnTriggerEnterFixed(Collider other)
    {
        if (_hasHit) return;
        _hasHit = true;

        // 只對 EnemyStats 結算（主人之後要打爆炸物/可破壞物，再擴展介面）
        var enemy = other.GetComponentInParent<EnemyStats>();
        if (enemy != null)
        {
            float final =
                physicalDamage * enemy.GetDefenseMultiplier(enemy.physicalDefense) +
                explosionDamage * enemy.GetDefenseMultiplier(enemy.explosionDefense) +
                energyDamage * enemy.GetDefenseMultiplier(enemy.energyDefense) +
                coldDamage * enemy.GetDefenseMultiplier(enemy.coldDefense);

            bool isCrit = (Random.value < Mathf.Clamp01(criticalChance));
            if (isCrit)
                final *= Mathf.Max(1f, criticalMultiplier);

            enemy.TakeDamage(final);
        }

        Destroy(gameObject);
    }
}