using UnityEngine;
using UnityEngine.AI;

public class PathFinder : MonoBehaviour
{
    public Vector3 targetLocation;
    public bool isTargetOnShip = false;
    public bool isOnShip = false;
    private NavMeshPath path;

    // ── 船的 root：唯一來源是 LandshipNavigation ─────────────────────────
    //
    // 原本這裡是兩個 [SerializeField] Transform，靠 Inspector 逐一指定。
    // Prefab 存不了場景物件的引用，所以任何在執行期 Instantiate 出來的敵人
    // （EnemySpawner 生的、物件池生的）那兩個欄位必然是 None。
    // 一旦 SyncShipFlags 把 isOnShip / isTargetOnShip 設成 true，
    // ShipNavProjector 就會拿 null 去做投影 → NRE → FixedUpdate 中斷 →
    // 那隻敵人連移動都停掉。症狀是「平常正常，一靠近船整個 AI 死掉」。
    //
    // 現在一律讀 LandshipNavigation.Instance，場景裡指一次就好，
    // 生成端（EnemySpawner）一行都不用改。
    //
    // ⚠ realShip 與 ghostShip 必須是同一個物件的「本尊 / 分身」關係。
    //   指錯不會報錯，只會讓船上的路徑投影到錯誤的世界座標。


    private Transform RealShipRoot
    {
        get
        {
            LandshipNavigation nav = LandshipNavigation.Instance;
            return (nav != null) ? nav.realShip : null;
        }
    }

    private Transform GhostShipRoot
    {
        get
        {
            LandshipNavigation nav = LandshipNavigation.Instance;
            return (nav != null) ? nav.ghostShip : null;
        }
    }

    /// <summary>兩個 root 都拿得到才能做投影。</summary>
    private bool HasShipRoots => RealShipRoot != null && GhostShipRoot != null;
    float navSampleMaxDistance = 30f; // Maximum distance for NavMesh.SamplePosition

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

    [SerializeField] private float disembarkDistance = 50f;
    void Start()
    {
        path = new NavMeshPath();
    }

    /// <summary>
    /// 上一次算出來的是不是「保底路徑」（NavMesh 什麼都沒算出來，改用目的地直線）。
    /// 呼叫端可以用它來縮短重試間隔或做別的降級處理。
    /// </summary>
    public bool LastPathIsFallback { get; private set; }

    public Vector3[] FindPath(Vector3 targetLocation)
    {
        Vector3[] result;
        lastPathOnShip = false;   // 預設地面；下面命中 ghost 分支才設 true
        LastPathIsFallback = false;

        if (isOnShip && isTargetOnShip)// if both side on ship, convert gobal coordinates to ship local coordinates
        {
            result = ComputeGhostPath(transform.position, targetLocation);
            lastPathOnShip = true;

            if (result.Length == 0)
                result = BuildFallbackPath(targetLocation, true);
        }
        else if (isOnShip && !isTargetOnShip)// if entity on ship but target not on ship
        {
            // 先算「走到 dock」的船上路徑
            Vector3 dockPos = GetTargetClosestDockingLocation();
            result = ComputeGhostPath(transform.position, dockPos);
            lastPathOnShip = true;

            // ★ 在路徑尾端追加一個「離船點」：dock 往船外方向延伸幾公尺。
            //   實體為了走到它，必然跨出船的 trigger → isOnShip 翻 false
            //   → 下次 PathUpdate 自然改用地面路徑直奔目標。
            if (result.Length > 0)
            {
                Vector3 exitPoint = GetDisembarkPoint(dockPos);
                var extended = new Vector3[result.Length + 1];
                result.CopyTo(extended, 0);
                extended[extended.Length - 1] = exitPoint;
                result = extended;

                // ghost 快取也要同步追加，否則 GetCurrentWorldPath 投影出來會少最後一點
                var extendedGhost = new Vector3[lastPathGhost.Length + 1];
                lastPathGhost.CopyTo(extendedGhost, 0);
                extendedGhost[extendedGhost.Length - 1] =
                    ShipNavProjector.RealToGhostPoint(RealShipRoot, GhostShipRoot, exitPoint);
                lastPathGhost = extendedGhost;
            }
            else
            {
                result = BuildFallbackPath(dockPos, true);
            }
        }
        else if (!isOnShip && isTargetOnShip) // if player not on ship but target on ship, find path to ship docking point
        {
            Vector3 dockPos = GetClosestDockingLocation();
            result = ComputeGroundPath(transform.position, dockPos);

            if (result.Length == 0)
                result = BuildFallbackPath(dockPos, false);
        }
        else
        {
            result = ComputeGroundPath(transform.position, targetLocation);

            if (result.Length == 0)
                result = BuildFallbackPath(targetLocation, false);
        }

        lastPath = result;
        return result;
    }

