using UnityEngine;
using UnityEngine.AI;

public class EnemyBrain : MonoBehaviour
{
    [Header("Repath When Path Finished")]
    [SerializeField] private float repathWhenCloseToLastCorner = 1.0f;   // 距離小於這個值，視為到終點
    [SerializeField] private float repathCooldown = 0.25f;               // 避免每幀重算

    private CreatePath pathFinder;
    private EnemyMovement movement;

    private Vector3 nextMoveLocation;
    private float _nextAllowedRepathTime;

    private void Awake()
    {
        pathFinder = GetComponent<CreatePath>();
        movement = GetComponent<EnemyMovement>();
    }

    private void Update()
    {
        if (pathFinder == null || movement == null) return;

        // 1) 若已到路徑終點附近，就立即重算路徑
        TryRepathIfPathFinished();

        // 2) 正常沿路徑移動
        nextMoveLocation = pathFinder.FindNextMoveLocation(transform);

        Vector3 dir = nextMoveLocation - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
        {
            movement.HorizontalMovement(0f, 0f);
            return;
        }

        dir.Normalize();
        movement.SetWorldMoveDirection(dir);
    }

    /// <summary>
    /// 當「最近的 corner 是最後一個 corner」且距離小於門檻，就立即 FindPath() 產生新路徑
    /// </summary>
    private void TryRepathIfPathFinished()
    {
        if (Time.time < _nextAllowedRepathTime) return;

        NavMeshPath p = pathFinder.GetPath();
        if (p == null || p.corners == null) return;

        int len = p.corners.Length;

        // corners 太少通常代表路徑無效/剛好只有終點，直接嘗試重算一次
        if (len < 2)
        {
            pathFinder.FindPath();
            _nextAllowedRepathTime = Time.time + repathCooldown;
            return;
        }

        // 找最近 corner（包含最後一點）
        Vector3 pos = transform.position;
        pos.y = 0f;

        int closestIndex = -1;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < len; i++)
        {
            Vector3 c = p.corners[i];
            c.y = 0f;

            float sqr = (c - pos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closestIndex = i;
            }
        }

        // 最近 corner 是最後一個 + 距離小於門檻 -> 重算新路徑
        float thresholdSqr = repathWhenCloseToLastCorner * repathWhenCloseToLastCorner;
        if (closestIndex == len - 1 && bestSqr <= thresholdSqr)
        {
            pathFinder.FindPath();
            _nextAllowedRepathTime = Time.time + repathCooldown;
        }
    }
}
