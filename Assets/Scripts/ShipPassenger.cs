using UnityEngine;

/// <summary>
/// 實體的「在不在移動平台上」狀態，以及那個平台的 Rigidbody。
///
/// 專案裡「在不在船上」現在只有這一個真相來源：
///   PathFinder（走 ghost 空間還是地面空間）   ← ModularEntityBrain.SyncShipFlags
///   ModularEntityMovement（要不要疊平台速度）  ← GetMobilePlatformVelocity
///
/// ⚠ 這個元件必須跟實體的 collider 掛在同一個 GameObject 上，否則收不到 trigger 訊息。
/// </summary>  
public class ShipPassenger : MonoBehaviour
{
    [SerializeField] private string platformTag = "Mobile Platform Hitbox";

    [Tooltip("固定在船上的物件（建築、砲塔）用。\n" +
             "開啟後，只要這個物件是真實船（LandshipNavigation.realShip）的子物件，就視為在船上，\n" +
             "不需要 Rigidbody、也不需要 trigger。會走動的實體（敵人、玩家）請保持關閉。")]
    [SerializeField] private bool detectByHierarchy = false;

    /// <summary>目前是否站在移動平台上。</summary>
    public bool isOnShip { get; private set; }

    /// <summary>isOnShip 的別名（CreatePath 用的是這個大小寫）。</summary>
    public bool IsOnShip => isOnShip;

    // 階層判定的快取：船不會換，父物件改變時才需要重查
    private Transform _hierarchyShip;
    private Transform _hierarchyParent;
    private bool _hierarchyOnShip;
    private Rigidbody _hierarchyRb;

    /// <summary>目前所在平台的 Rigidbody；不在平台上時為 null。</summary>
    public Rigidbody PlatformRigidbody { get; private set; }

    /// <summary>平台速度，不在平台上時為 Vector3.zero。</summary>
    public Vector3 PlatformVelocity =>
        (isOnShip && PlatformRigidbody != null) ? PlatformRigidbody.linearVelocity : Vector3.zero;

    // ── 每個 physics step 重新計數 ───────────────────────────────────────
    //
    // 舊版是 OnTriggerStay 設 true、OnTriggerExit 設 false。那有一個 bug：
    // 船身如果由多個 "Mobile Platform" collider 組成，離開其中一個就會把狀態
    // 歸零，即使實體仍然站在另一個上面。
    //
    // 計數器（Enter++ / Exit--）能解決多 collider，但 Unity 在 collider 被
    // 銷毀時不保證發出 OnTriggerExit，漏掉一次就永久卡住 —— 那正是這次要修的
    // 那類 bug。
    //
    // 改成「每個 physics step 從零重數」：OnTriggerStay 每步都會對每個重疊的
    // collider 各觸發一次，所以只要重數就好，不需要任何 Exit 事件。
    // 漏事件、多 collider、collider 被銷毀，這三種情況全部自動正確。
    //
    // 時序：Unity 的物理步驟是 FixedUpdate → 模擬 → OnTrigger 回呼。
    // 所以這裡 FixedUpdate 公布的是「上一步」數到的結果，有一個 step 的延遲
    // （0.02 秒），對載具搭乘來說無感。
    private int _contactsThisStep;

    // 快取，避免每個 physics step 都做 GetComponentInParent
    private Collider _cachedPlatformCollider;
    private Rigidbody _cachedPlatformRb;

    private void FixedUpdate()
    {
        if (detectByHierarchy && CheckHierarchy())
        {
            // 掛在船底下：不看 trigger，直接視為在船上
            isOnShip = true;
            PlatformRigidbody = _hierarchyRb;
            _contactsThisStep = 0;
            return;
        }

        isOnShip = _contactsThisStep > 0;

        if (!isOnShip)
        {
            PlatformRigidbody = null;
            _cachedPlatformCollider = null;
            _cachedPlatformRb = null;
        }

        _contactsThisStep = 0;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag(platformTag)) return;
        _contactsThisStep++;

        if (other != _cachedPlatformCollider)
        {
            _cachedPlatformCollider = other;
            _cachedPlatformRb = other.GetComponentInParent<Rigidbody>();
        }

        if (_cachedPlatformRb != null)
            PlatformRigidbody = _cachedPlatformRb;
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
        _contactsThisStep = 0;
    }
}