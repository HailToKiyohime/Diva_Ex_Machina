using UnityEngine;

/// <summary>
/// PrefabPool 幫每個池化實例掛上的標記。
///
/// 由池子在建立實例時自動加，不需要手動放到 prefab 上（放了也沒關係，池子會沿用既有的）。
/// 它的存在讓 Despawn(gameObject) 可以 O(1) 找回正確的池子，
/// 不用反查「這個物件是哪個 prefab 生的」。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]   // 不出現在 Add Component 選單，避免手動誤加
public class PooledInstance : MonoBehaviour
{
    [Tooltip("這個實例是從哪個 prefab 生出來的。由 PrefabPool 寫入，不要手動改。")]
    public GameObject sourcePrefab;

    [Tooltip("目前是否躺在池子裡（未使用中）。用來擋掉同一幀被 Despawn 兩次 —— " +
             "例如一顆子彈同時撞到兩個 collider。")]
    public bool isIdle;

    /// <summary>
    /// 這個實例（含所有子物件）身上的 IPooled 快取。
    ///
    /// 只在「真的建立實例」時掃一次。如果每次 Spawn 都呼叫 GetComponentsInChildren，
    /// 每次都會配置一個新陣列 —— 那等於把池化省下來的 GC 又還回去了。
    /// </summary>
    [System.NonSerialized] public IPooled[] hooks;
}