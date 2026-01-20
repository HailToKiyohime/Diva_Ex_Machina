using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
public class CreatePath : MonoBehaviour
{
    public Transform target;
    public Vector3[] midwayPoint;
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

    public bool circularPathFinding = false;

    [Header("Circular Orbit")]
    [SerializeField] private float orbitStepDeg = 20f;   // 每次重算前進幾度
    [SerializeField] private float orbitRadius = 10f;
    [SerializeField] private bool orbitClockwise = true; // true=順時針(角度遞減), false=逆時針(角度遞增)
    [SerializeField] private bool orbitUseCurrentAsStart = true; // 初始角度用敵人目前方位，或用 random
    private float orbitAngleDeg;
    private bool orbitAngleInitialized;
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


            navmeshPoint = GetDestinationPoint(target.position);


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
        Vector3 navmeshPoint;
        NavMeshHit hit;

        navmeshPoint = GetDestinationPoint(target.position);


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
    public Vector3 GetDestinationPoint(Vector3 targetPosition)
    {
        float targetDistance = Vector3.Distance(transform.position, target.position);
        if (targetDistance < combatRange)
        {
            if (circularPathFinding)
            {
                return GetNextOrbitPoint(
                transform,
                target,
                orbitRadius,
                orbitStepDeg,
                orbitClockwise,
                orbitUseCurrentAsStart,
                target.position.y
                ) + new Vector3(Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset), 0, Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset));
            }

            return new Vector3(
                target.position.x + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                1.5f,
                target.position.z + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset)
            );
        }
        else
        {
            return new Vector3(
                target.position.x + Random.Range(-10f, 10f),
                1.5f,
                target.position.z + Random.Range(-10f, 10f)
            );
        }

    }


    public Vector3[] BuildPointsAround(
    Transform center,
    float radius,
    int count = 8,
    float startAngleDeg = 0f,
    bool keepCenterY = true
)
    {
        if (count <= 0) return System.Array.Empty<Vector3>();

        Vector3 c = center.position;
        float y = keepCenterY ? c.y : 0f;

        Vector3[] points = new Vector3[count];
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angleDeg = startAngleDeg + step * i;
            float rad = angleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radius;
            float z = Mathf.Sin(rad) * radius;

            points[i] = new Vector3(c.x + x, y, c.z + z);
        }

        return points;
    }

    private Vector3 GetNextOrbitPoint(
    Transform self,
    Transform centerTarget,
    float radius,
    float stepDeg,
    bool clockwise,
    bool useCurrentAsStart,
    float centerY
)
    {
        // 初始化角度（只做一次）
        if (!orbitAngleInitialized)
        {
            if (useCurrentAsStart)
            {
                Vector3 toEnemy = self.position - centerTarget.position;
                toEnemy.y = 0f;

                // 如果剛好重疊，避免 Atan2(0,0) 造成不穩
                if (toEnemy.sqrMagnitude < 0.0001f)
                    orbitAngleDeg = Random.Range(0f, 360f);
                else
                    orbitAngleDeg = Mathf.Atan2(toEnemy.z, toEnemy.x) * Mathf.Rad2Deg;
            }
            else
            {
                orbitAngleDeg = Random.Range(0f, 360f);
            }

            orbitAngleInitialized = true;
        }

        // 推進角度：順/逆時針
        orbitAngleDeg += (clockwise ? -stepDeg : stepDeg);

        // 角度保持在 0~360 內，避免長期累積變很大（非必要但乾淨）
        orbitAngleDeg = Mathf.Repeat(orbitAngleDeg, 360f);

        float rad = orbitAngleDeg * Mathf.Deg2Rad;

        // 回傳未校正的圓周點（y 給你控制：通常用 target 的 y）
        return new Vector3(
            centerTarget.position.x + Mathf.Cos(rad) * radius,
            centerY,
            centerTarget.position.z + Mathf.Sin(rad) * radius
        );
    }
}
