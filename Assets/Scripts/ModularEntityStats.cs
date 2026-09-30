using UnityEngine;
using System;

public class ModularEntityStats : MonoBehaviour, IDamageable
{
    public float maxHealth;
    public float health;
    // 防禦值（不是百分比）。減傷 = 防禦 ÷ (防禦 + 1000)，詳見 DefenseFormula。
    public float physicalDefense;
    public float explosionDefense;
    public float energyDefense;
    public float coldDefense;

    public float sprintSpeed;
    public float accelerationSpeed;
    public float decelerationSpeed;

    public float rotationSpeed;
    // Optional events
    public event Action OnDeath;

    public ModularEntityEffectManager modularEntityEffectManager;

    public float jumpHeight;

    public void TakeDamage(DamageInfo dmg, GameObject attacker)
    {
        float amount = DefenseFormula.Apply(dmg, physicalDefense, explosionDefense, energyDefense, coldDefense);

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
}
