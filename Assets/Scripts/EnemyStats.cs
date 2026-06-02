using UnityEngine;
using System;
using MoreMountains.Feedbacks;

public class EnemyStats : MonoBehaviour
{
    public float maxHealth;
    private float health;
    public float physicalDefense; // in percentage (0-100)
    public float explosionDefense; // in percentage (0-100)
    public float energyDefense; // in percentage (0-100)
    public float coldDefense; // in percentage (0-100)

    public float speed;
    public float accelerationSpeed;
    public float decelerationSpeed;

    // Optional events
    public event Action OnDeath;

    public MMF_Player damageFeedback;

    public EnemyBrain enemyBrain;

    void Start()
    {
        health = maxHealth;
        enemyBrain = GetComponent<EnemyBrain>();
    }

    void Update() { }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        health -= amount;

        damageFeedback.PlayFeedbacks(this.transform.position, amount);

        if (health <= 0f)
        {
            health = 0f;
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (amount <= 0f) return;

        health -= amount;

        damageFeedback.PlayFeedbacks(this.transform.position, amount);

        AddAggro(attacker, amount);

        if (health <= 0f)
        {
            health = 0f;
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    private void AddAggro(GameObject attacker, float amountOfDamage)
    {
        if (attacker == null || enemyBrain == null) return;

        TargetPriority existing = enemyBrain.targetList
            .Find(t => t != null && t.target == attacker.transform);

        // Use the same threshold as EnemyBrain so decay and accumulation stay in sync
        float threshold = enemyBrain.DamagePerAggroStep;

        if (existing != null)
        {
            // Track stepped aggro: every `threshold` damage = +1 damageAggro
            int aggroBefore = Mathf.FloorToInt(existing.damageCauseByTarget / threshold);
            existing.damageCauseByTarget += amountOfDamage;
            int aggroAfter = Mathf.FloorToInt(existing.damageCauseByTarget / threshold);
            existing.damageAggro += (aggroAfter - aggroBefore);
        }
        else
        {
            enemyBrain.targetList.Add(new TargetPriority
            {
                target = attacker.transform,
                baseAggro = 1,
                isMainTarget = false,
                damageCauseByTarget = amountOfDamage,
                damageAggro = Mathf.FloorToInt(amountOfDamage / threshold)
            });
        }
    }

    public float GetDefenseMultiplier(float defenseValue)
    {
        float reduction = Mathf.Clamp01(defenseValue / 1000f);
        return 1f - reduction;
    }
}