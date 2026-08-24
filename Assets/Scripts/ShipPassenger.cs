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
    [SerializeField] private string platformTag = "Mobile Platform";

    /// <summary>目前是否站在移動平台上。</summary>
    public bool isOnShip { get; private set; }

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

    private void OnDisable()
    {
        // 被停用（或之後池化回收）時不要留著上一次的狀態
        isOnShip = false;
        PlatformRigidbody = null;
        _cachedPlatformCollider = null;
        _cachedPlatformRb = null;
        _contactsThisStep = 0;
    }
}