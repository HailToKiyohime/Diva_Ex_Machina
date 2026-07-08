using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

[System.Serializable]
public enum EnemyState
{
    Idle,
    Patrolling,
    Retreat,
    Chasing,
    Combat,
}

[System.Serializable]
public enum TargetType
{
    Player,
    Enemy,
    Building,
    Core,
    Obstacle,
}

[System.Serializable]
public class Target
{
    public Transform targetTransform;
    public TargetType type;
    public float targetPriority;
    public float priorityDecreaseMultiplier;
    public Target(Transform targetTransform, float targetPriority, float priorityDecreaseMultiplier)
    {
        this.targetTransform = targetTransform;
        this.targetPriority = targetPriority;
        this.priorityDecreaseMultiplier = priorityDecreaseMultiplier;
    }
}

[System.Serializable]
public class TargerPreference
{
    public TargetType targetType;
    public float priorityMultiplier;
    public float priorityDecreaseMultiplier;
}

public class ModularEntityBrain : MonoBehaviour
{
    public EnemyState currentState;
    public List<TargerPreference> targetPreferences = new List<TargerPreference>();
    public List<Target> targets = new List<Target>();

    public Vector3 nextWaypoint;
    public Vector3 destination;
    public Vector3 spawnLocation;
    [Header("Idle Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 idleDestinationUpdateRange = new Vector2(1f, 3f);
    public float idleActivityAreaRadius = 10f;
    [Header("Patrol Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 patrolDestinationUpdateRange = new Vector2(1f, 3f);
    public float patrolActivityAreaRadius = 20f;
    [Header("Retreat Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 retreatDestinationUpdateRange = new Vector2(1f, 3f);
    [Header("Chase Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 chaseDestinationUpdateRange = new Vector2(1f, 3f);
    [Header("Combat Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 combatDestinationUpdateRange = new Vector2(1f, 3f);
    public float combatActivityAreaRadius = 20f;
    public float reactionCoolDown = 3f;
    public float reactionChance = 0.5f;

    private float destinationTimer;   // 單一計時器，倒數到 0 就更新 destination

    // Runtime lookup cache：把 targetPreferences 攤平成 type -> decrease multiplier
    // lookup 從原本每個 target 都要跑一輪內層 loop (O(n*m)) 變成 O(1)
    private readonly Dictionary<TargetType, float> decayMultiplierByType = new Dictionary<TargetType, float>();

    public void Start()
    {
        currentState = EnemyState.Idle;
        RebuildPreferenceCache();
    }

    public void FixedUpdate()
    {
        PriorityUpdate();
        StateUpdate();
    }

    protected void PriorityUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (targets.Count > 1)
        {
            // 倒序：RemoveAt 時不會影響還沒處理到的較小 index
            for (int x = targets.Count - 1; x >= 0; x--)
            {
                Target t = targets[x];

                // 找不到對應 preference 時 multiplier 維持 1（等同原本的 else 分支）
                float typeMultiplier = 1f;
                if (decayMultiplierByType.TryGetValue(t.type, out float found))
                    typeMultiplier = found;

                t.targetPriority -= t.priorityDecreaseMultiplier * typeMultiplier * dt;

                if (t.targetPriority <= 0f)
                    targets.RemoveAt(x);
            }
        }
    }

    // 只要在 runtime 用程式改過 targetPreferences，就呼叫這個重建 cache
    // 只在 Inspector 設定、play 前固定的話，Start 跑一次就夠
    protected void RebuildPreferenceCache()
    {
        decayMultiplierByType.Clear();
        for (int i = 0; i < targetPreferences.Count; i++)
        {
            // 用 indexer 而非 Add：同 type 重複時「後面蓋前面」，跟原本 loop 行為一致
            decayMultiplierByType[targetPreferences[i].targetType] = targetPreferences[i].priorityDecreaseMultiplier;
        }
    }

    protected void StateUpdate()
    {
        destinationTimer -= Time.fixedDeltaTime;
        if (destinationTimer > 0f) return;   // 還沒到時間，什麼都不做

        // 時間到：執行「當前 state」的 behaviour，並用「當前 state 的 range」重 roll 下一次間隔
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleBehaviour();
                destinationTimer = RollInterval(idleDestinationUpdateRange);
                break;
            case EnemyState.Patrolling:
                PatrollingBehaviour();
                destinationTimer = RollInterval(patrolDestinationUpdateRange);
                break;
            case EnemyState.Retreat:
                RetreatBehaviour();
                destinationTimer = RollInterval(retreatDestinationUpdateRange);
                break;
            case EnemyState.Chasing:
                ChasingBehaviour();
                destinationTimer = RollInterval(chaseDestinationUpdateRange);
                break;
            case EnemyState.Combat:
                CombatBehaviour();
                destinationTimer = RollInterval(combatDestinationUpdateRange);
                break;
        }
    }

    public Transform FindTarget()
    {
        if (targets.Count == 0) return null;
        Target bestTarget = targets[0];
        for (int i = 1; i < targets.Count; i++)
        {
            if (targets[i].targetPriority > bestTarget.targetPriority)
                bestTarget = targets[i];
        }
        return bestTarget.targetTransform;
    }

    private float RollInterval(Vector2 range){ 
        return Random.Range(range.x, range.y); 
    }
    protected void IdleBehaviour()
    {
        Vector2 r = Random.insideUnitCircle * idleActivityAreaRadius;
        destination = spawnLocation + new Vector3(r.x, 0f, r.y);
    }

    protected void PatrollingBehaviour()
    {
        Vector2 r = Random.insideUnitCircle * patrolActivityAreaRadius;
        destination = spawnLocation + new Vector3(r.x, 0f, r.y);
    }

    protected void RetreatBehaviour()
    {
        destination = spawnLocation;
    }

    protected void ChasingBehaviour()
    {
        destination = FindTarget().position;
    }

    protected void CombatBehaviour()
    {
        Vector3 r = Random.insideUnitCircle * combatActivityAreaRadius;
        destination = spawnLocation + new Vector3(r.x, 0f, r.y);

    }
    public void ChangeState(EnemyState next)
    {
        currentState = next;
        destinationTimer = 0f;   // 下一次 StateUpdate 立刻觸發，馬上挑新 destination
    }

}