    /// <summary>
    /// 算不出路徑時的保底：回傳「只有目的地一個點」的路徑，而不是空陣列。
    ///
    /// ★ 這是這次修的 bug 的一半。
    ///   舊版只要 SamplePosition 或 CalculatePath 失敗一次就回傳空陣列，
    ///   呼叫端（Brain）拿到空路徑就整段 return → 實體停在原地不動。
    ///   而「原地」正是剛剛 sample 失敗的那個位置，所以下一次 FindPath 的
    ///   起點還是同一個點 → 再失敗 → 永遠失敗。一次失誤就變成永久死鎖。
    ///
    ///   給一個直線保底點，實體至少會離開那個壞位置，
    ///   移動之後 NavMesh.SamplePosition 才有機會重新成功。
    /// </summary>
    private Vector3[] BuildFallbackPath(Vector3 endWorld, bool ghostSpace)
    {
        LastPathIsFallback = true;

        if (ghostSpace && HasShipRoots)
        {
            // 船上的保底點也要存 ghost 空間，GetCurrentWorldPath 才會每幀跟著船投影
            lastPathOnShip = true;
            lastPathGhost = new Vector3[]
            {
                ShipNavProjector.RealToGhostPoint(RealShipRoot, GhostShipRoot, endWorld)
            };
        }
        else
        {
            lastPathOnShip = false;
            lastPathGhost = System.Array.Empty<Vector3>();
        }

        return new Vector3[] { endWorld };
    }

    // ★ 每幀呼叫：船上路徑用「當下船姿態」即時投影 → 不 stale；地面路徑直接回傳
    public Vector3[] GetCurrentWorldPath()
    {
        if (!lastPathOnShip)
            return lastPath;   // 地面路徑不會動，世界座標本來就不 stale

        if (lastPathGhost == null || lastPathGhost.Length == 0 || !HasShipRoots)
            return System.Array.Empty<Vector3>();

        var outArr = new Vector3[lastPathGhost.Length];
        for (int i = 0; i < lastPathGhost.Length; i++)
            outArr[i] = ShipNavProjector.GhostToRealPoint(RealShipRoot, GhostShipRoot, lastPathGhost[i]);
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
        // 兩個 root 都拿不到（場景沒有 LandshipNavigation、或它沒指好）→
        // 回傳空陣列讓呼叫端走保底直線，而不是把 null 丟進 ShipNavProjector 直接 NRE。
        if (!HasShipRoots)
        {
            lastPathGhost = System.Array.Empty<Vector3>();
            return System.Array.Empty<Vector3>();
        }

        Vector3 navStart = ShipNavProjector.RealToGhostPoint(RealShipRoot, GhostShipRoot, startWorld);
        Vector3 navEnd = ShipNavProjector.RealToGhostPoint(RealShipRoot, GhostShipRoot, endWorld);

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
            outArr[i] = ShipNavProjector.GhostToRealPoint(RealShipRoot, GhostShipRoot, g[i]);
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
    private Vector3 GetDisembarkPoint(Vector3 dockPos)
    {
        Transform ship = RealShipRoot;
        if (ship == null) return dockPos;   // 沒有 root 就沒有「往船外」的方向可言

        Vector3 outward = dockPos - ship.position;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f) outward = ship.forward;   // dock 在船中心的退路
        outward.Normalize();

        Vector3 candidate = dockPos + outward * disembarkDistance;

        // 貼回地面 navmesh（船外是地面 navmesh 的領域）
        if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit,
                navSampleMaxDistance, UnityEngine.AI.NavMesh.AllAreas))
            return hit.position;

        return candidate;   // sample 失敗就用原始點，至少方向是對的
    }
}