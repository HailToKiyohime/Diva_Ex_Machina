using UnityEngine;

/// <summary>
/// �ˮ`��o���C
///
/// �γ~�G��u�֦� Collider ������v(�Ҧp Player ����) �M�u�B�z���A/��q���}���v
/// (�Ҧp���b Player Manager �W�� PlayerStats) ���b�P�@�����l��W�ɡA
/// �l�u�� GetComponentInParent&lt;IDamageable&gt;() �|�䤣�� PlayerStats�C
///
/// ��o�Ӹ}�����b�u�� Collider ������v�W�A���ۤv��@ IDamageable�A
/// ����ˮ`��A��浹�u���������̡C�l�u�������Χ�C
///
/// ��m��m�G���b�Ҧ����� Collider ���u�@�P�ڪ���v(�q�`�O Player �ڪ���)�A
/// �o�ˤ��ޥ�����Ӥl Collider�A���W�䳣�|���o����o���C
/// </summary>
public class DamageRelay : MonoBehaviour, IDamageable
{
    [SerializeField] private MonoBehaviour damageReceiver;

    private IDamageable _target;

    void Awake()
    {
        _target = damageReceiver as IDamageable;
    }

    public void TakeDamage(DamageInfo damage, GameObject attacker)
    {
        _target?.TakeDamage(damage, attacker);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (damageReceiver != null && !(damageReceiver is IDamageable))
        {
            damageReceiver = null;
        }
    }
#endif
}