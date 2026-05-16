using UnityEngine;
using System;
using MoreMountains.Feedbacks;

public class EnemyStats : MonoBehaviour
{
    public float health;
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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

    public float GetDefenseMultiplier(float defenseValue)
    {
        // defenseValue: 0~1000
        // 1000 = 100% damage reduction => multiplier = 0
        float reduction = Mathf.Clamp01(defenseValue / 1000f);
        return 1f - reduction;
    }
}
