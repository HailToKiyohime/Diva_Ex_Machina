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
    // OnDrawGizmos 用：存最後一次算出來的路徑，否則 gizmo 沒東西可畫
    private Vector3[] lastPath = System.Array.Empty<Vector3>();

    void Start()
    {
        path = new NavMeshPath();
    }

    public Vector3[] FindPath(Vector3 targetLocation)
    {
        Vector3[] result;
        if (isOnShip && isTargetOnShip)// if both side on ship, convert gobal coordinates to ship local coordinates
        {
            result = ComputeGhostPath(transform.position, targetLocation);
        }
        else if (isOnShip && !isTargetOnShip)// if entity on ship but target not on ship, find path to ship docking point
        {
            result = ComputeGhostPath(transform.position, GetTargetClosestDockingLocation());
        }
        else if (!isOnShip && isTargetOnShip) // if player not on ship but target on ship, find path to ship docking point
        {
            result = ComputeGroundPath(transform.position, GetClosestDockingLocation());
        }
        else
        {
            result = ComputeGroundPath(transform.position, targetLocation);
        }

        lastPath = result;   // 存起來給 OnDrawGizmos 用
        return result;
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
            return System.Array.Empty<Vector3>();

        NavMesh.CalculatePath(s.position, e.position, NavMesh.AllAreas, path);
        if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length == 0)
            return System.Array.Empty<Vector3>();

        Vector3[] g = path.corners;
        var outArr = new Vector3[g.Length];
        for (int i = 0; i < g.Length; i++)
            outArr[i] = ShipNavProjector.GhostToRealPoint(realShipRoot, ghostShipRoot, g[i]);
        return outArr;
    }

    private Vector3[] ComputeGroundPath(Vector3 startWorld, Vector3 endWorld)
    {
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
        if (!drawPathGizmo || lastPath == null || lastPath.Length == 0) return;

        Gizmos.color = pathColor;

        // 每個 corner 畫一顆小球
        for (int i = 0; i < lastPath.Length; i++)
            Gizmos.DrawSphere(lastPath[i], waypointGizmoRadius);

        // corner 之間連線
        for (int i = 0; i < lastPath.Length - 1; i++)
            Gizmos.DrawLine(lastPath[i], lastPath[i + 1]);

        // 從實體目前位置連到第一個 corner，看得出「接下來往哪走」
        Gizmos.DrawLine(transform.position, lastPath[0]);
    }
}