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
    void Start()
    {
        path = new NavMeshPath();
    }

    public Vector3[] FindPath(Vector3 targetLocation)
    {
        if (isOnShip && isTargetOnShip)// if both side on ship, convert gobal coordinates to ship local coordinates
        {
            return ComputeGhostPath(transform.position, targetLocation);
        }
        else if (isOnShip && !isTargetOnShip)// if entity on ship but target not on ship, find path to ship docking point
        {
            return ComputeGhostPath(transform.position, GetTargetClosestDockingLocation());
        }
        else if (!isOnShip && isTargetOnShip) // if player not on ship but target on ship, find path to ship docking point
        {
            // 敵人在地面 → 先走到最近的 dock（純地面 navmesh，不投影）
            // 踏上船後 isOnShip 變 true，下次 FindPath 落到分支① 用 ghost 算後半段
            return ComputeGroundPath(transform.position, GetClosestDockingLocation());
        }
        else
        {
            // 敵人在地面 → 先走到最近的 dock（純地面 navmesh，不投影）
            // 踏上船後 isOnShip 變 true，下次 FindPath 落到分支① 用 ghost 算後半段
            return ComputeGroundPath(transform.position, targetLocation);
        }
    }
    public Vector3 GetClosestDockingLocation()
    {
        //find the closest docking point to the entity and return its position
        float closestDistance = Mathf.Infinity;
        Vector3 closestDockingPoint = Vector3.zero;
        for (int i = 0; i <LandshipNavigation.Instance.dockingPoints.Length; i++)
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
        //find the closest docking point to the entity and return its position
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

    // 起點/終點都給 real 世界座標，回傳 real 世界座標的路徑
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
}
