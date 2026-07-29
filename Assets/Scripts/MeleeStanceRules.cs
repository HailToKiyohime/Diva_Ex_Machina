using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器類型。由 blade 屬性 × handle 屬性推導而來，鍛造時決定。
/// </summary>
public enum MeleeWeaponClass
{
    Sword = 0, 
    LongSword = 1,
    GreatSword = 2,
    Polearm = 3,
    Glaive = 4,
    Hammer = 5,
    GreatHammer = 6,
    Gunblade = 7,//code later
    Dagger = 8,
    Lance = 9,
}

/// <summary>
/// 握持方式。Runtime 決定：另一隻手空著就是雙手持。
/// </summary>
public enum MeleeGrip
{
    OneHanded = 0,
    TwoHanded = 1,
}

/// <summary>
/// 一條組合規則：某種刀刃 + 某種柄 = 某個武器類型。
/// displayName / icon 給鍛造預覽 UI 用。
/// </summary>
[System.Serializable]
public class MeleeStanceRule
{
    [Tooltip("刀刃屬性，來自 MeleeWeapon.attribute")]
    public MeleeWeaponPartAttribute blade;

    [Tooltip("柄屬性，來自 Handle 零件的 attribute；沒裝零件時用 MeleeWeapon.defaultHandleAttribute")]
    public MeleeWeaponPartAttribute handle;

    [Tooltip("這個組合產出的武器類型，決定用哪一套連段")]
    public MeleeWeaponClass resultClass;

    [Tooltip("鍛造預覽顯示用，例如「長矛」")]
    public string displayName;

    [Tooltip("鍛造預覽顯示用（可留空）")]
    public Sprite icon;
}

/// <summary>
/// 近戰武器組合規則表。
///
/// 這張表刻意用「明列」而不是演算法推導 —— 「匕首 + 長柄 = 長矛」這種有趣的組合
/// 應該是被設計出來的，而不是規則意外產生的。
///
/// 鍛造 UI 與戰鬥系統共用同一份資產，確保預覽跟實際結果永遠一致。
/// </summary>
[CreateAssetMenu(fileName = "MeleeStanceRules", menuName = "Inventory/Melee Stance Rules")]
public class MeleeStanceRules : ScriptableObject
{
    [Tooltip("每個 (blade, handle) 組合一筆。重複的組合以最後一筆為準（會在 Console 警告）")]
    public List<MeleeStanceRule> rules = new List<MeleeStanceRule>();

    [Header("Fallback")]
    [Tooltip("找不到對應規則時使用的類型")]
    public MeleeWeaponClass fallbackClass = MeleeWeaponClass.Sword;

    [Tooltip("找不到規則時是否在 Console 警告（建議開發期開啟）")]
    public bool warnOnMissingRule = true;

    private Dictionary<(MeleeWeaponPartAttribute, MeleeWeaponPartAttribute), MeleeStanceRule> _lookup;

    private void OnEnable() => _lookup = null;

    private void BuildLookup()
    {
        _lookup = new Dictionary<(MeleeWeaponPartAttribute, MeleeWeaponPartAttribute), MeleeStanceRule>();
        if (rules == null) return;

        foreach (var rule in rules)
        {
            if (rule == null) continue;
            _lookup[(rule.blade, rule.handle)] = rule;
        }
    }

    // ────────────────────────────────────────────────
    //  低階查詢：給鍛造 UI 用（零件還沒組成 Instance 之前）
    // ────────────────────────────────────────────────

    public bool TryGetRule(MeleeWeaponPartAttribute blade, MeleeWeaponPartAttribute handle, out MeleeStanceRule rule)
    {
        if (_lookup == null) BuildLookup();
        return _lookup.TryGetValue((blade, handle), out rule);
    }

    /// <summary>組合出的武器類型；沒有對應規則則回傳 fallbackClass。</summary>
    public MeleeWeaponClass GetClass(MeleeWeaponPartAttribute blade, MeleeWeaponPartAttribute handle)
    {
        if (TryGetRule(blade, handle, out var rule))
            return rule.resultClass;

        if (warnOnMissingRule)
            Debug.LogWarning($"[MeleeStanceRules] 沒有 ({blade} + {handle}) 的規則，退回 {fallbackClass}。", this);

        return fallbackClass;
    }

