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
    private PathFinder pathFinder;

    public EnemyState currentState;
    public List<TargerPreference> targetPreferences = new List<TargerPreference>();
    public List<Target> targets = new List<Target>();

    protected Vector3[] path;
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

    private float destinationTimer;  

    private readonly Dictionary<TargetType, float> decayMultiplierByType = new Dictionary<TargetType, float>();

    [SerializeField] private float waypointArriveRadius = 1.5f;
    private int currentWaypointIndex = 0;

    public void Start()
    {
        pathFinder = gameObject.GetComponent<PathFinder>();
        currentState = EnemyState.Idle;
        RebuildPreferenceCache();
    }

    public void FixedUpdate()
    {
        PriorityUpdate();
        PathUpdate();
        StateBehaviour();
    }

    protected void PriorityUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (targets.Count > 1)
        {
            for (int x = targets.Count - 1; x >= 0; x--)
            {
                Target t = targets[x];

                float typeMultiplier = 1f;
                if (decayMultiplierByType.TryGetValue(t.type, out float found))
                    typeMultiplier = found;

                t.targetPriority -= t.priorityDecreaseMultiplier * typeMultiplier * dt;

                if (t.targetPriority <= 0f)
                    targets.RemoveAt(x);
            }
        }
    }
    protected void RebuildPreferenceCache()
    {
        decayMultiplierByType.Clear();
        for (int i = 0; i < targetPreferences.Count; i++)
        {
            decayMultiplierByType[targetPreferences[i].targetType] = targetPreferences[i].priorityDecreaseMultiplier;
        }
    }

    protected void StateBehaviour()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleBehaviour();
                break;
            case EnemyState.Patrolling:
                PatrollingBehaviour();
                break;
            case EnemyState.Retreat:
                RetreatBehaviour();
                break;
            case EnemyState.Chasing:
                ChasingBehaviour();
                break;
            case EnemyState.Combat:
                CombatBehaviour();
                break;
        }
    }

    protected void PathUpdate()
    {
        destinationTimer -= Time.fixedDeltaTime;
        if (destinationTimer > 0f) return;

        Vector2 r;
        switch (currentState)
        {
            case EnemyState.Idle:
                destinationTimer = RollInterval(idleDestinationUpdateRange);
                r = Random.insideUnitCircle * idleActivityAreaRadius;
                destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                path = pathFinder.FindPath(destination);
                break;
            case EnemyState.Patrolling:
                destinationTimer = RollInterval(patrolDestinationUpdateRange);
                r = Random.insideUnitCircle * patrolActivityAreaRadius;
                destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                path = pathFinder.FindPath(destination);
                break;
            case EnemyState.Retreat:
                destinationTimer = RollInterval(retreatDestinationUpdateRange);
                destination = spawnLocation;
                path = pathFinder.FindPath(destination);
                break;
            case EnemyState.Chasing:
                destinationTimer = RollInterval(chaseDestinationUpdateRange);
                destination = FindTarget().position;
                path = pathFinder.FindPath(destination);
                break;
            case EnemyState.Combat:
                destinationTimer = RollInterval(combatDestinationUpdateRange);
                r = Random.insideUnitCircle * combatActivityAreaRadius;
                destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                path = pathFinder.FindPath(destination);
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

    public Vector3 FindNextWaypoint()
    {
        if (path == null || path.Length == 0)
            return transform.position;

        // path 可能換過、變短，夾住避免越界
        if (currentWaypointIndex > path.Length - 1)
            currentWaypointIndex = path.Length - 1;

        // 已經抵達的 corner 就往前推進；到最後一個就停在那，不再 +1（自然不會越界）
        while (currentWaypointIndex < path.Length - 1 &&
               Vector3.Distance(transform.position, path[currentWaypointIndex]) < waypointArriveRadius)
        {
            currentWaypointIndex++;
        }

        return path[currentWaypointIndex];
    }

    private float RollInterval(Vector2 range){ 
        return Random.Range(range.x, range.y); 
    }
    protected void IdleBehaviour()
    {
    }

    protected void PatrollingBehaviour()
    {
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

    }
    public void ChangeState(EnemyState next)
    {
        currentState = next;
        destinationTimer = 0f;  
    }

}