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

    [Range(1, 50)]
    [SerializeField] private float combatRange;
    [SerializeField] private float randomCombatRangeOffset;
    [SerializeField] private float combatRangeBrainMinUpdateTime;
    [SerializeField] private float combatRangeBrainMaxUpdateTime;
    [SerializeField] private float normalRangeBrainUpdateTime;

    private float elapsed = 0.0f;
    private float currentUpdateTime;

    public bool circularPathFinding = false;

    [Header("Circular Orbit")]
    [SerializeField] private float orbitStepDeg = 20f;
    [SerializeField] private float orbitRadius = 10f;
    [SerializeField] private bool orbitClockwise = true;
    [SerializeField] private bool orbitUseCurrentAsStart = true;
    private float orbitAngleDeg;
    private bool orbitAngleInitialized;

    // ============================
    // Ship Nav Projection (NEW)
    // ============================
    [Header("Ship Nav Projection")]
    [SerializeField] private bool onShipNav = false;
    [SerializeField] private Transform realShipRoot;
    [SerializeField] private Transform ghostShipRoot;

    public float CombatRange => combatRange;
    public Transform Target => target;

    void Start()
    {
        path = new NavMeshPath();
        elapsed = 0.0f;
        currentUpdateTime = normalRangeBrainUpdateTime;
    }

    // ============================
    // Public API (NEW)
    // ============================
    public void SetOnShipNav(Transform realShip, Transform ghostShip, Transform newTarget)
    {
        // 切換到船上導航模式：啟用投影，設定船根和目標
        Debug.Log("Switching to Ship Navigation Mode");
        onShipNav = true;
        realShipRoot = realShip;
        ghostShipRoot = ghostShip;
        target = newTarget;

        // 船上通常不需要 orbit 初始角度沿用（避免奇怪跳動）
        orbitAngleInitialized = false;

        FindPath();
        elapsed = 0f;
    }

    public void SetOverworldNav(Transform newTarget)
    {
        onShipNav = false;
        realShipRoot = null;
        ghostShipRoot = null;
        target = newTarget;

        FindPath();
        elapsed = 0f;
    }

    void Update()
    {
        if (target == null) return;

        float targetDistance = Vector3.Distance(transform.position, target.position);
        elapsed += Time.deltaTime;

        if (elapsed > currentUpdateTime)
        {
            elapsed = 0f;

            FindPath();

            // 更新下一次刷新頻率（維持你原本行為）
            if (targetDistance < combatRange)
                currentUpdateTime = Random.Range(combatRangeBrainMinUpdateTime, combatRangeBrainMaxUpdateTime);
            else
                currentUpdateTime = normalRangeBrainUpdateTime;
        }

        // Debug：corners 是「計算座標系」下的點（船上模式會畫在 ghostShip 那邊）
        if (path != null && path.corners != null)
        {
            for (int i = 0; i < path.corners.Length - 1; i++)
                Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
        }
    }

    public void FindPath()
    {
        if (target == null) return;

        Vector3 startWorld = transform.position;
        Vector3 endWorld = target.position;

        // 1) 取得計算用 start/end（可能投影到 ghost）
        Vector3 navStart = startWorld;
        Vector3 navEnd = endWorld;

        if (onShipNav && realShipRoot != null && ghostShipRoot != null)
        {
            navStart = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, startWorld);
            navEnd = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, endWorld);
        }

        // 2) 目的地點：Overworld 用你原本 random offset；OnShip 先用「純目標」
        Vector3 navmeshPoint = navEnd;

        if (!onShipNav)
        {
            navmeshPoint = GetDestinationPoint(endWorld); // 你原本行為：帶 offset / orbit
        }
        else
        {
            // 船上模式建議先穩定：不要 random offset（避免把點偏到牆後）
            // 如果你之後想要船上也有 offset，我可以再幫你加「在 ghost 空間 offset」版本
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(navmeshPoint, out hit, 100.0f, NavMesh.AllAreas))
        {
            NavMesh.CalculatePath(navStart, hit.position, NavMesh.AllAreas, path);
        }
    }

    public NavMeshPath GetPath() => path;
    public Transform GetTarget() => target;

    public Vector3 FindNextMoveLocation(Transform objectTransform)
    {
        if (path == null || objectTransform == null) return Vector3.zero;

        int len = path.corners != null ? path.corners.Length : 0;
        if (len == 0) return objectTransform.position;

        if (len == 1)
            return ProjectCornerToReal(path.corners[0]);

        // ✅ 用同座標系去找最近 corner
        Vector3 queryPos = objectTransform.position;
        if (onShipNav && realShipRoot != null && ghostShipRoot != null)
        {
            queryPos = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, objectTransform.position);
        }

        float lowestDistance = Mathf.Infinity;
        int currentPointIndex = 0;

        for (int i = 0; i < len - 1; i++)
        {
            float d = Vector3.Distance(path.corners[i], queryPos);
            if (d < lowestDistance)
            {
                lowestDistance = d;
                currentPointIndex = i;
            }
        }

        int nextIndex = Mathf.Clamp(currentPointIndex + 1, 0, len - 1);
        return ProjectCornerToReal(path.corners[nextIndex]);
    }

    private Vector3 ProjectCornerToReal(Vector3 cornerWorld)
    {
        if (onShipNav && realShipRoot != null && ghostShipRoot != null)
        {
            return ShipNavProjector.GhostToRealPoint(realShipRoot, ghostShipRoot, cornerWorld);
        }
        return cornerWorld;
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
                    orbitRadius + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                    orbitStepDeg,
                    orbitClockwise,
                    orbitUseCurrentAsStart,
                    target.position.y
                );
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
        if (!orbitAngleInitialized)
        {
            if (useCurrentAsStart)
            {
                Vector3 toEnemy = self.position - centerTarget.position;
                toEnemy.y = 0f;

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

        orbitAngleDeg += (clockwise ? -stepDeg : stepDeg);
        orbitAngleDeg = Mathf.Repeat(orbitAngleDeg, 360f);

        float rad = orbitAngleDeg * Mathf.Deg2Rad;

        return new Vector3(
            centerTarget.position.x + Mathf.Cos(rad) * radius,
            centerY,
            centerTarget.position.z + Mathf.Sin(rad) * radius
        );
    }
}
