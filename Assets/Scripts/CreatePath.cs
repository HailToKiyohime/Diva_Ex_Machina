using GiantGrey.TileWorldCreator;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class CreatePath : MonoBehaviour
{
    /*
    private Transform _navTarget;
    public bool HasNavTarget => _navTarget != null;

    private EnemyBrain enemyBrain; // Reference to EnemyBrain for state info

    private NavMeshPath path;

    [Range(1, 100)]
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
    public Transform Target => enemyBrain.currentTargetTransform;

    [SerializeField] private float cornerReachDist = 1.2f;
    private int followCornerIndex = 1; // 通常 corners[0] 是起點，所以從 1 開始追

    public LandshipNavigation landshipNavigation; // Reference to LandshipNavigation

    void Start()
    {
        enemyBrain = GetComponent<EnemyBrain>();
        path = new NavMeshPath();
        elapsed = 0.0f;
        currentUpdateTime = normalRangeBrainUpdateTime;
    }

    // ============================
    // Public API (NEW)
    // ============================
    public void SetOnShipNav(Transform realShip, Transform ghostShip, Transform newTarget)
    {
        onShipNav = true;
        realShipRoot = realShip;
        ghostShipRoot = ghostShip;
        orbitAngleInitialized = false;

        _navTarget = newTarget;                   // 導航目標
        //enemyBrain.ForceSetTarget(newTarget);     // 戰鬥目標（加入 targetList）
        elapsed = 0f;
    }

    public void ClearShipNav()
    {
        onShipNav = false;
        realShipRoot = null;
        ghostShipRoot = null;
        _navTarget = null; // ← 讓 SelectTarget 重選，不留殘值
        FindPath();
        elapsed = 0f;
    }

    public void SetOverworldNav(Transform newTarget)
    {
        onShipNav = false;
        realShipRoot = null;
        ghostShipRoot = null;

        enemyBrain.ForceSetTarget(newTarget); // ✅ 同步 Brain
        elapsed = 0f;
    }

    void Update()
    {
        UpdateNavTarget(); // ← 取代 SelectTarget()

        Transform navDest = _navTarget ?? enemyBrain.currentTargetTransform;
        if (navDest == null) return;

        elapsed += Time.deltaTime;
        if (elapsed > currentUpdateTime)
        {
            elapsed = 0f;
            FindPath();

            // 頻率判斷用戰鬥目標距離，不是 navTarget 距離
            float combatDist = enemyBrain.currentTargetTransform != null
                ? Vector3.Distance(transform.position, enemyBrain.currentTargetTransform.position)
                : float.MaxValue;

            currentUpdateTime = combatDist < combatRange
                ? Random.Range(combatRangeBrainMinUpdateTime, combatRangeBrainMaxUpdateTime)
                : normalRangeBrainUpdateTime;
        }

        // Debug
        if (path != null && path.corners != null)
        {
            for (int i = 0; i < path.corners.Length - 1; i++)
                Debug.DrawLine(ProjectCornerToReal(path.corners[i]),
                               ProjectCornerToReal(path.corners[i + 1]), Color.red);
        }
    }

    public void FindPath()
    {
        Transform destination = _navTarget ?? enemyBrain.currentTargetTransform;
        if (destination == null) return; // ✅ 統一守衛，不再分兩條路

        Vector3 startWorld = transform.position;
        Vector3 endWorld = destination.position;

        Vector3 navStart = startWorld;
        //Vector3 navEnd = endWorld;

        if (onShipNav && realShipRoot != null && ghostShipRoot != null)
        {
            navStart = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, startWorld);
            //navEnd = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, endWorld);
        }

        Vector3 navmeshPoint = !onShipNav
            ? GetDestinationPoint(destination)          // ✅ 傳入 destination
            : GetDestinationPoint_OnShipLocal(startWorld, endWorld);

        NavMeshHit hit, hit2;
        if (NavMesh.SamplePosition(navmeshPoint, out hit, 100.0f, NavMesh.AllAreas))
        {
            if (NavMesh.SamplePosition(navStart, out hit2, 100.0f, NavMesh.AllAreas))
                NavMesh.CalculatePath(hit2.position, hit.position, NavMesh.AllAreas, path);
            else
                NavMesh.CalculatePath(navStart, hit.position, NavMesh.AllAreas, path);

            int len = (path.corners != null) ? path.corners.Length : 0;
            followCornerIndex = (len > 1) ? 1 : 0;
        }
    }

    public NavMeshPath GetPath() => path;
    public Transform GetTarget() => enemyBrain.currentTargetTransform;

    public Vector3 FindNextMoveLocation(Transform objectTransform)
    {
        if (path == null || objectTransform == null) return Vector3.zero;

        int len = path.corners != null ? path.corners.Length : 0;
        if (len == 0) return objectTransform.position;
        if (len == 1) return ProjectCornerToReal(path.corners[0]);

        // ✅ 用同座標系（ghost/real）計算距離
        Vector3 queryPos = objectTransform.position;
        if (onShipNav && realShipRoot != null && ghostShipRoot != null)
            queryPos = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, objectTransform.position);

        // clamp 防爆
        followCornerIndex = Mathf.Clamp(followCornerIndex, 0, len - 1);

        // 如果已經接近目前追的 corner，就往下一個推進（可以一次跳過多個很短的角點）
        while (followCornerIndex < len - 1 &&
               Vector3.Distance(path.corners[followCornerIndex], queryPos) <= cornerReachDist)
        {
            followCornerIndex++;
        }

        return ProjectCornerToReal(path.corners[followCornerIndex]);
    }

    private Vector3 ProjectCornerToReal(Vector3 cornerWorld)
    {
        if (onShipNav && realShipRoot != null && ghostShipRoot != null)
        {
            return ShipNavProjector.GhostToRealPoint(realShipRoot, ghostShipRoot, cornerWorld);
        }
        return cornerWorld;
    }

    public Vector3 GetDestinationPoint(Transform destination) // ✅ 改為接收參數
    {
        float targetDistance = Vector3.Distance(transform.position, destination.position);

        if (targetDistance < combatRange)
        {
            if (circularPathFinding)
            {
                return GetNextOrbitPoint(
                    transform,
                    destination,
                    orbitRadius + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                    orbitStepDeg,
                    orbitClockwise,
                    orbitUseCurrentAsStart,
                    destination.position.y  // ✅ 不再用 targetList[0]
                );
            }

            return new Vector3(
                destination.position.x + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                1.5f,
                destination.position.z + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset)
            );
        }
        else
        {
            return new Vector3(
                destination.position.x + Random.Range(-10f, 10f),
                1.5f,
                destination.position.z + Random.Range(-10f, 10f)
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

    private Vector3 GetDestinationPoint_OnShipLocal(Vector3 selfWorld, Vector3 targetWorld)
    {
        if (realShipRoot == null || ghostShipRoot == null)
            return GetDestinationPointInSpace(selfWorld, targetWorld, targetWorld.y);

        Vector3 selfLocal = realShipRoot.InverseTransformPoint(selfWorld);
        Vector3 targetLocal = realShipRoot.InverseTransformPoint(targetWorld);

        float yLocal = targetLocal.y;

        Vector3 destLocal = GetDestinationPointInSpace(selfLocal, targetLocal, yLocal);

        Vector3 destWorldGhost = ghostShipRoot.TransformPoint(destLocal);
        return destWorldGhost;
    }
    private Vector3 GetDestinationPointInSpace(Vector3 selfPos, Vector3 targetPos, float targetY)
    {
        float targetDistance = Vector3.Distance(selfPos, targetPos);

        if (targetDistance < combatRange)
        {
            if (circularPathFinding)
            {
                return GetNextOrbitPoint_Pos(
                    selfPos,
                    targetPos,
                    orbitRadius + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                    orbitStepDeg,
                    orbitClockwise,
                    orbitUseCurrentAsStart,
                    targetY
                );
            }

            return new Vector3(
                targetPos.x + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                targetY,
                targetPos.z + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset)
            );
        }

        return new Vector3(
            targetPos.x + Random.Range(-10f, 10f),
            targetY,
            targetPos.z + Random.Range(-10f, 10f)
        );
    }
    private Vector3 GetNextOrbitPoint_Pos(
    Vector3 selfPos,
    Vector3 centerPos,
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
                Vector3 toEnemy = selfPos - centerPos;
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
            centerPos.x + Mathf.Cos(rad) * radius,
            centerY,
            centerPos.z + Mathf.Sin(rad) * radius
        );
    }

    public void SelectTarget()
    {
        if (onShipNav) return;

        // ✅ 已有戰鬥目標：清除 nav override，讓 currentTargetTransform 同時驅動移動和攻擊
        if (enemyBrain.currentTargetTransform != null)
        {
            _navTarget = null;
            return;
        }

        // 沒有戰鬥目標：導航到最近 Docking Point（純移動，不攻擊）
        if (landshipNavigation == null || landshipNavigation.dockingPoints == null
            || landshipNavigation.dockingPoints.Length == 0) return;

        Transform closestDockingPoint = null;
        float closestDistance = float.MaxValue;
        foreach (Transform dp in landshipNavigation.dockingPoints)
        {
            float d = Vector3.Distance(transform.position, dp.position);
            if (d < closestDistance) { closestDistance = d; closestDockingPoint = dp; }
        }

        if (closestDockingPoint != null && closestDockingPoint != _navTarget)
            _navTarget = closestDockingPoint;
    }

    public void UpdateNavTarget()
    {
        if (enemyBrain.currentTargetTransform == null) return;

        if (onShipNav)
        {
            // ✅ 已在船上：直接追 currentTargetTransform，不需要 docking point
            // onShipNav 只由 OnTriggerExit (EnemyMovement) 清除，不在這裡判斷
            _navTarget = enemyBrain.currentTargetTransform;
            return;
        }

        // 不在船上：判斷目標是否在船上決定是否要走 docking point
        if (IsTargetOnShip(enemyBrain.currentTargetTransform))
        {
            Transform dp = GetNearestDockingPoint();
            if (dp != null) _navTarget = dp;
        }
        else
        {
            _navTarget = enemyBrain.currentTargetTransform;
        }
    }

    private bool IsTargetOnShip(Transform target)
    {
        if (landshipNavigation == null) return false;
        if (target.IsChildOf(landshipNavigation.transform)) return true;

        var passenger = target.GetComponent<ShipPassenger>();
        if (passenger != null && passenger.IsOnShip) return true;

        return false;
    }

    private Transform GetNearestDockingPoint()
    {
        if (landshipNavigation == null || landshipNavigation.dockingPoints == null) return null;

        Transform closest = null;
        float closestDist = float.MaxValue;
        foreach (Transform dp in landshipNavigation.dockingPoints)
        {
            if (dp == null) continue;
            float d = Vector3.Distance(transform.position, dp.position);
            if (d < closestDist) { closestDist = d; closest = dp; }
        }
        return closest;
    }*/
}
