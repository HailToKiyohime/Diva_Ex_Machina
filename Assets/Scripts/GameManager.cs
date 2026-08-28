using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("場上的敵人。執行期由 AddEnemy / RemoveEnemy 維護，載入場景時會補掃一次 tag。")]
    public List<GameObject> enemies = new List<GameObject>();

    [Header("Attack Limiter")]
    [Tooltip("Max number of enemies allowed to ATTACK (shoot / deal ranged damage) at the same time")]
    public int maxSimultaneousAttackers = 3;

    [Header("Scan")]
    [Tooltip("補掃場景敵人時使用的 tag")]
    [SerializeField] private string enemyTag = "Enemy";

    // 改存 EnemyBrain 參考，而不是 GetInstanceID()。
    //
    // 存 ID 的問題：持有名額的敵人被銷毀時如果沒呼叫 ReleaseAttackSlot（死亡、
    // 場景卸載、之後接物件池被 SetActive(false)），那個 ID 會永遠卡在集合裡。
    // 三個名額被三隻死掉的敵人佔滿之後，全場敵人就再也不會開火 ——
    // 而且完全沒有錯誤訊息，只會表現成「打到後面敵人變得很被動」。
    //
    // 存參考就能在名額用滿時回頭檢查持有者還在不在，自動回收。
    private readonly HashSet<EnemyBrain> _attackers = new HashSet<EnemyBrain>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // Destroy duplicate instances
            return;                // ★ 原本少了這行：重複的實例會繼續跑下面整段初始化
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _attackers.Clear();
        RefreshEnemyList();

        // DontDestroyOnLoad → 換場景時 Awake 不會再跑，只能靠這個事件補。
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 被銷毀的重複實例不該動到真正的單例
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;   // 否則 Instance 會指向一個已銷毀的物件
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重載關卡時如果不做這件事：
        //   1. enemies 停留在第一次進場景的狀態，新場景的敵人一個都不在裡面
        //   2. 清單裡塞滿已銷毀物件的空殼，所有走訪它的程式碼都要自己防 null
        //   3. 上一局持有攻擊名額的敵人已經不存在，名額卻還被佔著

        if (mode == LoadSceneMode.Single)
            _attackers.Clear();   // 疊加載入（UI 場景之類）不該影響進行中的戰鬥

        RefreshEnemyList();
    }

    // ============================
    // Enemy registry
    // ============================

    public void AddEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        if (enemies.Contains(enemy)) return;   // 重複註冊：例如敵人同時在 Awake 和 OnEnable 呼叫
        enemies.Add(enemy);
    }

    public void RemoveEnemy(GameObject enemy)
    {
        // 刻意不擋 null —— 傳進來的物件可能正在銷毀流程中（OnDestroy 裡呼叫），
        // 這時候擋掉反而會讓它留在清單裡
        enemies.Remove(enemy);
    }

    /// <summary>取得敵人清單。回傳前會先清掉已銷毀的項目。</summary>
    public List<GameObject> GetEnemies()
    {
        PruneEnemies();
        return enemies;
    }

    /// <summary>
    /// 清掉已銷毀的項目，再補掃場景上帶 tag 的敵人。
    /// 用「合併」而不是「重建」——
    /// 腳本執行順序不保證，敵人可能已經在自己的 Awake 裡先呼叫過 AddEnemy，
    /// 原本的 enemies = new List<>(FindGameObjectsWithTag(...)) 會把那些註冊蓋掉。
    /// </summary>
    public void RefreshEnemyList()
    {
        PruneEnemies();

        if (string.IsNullOrEmpty(enemyTag)) return;

        GameObject[] found = GameObject.FindGameObjectsWithTag(enemyTag);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && !enemies.Contains(found[i]))
                enemies.Add(found[i]);
        }
    }

    private void PruneEnemies()
    {
        // 倒著走，RemoveAt 才不會跳過元素
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null) enemies.RemoveAt(i);
        }
    }

    // ============================
    // Attack slot API (called by EnemyBrain)
    // ============================

    public bool TryClaimAttackSlot(EnemyBrain brain)
    {
        if (brain == null) return false;

        if (_attackers.Contains(brain)) return true;   // already has a slot

        int cap = Mathf.Max(0, maxSimultaneousAttackers);

        // 只在看起來額滿時才清理 —— 集合最多就 maxSimultaneousAttackers 個，很便宜
        if (_attackers.Count >= cap)
        {
            PruneAttackers();
            if (_attackers.Count >= cap) return false;
        }

        _attackers.Add(brain);
        return true;
    }

    public void ReleaseAttackSlot(EnemyBrain brain)
    {
        if (brain == null)
        {
            // 連是誰都不知道（呼叫端的參考已經失效）→ 整批清一次，至少不會卡住名額
            PruneAttackers();
            return;
        }

        _attackers.Remove(brain);
    }

    /// <summary>目前正在攻擊的敵人數量。</summary>
    public int CurrentAttackersCount
    {
        get
        {
            PruneAttackers();
            return _attackers.Count;
        }
    }

    /// <summary>持有者已被銷毀或停用 → 收回名額。停用中的敵人定義上不可能在攻擊。</summary>
    private int _pruneFrame = -1;

    private void PruneAttackers()
    {
        // 同一幀只清一次。
        //
        // 名額滿了之後（cap 預設 3），每個搶不到名額的敵人每次呼叫
        // TryClaimAttackSlot 都會觸發一次完整的 RemoveWhere，而
        // isActiveAndEnabled 是 native 呼叫。100 隻敵人輪詢就是每幀
        // 300 次 native call，第一次之後全部是白做的。
        if (_pruneFrame == Time.frameCount) return;
        _pruneFrame = Time.frameCount;

        _attackers.RemoveWhere(b => b == null || !b.isActiveAndEnabled);
    }
}