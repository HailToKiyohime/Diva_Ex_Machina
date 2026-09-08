using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家蓋在船上的建築清單。
///
/// 存在的理由：敵人在通往 core 的路被封死時，要從「離 core 最近」開始逐一測試
/// 哪一棟建築走得到，把它當成暫時目標拆掉。那個搜尋需要一份可以列舉的建築清單，
/// 而專案裡原本沒有 —— BuildingGrid.occupied 只有 bool，沒有物件引用。
///
/// 刻意做成 static 而不是掛在 BuildSystem 上：敵人 AI 不應該引用建造系統。
/// BuildSystem 是玩家輸入端的東西，跟 AI 在職責上沒有關係，中間隔一層清單剛好。
/// </summary>
public static class BuildingRegistry
{
    /// <summary>
    /// 一棟建築的本尊 / 分身配對。
    ///
    /// ghost 也一起存的原因：實際 carve 掉 ghost navmesh 的是分身那一份，
    /// 本尊只是視覺。之後寫拆除功能時，只拆本尊、留著分身，navmesh 上的洞
    /// 會永遠留在那裡 —— 路看起來通了，敵人卻還是走不過去。
    /// 配對存著，拆除時兩邊一起處理就不會漏。
    /// </summary>
    public class Entry
    {
        /// <summary>船上的本尊。</summary>
        public Transform real;

        /// <summary>ghost ship 上的分身；BuildSystem.duplicateToGhost 關掉時為 null。</summary>
        public Transform ghost;
    }

    private static readonly List<Entry> _entries = new List<Entry>();

    /// <summary>
    /// 目前存活的建築。讀之前會先清掉已銷毀的項目。
    ///
    /// 為什麼需要 Prune：現在還沒有拆除功能，測試時是直接在 Hierarchy 手動刪，
    /// 那不會經過 Unregister。被 Destroy 的 Transform 在 Unity 裡會變成
    /// 「假 null」—— 引用還在、但 == null 成立 —— 不清掉的話後面每次搜尋都會
    /// 對這些殘骸做一次 CalculatePath。
    /// </summary>
    public static IReadOnlyList<Entry> Entries
    {
        get
        {
            Prune();
            return _entries;
        }
    }

    public static void Register(Transform real, Transform ghost)
    {
        if (real == null) return;

        Prune();

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].real == real)
            {
                _entries[i].ghost = ghost;   // 重複註冊就當成更新
                return;
            }
        }

        _entries.Add(new Entry { real = real, ghost = ghost });
    }

    public static void Unregister(Transform real)
    {
        if (real == null) return;

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].real == real)
                _entries.RemoveAt(i);
        }
    }

    /// <summary>清掉已被銷毀的項目。</summary>
    private static void Prune()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i] == null || _entries[i].real == null)
                _entries.RemoveAt(i);
        }
    }

    /// <summary>
    /// 每次進入 Play Mode 前清空。
    ///
    /// static 欄位不會隨著 Play Mode 結束而重置 —— 如果專案開了
    /// Enter Play Mode Options（關閉 Domain Reload），上一輪跑剩的建築引用
    /// 會整批留到下一輪，全部是假 null。在 Editor 裡症狀是「第一次跑正常、
    /// 第二次開始怪怪的」，很難查。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _entries.Clear();
    }
}