    /// <summary>鍛造預覽用的名稱；沒有規則或沒填則回傳 null。</summary>
    public string GetDisplayName(MeleeWeaponPartAttribute blade, MeleeWeaponPartAttribute handle)
    {
        return TryGetRule(blade, handle, out var rule) && !string.IsNullOrWhiteSpace(rule.displayName)
            ? rule.displayName
            : null;
    }

    /// <summary>鍛造預覽用的圖示；沒有規則則回傳 null。</summary>
    public Sprite GetIcon(MeleeWeaponPartAttribute blade, MeleeWeaponPartAttribute handle)
    {
        return TryGetRule(blade, handle, out var rule) ? rule.icon : null;
    }

    // ────────────────────────────────────────────────
    //  高階查詢：給戰鬥系統用
    // ────────────────────────────────────────────────

    /// <summary>
    /// 從已組裝的 MeleeWeaponInstance 解析出武器類型。
    /// 刻意「每次現算」而不是存進 Instance —— 存檔裡的舊武器不會因為規則表改動而停在舊值。
    /// </summary>
    public MeleeWeaponClass ResolveClass(MeleeWeaponInstance mwi)
    {
        if (mwi == null || mwi.item is not MeleeWeapon mw)
            return fallbackClass;

        return GetClass(mw.attribute, MeleeStanceResolver.ResolveHandleAttribute(mwi));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _lookup = null;   // Inspector 改動後強制重建

        if (rules == null) return;

        var seen = new HashSet<(MeleeWeaponPartAttribute, MeleeWeaponPartAttribute)>();
        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null) continue;

            if (!seen.Add((rule.blade, rule.handle)))
                Debug.LogWarning($"[MeleeStanceRules] 第 {i} 筆：({rule.blade} + {rule.handle}) 重複定義，只有最後一筆會生效。", this);

            if (MeleeStanceResolver.IsHandleAttribute(rule.blade))
                Debug.LogWarning($"[MeleeStanceRules] 第 {i} 筆：blade 欄位填了柄屬性 '{rule.blade}'，可能填反了。", this);

            if (!MeleeStanceResolver.IsHandleAttribute(rule.handle))
                Debug.LogWarning($"[MeleeStanceRules] 第 {i} 筆：handle 欄位填了刀刃屬性 '{rule.handle}'，可能填反了。", this);
        }
    }
#endif
}

/// <summary>
/// 無狀態的解析工具。跟規則表分開，是因為「柄從哪來」和「握持怎麼算」
/// 屬於資料結構的走訪邏輯，跟規則表的內容無關。
/// </summary>
public static class MeleeStanceResolver
{
    /// <summary>這個屬性是柄類還是刀刃類。新增柄類型時記得加進來。</summary>
    public static bool IsHandleAttribute(MeleeWeaponPartAttribute attribute)
    {
        return attribute == MeleeWeaponPartAttribute.LongHandle
            || attribute == MeleeWeaponPartAttribute.ShortHandle;
    }

    /// <summary>
    /// 取得這把武器實際使用的柄屬性。
    /// 有裝 Handle 零件就用零件的，沒有就用 base SO 的 defaultHandleAttribute。
    /// </summary>
    public static MeleeWeaponPartAttribute ResolveHandleAttribute(MeleeWeaponInstance mwi)
    {
        var mw = mwi?.item as MeleeWeapon;

        if (mwi?.attachment != null)
        {
            foreach (var part in mwi.attachment)
            {
                if (part == null || part.partType != WeaponPartType.Handle) continue;
                if (part.item is MeleeWeaponPart mwp) return mwp.attribute;
            }
        }
        return (mw != null) ? mw.defaultHandleAttribute : MeleeWeaponPartAttribute.ShortHandle;
    }

    /// <summary>
    /// 這隻手的握持方式：另一隻手空著就是雙手持。
    ///
    /// 注意 PlayerStats.OnHandWeaponDataChanged 在任何一隻手變動時都會觸發，
    /// AttackManager.SyncFromPlayerStats 會同時重算左右手，所以卸下副手時
    /// 主手會自動從單手切成雙手，不需要額外的通知路徑。
    /// </summary>
    public static MeleeGrip ResolveGrip(bool isLeftHand)
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return MeleeGrip.OneHanded;

        var otherHand = isLeftHand ? ps.rightHand : ps.leftHand;

        bool otherHandEmpty = otherHand == null
                           || otherHand.weaponKind == HandWeaponKind.None
                           || !otherHand.HasWeapon;

        return otherHandEmpty ? MeleeGrip.TwoHanded : MeleeGrip.OneHanded;
    }
}