    using UnityEngine;

    public class DockingPointTrigger : MonoBehaviour
    {   
        [SerializeField] private LandshipNavigation landship;

        private void Reset()
        {
            // 自動嘗試找場景裡的 LandshipNavigation
            if (landship == null) landship = FindObjectOfType<LandshipNavigation>();
        }

        private void OnTriggerEnter(Collider other)
        {
            CreatePath path = other.GetComponentInParent<CreatePath>();
            if (path == null) return;

            EnemyBrain brain = other.GetComponentInParent<EnemyBrain>();
            if (brain == null) return;

            // 用 Brain 的當前目標（可能是 Core、也可能是船上的玩家）
            // fallback 到 core 只在目標 null 時才用
            Transform combatTarget = brain.currentTargetTransform;
            if (combatTarget == null || !combatTarget.IsChildOf(landship.transform))
                combatTarget = landship.core;

            // 登船：_navTarget 切換成真正的戰鬥目標
            path.SetOnShipNav(landship.transform, landship.ghostShip, combatTarget);
        }
}
