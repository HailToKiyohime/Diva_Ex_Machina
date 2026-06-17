using UnityEngine;

public struct DamageInfo
{
    public float physical;
    public float explosion;
    public float energy;
    public float cold;

    public DamageInfo(float physical, float explosion, float energy, float cold)
    {
        this.physical = physical;
        this.explosion = explosion;
        this.energy = energy;
        this.cold = cold;
    }
}

public interface IDamageable
{
    // 由「被打的實體」自己套用防禦、結算血量
    void TakeDamage(DamageInfo damage, GameObject attacker);
}