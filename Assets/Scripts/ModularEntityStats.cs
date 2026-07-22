using UnityEngine;
using System;
using MoreMountains.Feedbacks;

public class ModularEntityStats : MonoBehaviour, IDamageable
{
    public float maxHealth;
    public float health;
    public float physicalDefense; // in percentage (0-100)
    public float explosionDefense; // in percentage (0-100)
    public float energyDefense; // in percentage (0-100)
    public float coldDefense; // in percentage (0-100)

    public float sprintSpeed;
    public float accelerationSpeed;
    public float decelerationSpeed;

    public float rotationSpeed;
    // Optional events
    public event Action OnDeath;

    public ModularEntityBrain enemyBrain;
    public ModularEntityEffectManager modularEntityEffectManager;

    public float jumpHeight;

    public void TakeDamage(DamageInfo dmg, GameObject attacker)
    {
        float amount =
            dmg.physical * GetDefenseMultiplier(physicalDefense) +
            dmg.explosion * GetDefenseMultiplier(explosionDefense) +
            dmg.energy * GetDefenseMultiplier(energyDefense) +
            dmg.cold * GetDefenseMultiplier(coldDefense);

        if (amount <= 0f) return;

        health -= amount;
        modularEntityEffectManager.PlayDamageFeedback(amount);

        if (health <= 0f)
        {
            health = 0f;
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }
    public float GetDefenseMultiplier(float defenseValue)
    {
        float reduction = Mathf.Clamp01(defenseValue / 1000f);
        return 1f - reduction;
    }

}
