using UnityEngine;

public class DockingPointTrigger : MonoBehaviour
{
    [SerializeField] private LandshipNavigation landship;
    [Tooltip("可選：只允許這個 Tag 的物件觸發（留空=不檢查）")]
    [SerializeField] private string requiredTag = "";

    private void Reset()
    {
        // 自動嘗試找場景裡的 LandshipNavigation
        if (landship == null) landship = FindObjectOfType<LandshipNavigation>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (landship == null) return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        // 你的敵人 collider 可能在子物件，所以用 InParent
        CreatePath path = other.GetComponentInParent<CreatePath>();
        if (path == null) return;

        if (landship.core == null || landship.ghostShip == null) return;

        // ✅ 切到船上模式：目標改 core，使用 ghost navmesh 計算路徑
        path.SetOnShipNav(landship.transform, landship.ghostShip, landship.core);
    }
}
