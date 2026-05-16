using GiantGrey.TileWorldCreator;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class CreatePath : MonoBehaviour
{
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
    public Transform Target => enemyBrain.targetList[0].target;

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
        // 切換到船上導航模式：啟用投影，設定船根和目標
        Debug.Log("Switching to Ship Navigation Mode");
        onShipNav = true;
        realShipRoot = realShip;
        ghostShipRoot = ghostShip;
        enemyBrain.targetList[0].target = newTarget;

        // 船上通常不需要 orbit 初始角度沿用（避免奇怪跳動）
        orbitAngleInitialized = false;

        FindPath();
        elapsed = 0f;
    }
    public void ClearShipNav()
    {
        // 清除船上導航模式：關閉投影，重置船根，保持當前目標（或可選擇重置）
        Debug.Log("Clearing Ship Navigation Mode");
        onShipNav = false;
        realShipRoot = null;
        ghostShipRoot = null;
        // 可選：重置目標為原本的 target（如果需要）
        // enemyBrain.targetList[0].target = originalTarget;
        FindPath();
        elapsed = 0f;
    }

    public void SetOverworldNav(Transform newTarget)
    {
        onShipNav = false;
        realShipRoot = null;
        ghostShipRoot = null;
        enemyBrain.targetList[0].target = newTarget;

        FindPath();
        elapsed = 0f;
    }

    void Update()
    {
        if (enemyBrain.targetList == null || enemyBrain.targetList[0].target == null) return;

        SelectTarget();

        float targetDistance = Vector3.Distance(transform.position, enemyBrain.targetList[0].target.position);
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
            {
                Vector3 a = ProjectCornerToReal(path.corners[i]);
                Vector3 b = ProjectCornerToReal(path.corners[i + 1]);
                Debug.DrawLine(a, b, Color.red);
            }
        }
    }

    public void FindPath()
    {
        if (enemyBrain.targetList[0].target == null)
        {

        }
        else
        {
            Vector3 startWorld = transform.position;
            Vector3 endWorld = enemyBrain.targetList[0].target.position;

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
                // 船上模式：在 ghost 空間套用與 GetDestinationPoint 相同的邏輯
                navmeshPoint = GetDestinationPoint_OnShipLocal(startWorld, endWorld);
            }

            NavMeshHit hit;
            NavMeshHit hit2;
            if (NavMesh.SamplePosition(navmeshPoint, out hit, 100.0f, NavMesh.AllAreas))
            {
                //Debug.Log($"Sampled NavMesh position: {hit.position} for target {navmeshPoint}");
                if (NavMesh.SamplePosition(navStart, out hit2, 100.0f, NavMesh.AllAreas))
                {
                    NavMesh.CalculatePath(hit2.position, hit.position, NavMesh.AllAreas, path);
                }
                else
                {
                    NavMesh.CalculatePath(navStart, hit.position, NavMesh.AllAreas, path);
                }
                int len = (path.corners != null) ? path.corners.Length : 0;
                followCornerIndex = (len > 1) ? 1 : 0;
            }
        }
    }

    public NavMeshPath GetPath() => path;
    public Transform GetTarget() => enemyBrain.targetList[0].target;

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

    public Vector3 GetDestinationPoint(Vector3 targetPosition)
    {
        float targetDistance = Vector3.Distance(transform.position, enemyBrain.targetList[0].target.position);

        if (targetDistance < combatRange)
        {
            if (circularPathFinding)
            {
                return GetNextOrbitPoint(
                    transform,
                    enemyBrain.targetList[0].target,
                    orbitRadius + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                    orbitStepDeg,
                    orbitClockwise,
                    orbitUseCurrentAsStart,
                    enemyBrain.targetList[0].target.position.y
                );
            }

            return new Vector3(
                enemyBrain.targetList[0].target.position.x + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset),
                1.5f,
                enemyBrain.targetList[0].target.position.z + Random.Range(-randomCombatRangeOffset, randomCombatRangeOffset)
            );
        }
        else
        {
            return new Vector3(
                enemyBrain.targetList[0].target.position.x + Random.Range(-10f, 10f),
                1.5f,
                enemyBrain.targetList[0].target.position.z + Random.Range(-10f, 10f)
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
        //select the closest docking point on the ship as the target
        if (landshipNavigation == null || landshipNavigation.dockingPoints == null || landshipNavigation.dockingPoints.Length == 0)
        {
            Debug.LogWarning("LandshipNavigation or docking points not set!");
            return;
        }
        else
        {
            //Search for the closest docking point
            Transform closestDockingPoint = null;
            float closestDistance = float.MaxValue;
            foreach (Transform dockingPoint in landshipNavigation.dockingPoints)
            {
                float distance = Vector3.Distance(transform.position, dockingPoint.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDockingPoint = dockingPoint;
                }
            }
            if (closestDockingPoint != null)
                enemyBrain.targetList[0].target = closestDockingPoint;
        }
    }
}
