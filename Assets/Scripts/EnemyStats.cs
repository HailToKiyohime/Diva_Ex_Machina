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

    public EnemyBrain enemyBrain; // Reference to the EnemyBrain for potential interactions (e.g., alerting on hit)
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        health -= amount;

        damageFeedback.PlayFeedbacks(this.transform.position, amount);

        // Optional: debug
        // Debug.Log($"[EnemyStats] Took damage: {amount}, HP now: {health}");

        if (health <= 0f)
        {
            health = 0f;
            OnDeath?.Invoke();

            // 最簡：直接刪掉（主人之後可換成 ragdoll / drop / pooling）
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

            // 最簡：直接刪掉（主人之後可換成 ragdoll / drop / pooling）
            Destroy(gameObject);
        }
    }
    private void AddAggro(GameObject attacker, float amountOfDamage)
    {
        if (attacker == null || enemyBrain == null) return;

        TargetPriority existing = enemyBrain.targetList
            .Find(t => t != null && t.target == attacker.transform);

        if (existing != null)
        {
            int aggroBefore = Mathf.FloorToInt(existing.damageCauseByTarget / (maxHealth * 0.005f));
            existing.damageCauseByTarget += amountOfDamage;
            int aggroAfter = Mathf.FloorToInt(existing.damageCauseByTarget / (maxHealth * 0.005f));

            existing.aggro += (aggroAfter - aggroBefore);
        }
        else
        {
            enemyBrain.targetList.Add(new TargetPriority
            {
                target = attacker.transform,
                aggro = 3,
                isMainTarget = false,
                damageCauseByTarget = amountOfDamage
            });
        }
    }


    public float GetDefenseMultiplier(float defenseValue)
    {
        float reduction = Mathf.Clamp01(defenseValue / 1000f);
        return 1f - reduction;
    }
}
