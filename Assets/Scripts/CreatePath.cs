using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class CreatePath : MonoBehaviour
{
    public Transform target;
    private NavMeshPath path;

    [Range(1, 40)]
    [SerializeField] private float combatRange;
    [SerializeField] private float randomCombatRangeOffset;
    [SerializeField] private float combatRangeBrainMinUpdateTime;
    [SerializeField] private float combatRangeBrainMaxUpdateTime;
    [SerializeField] private float normalRangeBrainUpdateTime;

    private float elapsed = 0.0f;

    // 新增：持久化本回合的 updateTime
    private float currentUpdateTime;

    void Start()
    {
        path = new NavMeshPath();
        elapsed = 0.0f;

        // 初始化一個門檻
        currentUpdateTime = normalRangeBrainUpdateTime;
    }

    void Update()
    {
        if (target == null) return;

        float targetDistance = Vector3.Distance(transform.position, target.position);
        elapsed += Time.deltaTime;

        if (elapsed > currentUpdateTime)
        {
            elapsed = 0f;

            // 先算路徑（你原本的邏輯）
            Vector3 navmeshPoint;
            NavMeshHit hit;

            if (targetDistance < combatRange)
            {
                navmeshPoint = new Vector3(
                    target.position.x + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                    1.5f,
                    target.position.z + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset)
                );
            }
            else
            {
                navmeshPoint = new Vector3(
                    target.position.x + Random.Range(-10f, 10f),
                    1.5f,
                    target.position.z + Random.Range(-10f, 10f)
                );
            }

            if (NavMesh.SamplePosition(navmeshPoint, out hit, 100.0f, NavMesh.AllAreas))
            {
                NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path);
            }

            // 重點：更新完這次才決定「下一次」要等多久
            if (targetDistance < combatRange)
                currentUpdateTime = Random.Range(combatRangeBrainMinUpdateTime, combatRangeBrainMaxUpdateTime);
            else
                currentUpdateTime = normalRangeBrainUpdateTime;
        }

        for (int i = 0; i < path.corners.Length - 1; i++)
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
    }
    public void FindPath()
    {
        float targetDistance = Vector3.Distance(transform.position, target.position);
        Vector3 navmeshPoint;
        NavMeshHit hit;
        if (targetDistance < 50)
        {
            navmeshPoint = new Vector3(target.position.x + Random.Range(-30, 30), 1.5f, target.position.z + Random.Range(-30, 30));
        }
        else
        {
            navmeshPoint = new Vector3(target.position.x + Random.Range(-10f, 10f), 1.5f, target.position.z + Random.Range(-10f, 10f));
        }
        if (NavMesh.SamplePosition(navmeshPoint, out hit, 100.0f, NavMesh.AllAreas))
        {
            NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path);
        }
    }

    public NavMeshPath GetPath()
    {
        return path;
    }
    public Transform GetTarget()
    {
        return target;
    }
    public Vector3 FindNextMoveLocation(Transform objectTransform)
    {
        if (path == null || objectTransform == null) return Vector3.zero;

        int len = path.corners != null ? path.corners.Length : 0;
        if (len == 0) return objectTransform.position;
        if (len == 1) return path.corners[0];

        float lowestDistance = Mathf.Infinity;
        int currentPointIndex = 0;

        for (int i = 0; i < len - 1; i++)
        {
            float d = Vector3.Distance(path.corners[i], objectTransform.position);
            if (d < lowestDistance)
            {
                lowestDistance = d;
                currentPointIndex = i;
            }
        }

        int nextIndex = currentPointIndex + 1;
        return path.corners[nextIndex];
    }
}
