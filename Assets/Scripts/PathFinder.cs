using UnityEngine;
using UnityEngine.AI;

public class PathFinder : MonoBehaviour
{
    public Vector3 targetLocation;
    public bool isTargetOnShip = false;
    public bool isOnShip = false;
    private NavMeshPath path;

    [SerializeField] private Transform realShipRoot;
    [SerializeField] private Transform ghostShipRoot;
    float navSampleMaxDistance = 100f; // Maximum distance for NavMesh.SamplePosition

    [Header("Debug Gizmo")]
    [SerializeField] private bool drawPathGizmo = true;
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private float waypointGizmoRadius = 0.3f;

    // ── Path cache ──────────────────────────────────────────────
    // lastPath      : 計算當下的 real world 快照（地面路徑用；船上路徑會 stale，僅供參考）
    // lastPathGhost : ghost 空間的原始 corner（船上路徑用，不會 stale），每幀再投影
    // lastPathOnShip: 這條路徑是不是船上算的 → 決定要不要每幀即時投影
    private Vector3[] lastPath = System.Array.Empty<Vector3>();
    private Vector3[] lastPathGhost = System.Array.Empty<Vector3>();
    private bool lastPathOnShip = false;

    void Start()
    {
        path = new NavMeshPath();
    }

    public Vector3[] FindPath(Vector3 targetLocation)
    {
        Vector3[] result;
        lastPathOnShip = false;   // 預設地面；下面命中 ghost 分支才設 true

        if (isOnShip && isTargetOnShip)// if both side on ship, convert gobal coordinates to ship local coordinates
        {
            result = ComputeGhostPath(transform.position, targetLocation);
            lastPathOnShip = true;
        }
        else if (isOnShip && !isTargetOnShip)// if entity on ship but target not on ship, find path to ship docking point
        {
            result = ComputeGhostPath(transform.position, GetTargetClosestDockingLocation());
            lastPathOnShip = true;
        }
        else if (!isOnShip && isTargetOnShip) // if player not on ship but target on ship, find path to ship docking point
        {
            result = ComputeGroundPath(transform.position, GetClosestDockingLocation());
        }
        else
        {
            result = ComputeGroundPath(transform.position, targetLocation);
        }

        lastPath = result;
        return result;
    }

    // ★ 每幀呼叫：船上路徑用「當下船姿態」即時投影 → 不 stale；地面路徑直接回傳
    public Vector3[] GetCurrentWorldPath()
    {
        if (!lastPathOnShip)
            return lastPath;   // 地面路徑不會動，世界座標本來就不 stale

        if (lastPathGhost == null || lastPathGhost.Length == 0)
            return System.Array.Empty<Vector3>();

        var outArr = new Vector3[lastPathGhost.Length];
        for (int i = 0; i < lastPathGhost.Length; i++)
            outArr[i] = ShipNavProjector.GhostToRealPoint(realShipRoot, ghostShipRoot, lastPathGhost[i]);
        return outArr;
    }

    public Vector3 GetClosestDockingLocation()
    {
        float closestDistance = Mathf.Infinity;
        Vector3 closestDockingPoint = Vector3.zero;
        for (int i = 0; i < LandshipNavigation.Instance.dockingPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, LandshipNavigation.Instance.dockingPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestDockingPoint = LandshipNavigation.Instance.dockingPoints[i].position;
            }
        }
        return closestDockingPoint;
    }

    public Vector3 GetTargetClosestDockingLocation()
    {
        float closestDistance = Mathf.Infinity;
        Vector3 closestDockingPoint = Vector3.zero;
        for (int i = 0; i < LandshipNavigation.Instance.dockingPoints.Length; i++)
        {
            float distance = Vector3.Distance(targetLocation, LandshipNavigation.Instance.dockingPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestDockingPoint = LandshipNavigation.Instance.dockingPoints[i].position;
            }
        }
        return closestDockingPoint;
    }

    private Vector3[] ComputeGhostPath(Vector3 startWorld, Vector3 endWorld)
    {
        Vector3 navStart = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, startWorld);
        Vector3 navEnd = ShipNavProjector.RealToGhostPoint(realShipRoot, ghostShipRoot, endWorld);

        if (!NavMesh.SamplePosition(navStart, out NavMeshHit s, navSampleMaxDistance, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(navEnd, out NavMeshHit e, navSampleMaxDistance, NavMesh.AllAreas))
        {
            lastPathGhost = System.Array.Empty<Vector3>();
            return System.Array.Empty<Vector3>();
        }

        NavMesh.CalculatePath(s.position, e.position, NavMesh.AllAreas, path);
        if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length == 0)
        {
            lastPathGhost = System.Array.Empty<Vector3>();
            return System.Array.Empty<Vector3>();
        }

        Vector3[] g = path.corners;

        // ★ 存 ghost 空間原始 corner（不會隨船移動而 stale），供 GetCurrentWorldPath 每幀投影
        lastPathGhost = (Vector3[])g.Clone();

        // return 當下的 real world 版本（呼叫端 SetPath 立即使用一次）
        var outArr = new Vector3[g.Length];
        for (int i = 0; i < g.Length; i++)
            outArr[i] = ShipNavProjector.GhostToRealPoint(realShipRoot, ghostShipRoot, g[i]);
        return outArr;
    }

    private Vector3[] ComputeGroundPath(Vector3 startWorld, Vector3 endWorld)
    {
        // 地面路徑：清掉 ghost 快取，避免萬一 lastPathOnShip 判斷有誤時投影到舊資料
        lastPathGhost = System.Array.Empty<Vector3>();

        if (!NavMesh.SamplePosition(startWorld, out NavMeshHit s, navSampleMaxDistance, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(endWorld, out NavMeshHit e, navSampleMaxDistance, NavMesh.AllAreas))
            return System.Array.Empty<Vector3>();

        NavMesh.CalculatePath(s.position, e.position, NavMesh.AllAreas, path);
        if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length == 0)
            return System.Array.Empty<Vector3>();

        return path.corners;
    }

    private void OnDrawGizmos()
    {
        if (!drawPathGizmo) return;
        if (!Application.isPlaying) return;   // 編輯模式沒有即時 path，避免雜訊 / 空引用

        // ★ 用即時投影版：船上路徑會貼著船跑，不再停在舊位置
        Vector3[] draw = GetCurrentWorldPath();
        if (draw == null || draw.Length == 0) return;

        Gizmos.color = pathColor;

        for (int i = 0; i < draw.Length; i++)
            Gizmos.DrawSphere(draw[i], waypointGizmoRadius);

        for (int i = 0; i < draw.Length - 1; i++)
            Gizmos.DrawLine(draw[i], draw[i + 1]);

        Gizmos.DrawLine(transform.position, draw[0]);
    }
}