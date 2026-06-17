using UnityEngine;

/// <summary>
/// 傷害轉發器。
///
/// 用途：當「擁有 Collider 的物件」(例如 Player 本體) 和「處理狀態/血量的腳本」
/// (例如掛在 Player Manager 上的 PlayerStats) 不在同一條父子鏈上時，
/// 子彈的 GetComponentInParent&lt;IDamageable&gt;() 會找不到 PlayerStats。
///
/// 把這個腳本掛在「有 Collider 的物件」上，它自己實作 IDamageable，
/// 收到傷害後再轉交給真正的接收者。子彈完全不用改。
///
/// 放置位置：掛在所有相關 Collider 的「共同根物件」(通常是 Player 根物件)，
/// 這樣不管打到哪個子 Collider，往上找都會找到這個轉發器。
/// </summary>
public class DamageRelay : MonoBehaviour, IDamageable
{
    [Tooltip("真正處理傷害的腳本，必須實作 IDamageable。把 Player Manager 上的 PlayerStats 拖進來。")]
    [SerializeField] private MonoBehaviour damageReceiver;

    private IDamageable _target;

    void Awake()
    {
        _target = damageReceiver as IDamageable;

        if (_target == null)
            Debug.LogError($"[DamageRelay] '{name}' 的 damageReceiver 未設定或未實作 IDamageable。", this);
    }

    public void TakeDamage(DamageInfo damage, GameObject attacker)
    {
        _target?.TakeDamage(damage, attacker);
    }

#if UNITY_EDITOR
    // 在 Inspector 拖到不對的腳本時即時提醒並清空
    void OnValidate()
    {
        if (damageReceiver != null && !(damageReceiver is IDamageable))
        {
            Debug.LogWarning(
                $"[DamageRelay] {damageReceiver.GetType().Name} 沒有實作 IDamageable，請改拖有實作的腳本。",
                this);
            damageReceiver = null;
        }
    }
#endif
}