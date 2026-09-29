using UnityEngine;

/// <summary>
/// 實體的「在不在移動平台上」狀態，以及那個平台的 Rigidbody。
///
/// 專案裡「在不在船上」只有這一個真相來源：
///   PathFinder（走 ghost 空間還是地面空間）   ← ModularEntityBrain.SyncShipFlags
///   ModularEntityMovement（要不要疊平台速度）  ← GetMobilePlatformVelocity
///
/// 偵測方式：每個 physics step 從自己的位置往正下方打一條射線（預設 60 公尺），
/// 只打 groundLayers（地形 + 船）。
///   第一個打到的是船（platformLayers）→ 在船上
///   打到地形，或什麼都沒打到             → 不在船上
/// 所以跳起來、飛在甲板上方時仍然算在船上，不會在空中突然失去船速。
///
/// 不再依賴 trigger：不需要 Mobile Platform Hitbox、不需要 Rigidbody、
/// 也不必跟 collider 掛在同一個 GameObject 上。
///
/// 有移動腳本的實體（ModularEntityMovement / PlayerMovement）：
///   地面偵測和船上偵測共用同一條射線 —— 移動腳本的 GroundCheck 打完之後
///   呼叫 ReportGroundHit() 把結果交過來，這裡就不再自己打。
///   移動腳本停止回報（例如被停用）超過一個 physics step，會自動退回自己打射線。
/// 沒有移動腳本的物件：照舊每個 physics step 自己打一條。
/// </summary>
public class ShipPassenger : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("射線會打的「地面」層（例如地形 Ground）。留空（Nothing）會在執行時自動用 Ground。\n" +
             "Platform Layers 一定會被一起打，不用重複勾。\n" +
             "不要包含實體自己的層（Player / Enemy），否則會打到自己。")]
    [SerializeField] private LayerMask groundLayers;

    [Tooltip("這些層代表「船」。射線第一個打到的東西在這些層上，就算在船上。\n" +
             "留空（Nothing）會在執行時自動用 Mobile Platform。")]
    [SerializeField] private LayerMask platformLayers;

    [Tooltip("往下偵測的最大距離（公尺）。")]
    [SerializeField] private float maxDistance = 60f;

    [Tooltip("射線起點往上抬多少（公尺）。pivot 在腳底時，抬一點才不會從甲板表面底下開始打而漏掉甲板。")]
    [SerializeField] private float rayStartOffset = 0.5f;

    [SerializeField] private bool drawDebugRay = false;

    [Header("Fixed Objects")]
    [Tooltip("固定在船上的物件（建築、砲塔）用。\n" +
             "開啟後，只要這個物件是真實船（LandshipNavigation.realShip）的子物件，就視為在船上，\n" +
             "不打射線。會走動的實體（敵人、玩家）請保持關閉。")]
    [SerializeField] private bool detectByHierarchy = false;

    /// <summary>目前是否在移動平台上（腳下 maxDistance 內第一個地面是船）。</summary>
    public bool isOnShip { get; private set; }

    /// <summary>目前所在平台的 Rigidbody；不在平台上時為 null。</summary>
    public Rigidbody PlatformRigidbody { get; private set; }

    /// <summary>平台速度，不在平台上時為 Vector3.zero。</summary>
    public Vector3 PlatformVelocity =>
        (isOnShip && PlatformRigidbody != null) ? PlatformRigidbody.linearVelocity : Vector3.zero;

    // 階層判定的快取：船不會換，父物件改變時才需要重查
    private Transform _hierarchyShip;
    private Transform _hierarchyParent;
    private bool _hierarchyOnShip;
    private Rigidbody _hierarchyRb;

    // 射線打到的 collider → Rigidbody 快取，避免每步都 GetComponentInParent
    private Collider _cachedPlatformCollider;
    private Rigidbody _cachedPlatformRb;

    // 外部（移動腳本）最後一次回報射線結果的 physics 時間；-1 = 從沒回報過
    private float _lastReportFixedTime = -1f;

    // ── 給移動腳本共用射線用 ─────────────────────────────────────────
    /// <summary>船的層。移動腳本的地面射線要把它加進遮罩。</summary>
    public int PlatformLayers => platformLayers.value;
    /// <summary>往下偵測的最大距離（公尺）。</summary>
    public float MaxDistance => maxDistance;
    /// <summary>射線起點往上抬的距離（公尺）。</summary>
    public float RayStartOffset => rayStartOffset;

    private void Reset()
    {
        // 新加元件時給預設值（LayerMask.GetMask 不能在欄位初始化時呼叫）
        groundLayers = LayerMask.GetMask("Ground", "Mobile Platform");
        platformLayers = LayerMask.GetMask("Mobile Platform");
    }

    private void Awake()
    {
        // 既有 prefab 上的舊元件沒有這兩個欄位，序列化後會是 Nothing → 補預設值
        if (groundLayers.value == 0) groundLayers = LayerMask.GetMask("Ground");
        if (platformLayers.value == 0) platformLayers = LayerMask.GetMask("Mobile Platform");
    }

    private void FixedUpdate()
    {
        if (detectByHierarchy && CheckHierarchy())
        {
            // 掛在船底下：不打射線，直接視為在船上
            isOnShip = true;
            PlatformRigidbody = _hierarchyRb;
            return;
        }

        // 移動腳本這一步（或上一步）已經回報過 → 不用再打。
        // 容許 1.5 步：腳本執行順序不固定，這個 FixedUpdate 可能比移動腳本早跑。
        if (_lastReportFixedTime >= 0f &&
            Time.fixedTime - _lastReportFixedTime <= Time.fixedDeltaTime * 1.5f)
            return;

        Vector3 origin = transform.position + Vector3.up * rayStartOffset;
        float distance = maxDistance + rayStartOffset;

        // 船的層一定要在射線遮罩裡：如果 groundLayers 只勾了 Ground，
        // 射線會直接穿過甲板，永遠判定「不在船上」。
        int mask = groundLayers.value | platformLayers.value;

        bool hit = Physics.Raycast(origin, Vector3.down, out RaycastHit info,
                                   distance, mask, QueryTriggerInteraction.Ignore);

        ApplyHit(hit, info, origin, distance);
    }

    /// <summary>
    /// 由移動腳本的 GroundCheck 呼叫：把「同一條」往下射線的結果交給這裡判斷在不在船上。
    /// 射線遮罩要包含 PlatformLayers，最遠距離建議用 MaxDistance。
    /// </summary>
    public void ReportGroundHit(bool hasHit, RaycastHit hit, Vector3 origin, float distance)
    {
        _lastReportFixedTime = Time.fixedTime;

        // 固定在船上的物件以階層判斷為準
        if (detectByHierarchy && CheckHierarchy()) return;

        ApplyHit(hasHit, hit, origin, distance);
    }

    private void ApplyHit(bool hit, RaycastHit info, Vector3 origin, float distance)
    {
        if (drawDebugRay)
        {
            Color c = !hit ? Color.gray : (IsPlatform(info.collider) ? Color.cyan : Color.yellow);
            Debug.DrawLine(origin, hit ? info.point : origin + Vector3.down * distance, c, Time.fixedDeltaTime);
        }

        if (hit && IsPlatform(info.collider))
        {
            Rigidbody rb = ResolvePlatformRigidbody(info.collider);
            isOnShip = true;
            PlatformRigidbody = rb;
        }
        else
        {
            isOnShip = false;
            PlatformRigidbody = null;
        }
    }

    private bool IsPlatform(Collider col)
    {
        if (col == null) return false;
        if ((platformLayers.value & (1 << col.gameObject.layer)) != 0) return true;

        // 船上加蓋的地板 / 物件可能在別的層，但只要掛在船的 Rigidbody 底下，也算船
        Rigidbody rb = col.attachedRigidbody;
        return rb != null && (platformLayers.value & (1 << rb.gameObject.layer)) != 0;
    }

    private Rigidbody ResolvePlatformRigidbody(Collider col)
    {
        if (col != _cachedPlatformCollider)
        {
            _cachedPlatformCollider = col;
            // attachedRigidbody：collider 掛在船的 Rigidbody 底下時直接拿到船本體
            _cachedPlatformRb = col.attachedRigidbody != null
                ? col.attachedRigidbody
                : col.GetComponentInParent<Rigidbody>();
        }
        return _cachedPlatformRb;
    }

    /// <summary>
    /// 這個物件是否掛在真實船底下。結果依「船 + 父物件」快取，
    /// 建築被放上 / 移出船時（parent 改變）才會重算。
    /// </summary>
    private bool CheckHierarchy()
    {
        LandshipNavigation nav = LandshipNavigation.Instance;
        Transform ship = (nav != null) ? nav.realShip : null;
        if (ship == null) return false;

        if (ship != _hierarchyShip || transform.parent != _hierarchyParent)
        {
            _hierarchyShip = ship;
            _hierarchyParent = transform.parent;
            _hierarchyOnShip = transform.IsChildOf(ship) && transform != ship;

            _hierarchyRb = null;
            if (_hierarchyOnShip)
            {
                // 船的 Rigidbody 可能在 root 上，也可能在 root 的父物件上
                _hierarchyRb = ship.GetComponent<Rigidbody>();
                if (_hierarchyRb == null) _hierarchyRb = ship.GetComponentInParent<Rigidbody>();
            }
        }

        return _hierarchyOnShip;
    }

    private void OnDisable()
    {
        _hierarchyShip = null;
        _hierarchyParent = null;
        _hierarchyOnShip = false;
        _hierarchyRb = null;

        // 被停用（或之後池化回收）時不要留著上一次的狀態
        isOnShip = false;
        PlatformRigidbody = null;
        _cachedPlatformCollider = null;
        _cachedPlatformRb = null;
        _lastReportFixedTime = -1f;
    }
}
