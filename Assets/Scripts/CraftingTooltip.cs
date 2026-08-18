using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CraftingManager craftingManager;

    [Tooltip("若為 true：bulletPerShot / roundPerTap 為 1 時也會顯示 x 1；若為 false：1 會被省略")]
    public bool showX1Multipliers = false;

    [Header("Tooltip")]
    [Tooltip("Tooltip 面板（跟隨滑鼠顯示）。建議放在 Crafting UI Canvas 之下，並設為預設關閉。")]
    public RectTransform tooltipPanel;
    public TextMeshProUGUI tooltipTitle;
    public TextMeshProUGUI tooltipBody;
    [Tooltip("若留空，會自動從 tooltipPanel 往上找 Canvas。若 Canvas 是 Screen Space - Camera，請填入該 Canvas。")]
    public Canvas tooltipCanvas;
    [Tooltip("Tooltip 相對滑鼠的偏移（像素）。")]
    public Vector2 tooltipOffset = new Vector2(16f, -16f);
    [Tooltip("若為 true：顯示 (current -> new)；若為 false：只顯示 +/- 差異。 ")]
    public bool tooltipShowCurrentAndNew = true;
    [Tooltip("若為 true：差異為 0 的屬性也會顯示。通常建議關閉以保持乾淨。 ")]
    public bool tooltipShowZeroDiff = false;

    private ItemInstance _tooltipItem;
    private bool _tooltipVisible;

    /// <summary>
    /// Bind the tooltip system to a CraftingManager.
    /// </summary>
    public void Init(CraftingManager manager)
    {
        craftingManager = manager;
        HideTooltipImmediate();
    }

    private void Awake()
    {
        // Safe default: keep hidden on scene start
        HideTooltipImmediate();
    }

    private void Update()
    {
        if (_tooltipVisible)
            UpdateTooltipPosition(Input.mousePosition);
    }

    private string GetDisplayName(ItemInstance inv)
    {
        if (inv is RangeWeaponInstance rwi && !string.IsNullOrEmpty(rwi.newWeaponName))
            return rwi.newWeaponName;
        return inv.item.itemName;
    }

    // ===== Tooltip（Hover 顯示零件差異 / 武器本體 Buff） =====

    private void HideTooltipImmediate()
    {
        _tooltipItem = null;
        _tooltipVisible = false;
        if (tooltipPanel != null)
            tooltipPanel.gameObject.SetActive(false);
    }

    public void ShowTooltip(ItemInstance item)
    {
        if (tooltipPanel == null || tooltipTitle == null || tooltipBody == null)
            return;

        _tooltipItem = item;
        _tooltipVisible = item != null;

        tooltipPanel.gameObject.SetActive(_tooltipVisible);
        if (!_tooltipVisible)
            return;

        RefreshTooltipContent();
        UpdateTooltipPosition(Input.mousePosition);
    }

    public void HideTooltip()
    {
        HideTooltipImmediate();
    }

    public void RefreshTooltipIfVisible()
    {
        if (!_tooltipVisible || tooltipPanel == null || !tooltipPanel.gameObject.activeInHierarchy)
            return;
        RefreshTooltipContent();
    }

    private Canvas GetTooltipCanvas()
    {
        if (tooltipCanvas != null) return tooltipCanvas;
        if (tooltipPanel == null) return null;
        return tooltipPanel.GetComponentInParent<Canvas>();
    }

    private void UpdateTooltipPosition(Vector2 mouseScreenPos)
    {
        if (!_tooltipVisible || tooltipPanel == null) return;

        var canvas = GetTooltipCanvas();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Camera uiCam = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, mouseScreenPos, uiCam, out var localPos))
            return;

        // 固定往左下展開，理由同 TooltipManager
        tooltipPanel.pivot = new Vector2(1f, 1f);

        Vector2 finalOffset = new Vector2(
            -Mathf.Abs(tooltipOffset.x),
            -Mathf.Abs(tooltipOffset.y));

        tooltipPanel.anchoredPosition = localPos + finalOffset;

        // 靠近螢幕左緣 / 下緣時把面板推回畫面內
        ClampTooltipToCanvas(canvasRect);
    }

    private void ClampTooltipToCanvas(RectTransform canvasRect)
    {
        if (tooltipPanel == null) return;

        // 取得 Tooltip 在 Canvas Local 的四個角
        Vector3[] corners = new Vector3[4];
        tooltipPanel.GetWorldCorners(corners);

        // 把 world corners 轉回 canvas local
        for (int i = 0; i < 4; i++)
            corners[i] = canvasRect.InverseTransformPoint(corners[i]);

        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[2].y;

        Rect r = canvasRect.rect;
        Vector2 shift = Vector2.zero;
        if (maxX > r.xMax) shift.x -= (maxX - r.xMax);
        if (minX < r.xMin) shift.x += (r.xMin - minX);
        if (maxY > r.yMax) shift.y -= (maxY - r.yMax);
        if (minY < r.yMin) shift.y += (r.yMin - minY);

        if (shift != Vector2.zero)
            tooltipPanel.anchoredPosition += shift;
    }

    private void RefreshTooltipContent()
    {
        if (!_tooltipVisible || _tooltipItem == null || tooltipTitle == null || tooltipBody == null)
        {
            HideTooltipImmediate();
            return;
        }

        // Hover 武器：只顯示武器本體 buffs（不含零件總和、不含 craftingManager.craftingSlots 總和）
        if (_tooltipItem is RangeWeaponInstance rwi)
        {
            tooltipTitle.text = $"{GetDisplayName(rwi)}";
            tooltipBody.text = BuildOwnBuffText(rwi.buffs);
            return;
        }

        // Hover 零件：保留 (old -> new)，但顯示的是「零件自身修正值」而不是 delta。
        // 新值會依照更好/更差上綠/紅色。
        if (_tooltipItem is PartInstance pi)
        {
            WeaponPartType partType = GetPartTypeFromInstance(pi);
            tooltipTitle.text = $"{partType}: {GetDisplayName(pi)}";

            var current = FindCurrentPartInstance(partType);
            tooltipBody.text = BuildPartCompareText(current != null ? current.buffs : null, pi.buffs);
            return;
        }

        // 其他類型：保底顯示名稱
        tooltipTitle.text = GetDisplayName(_tooltipItem);
        tooltipBody.text = "";
    }

    private WeaponPartType GetPartTypeFromInstance(PartInstance pi)
    {
        if (pi != null && pi.item is RangeWeaponPart rwp)
            return rwp.partType;

        // 保底：用 slot.equipmentType 推測（找出目前這個 PartInstance 被裝在哪個 slot）
        if (craftingManager.craftingSlots != null)
        {
            foreach (var s in craftingManager.craftingSlots)
                if (s != null && s.item == pi)
                    return s.equipmentType;
        }
        return WeaponPartType.Gun;
    }

    private PartInstance FindCurrentPartInstance(WeaponPartType partType)
    {
        if (craftingManager.craftingSlots == null) return null;
        foreach (var s in craftingManager.craftingSlots)
        {
            if (s == null) continue;
            if (s.item is not PartInstance part) continue;
            if (s.equipmentType != partType) continue;
            if (part.item == null) continue;
            return part;
        }
        return null;
    }

    private struct BuffAgg
    {
        public float add;      // 加總後的加法值
        public float mul;      // 乘法因子（把所有 (1+value) 乘起來）
        public bool hasAdd;
        public bool hasMul;
    }

    /// <summary>
    /// 把 buffs 彙整成「加法總和」與「乘法因子」。
    /// - Add: add += value
    /// - Multiplier: mul *= (1 + value)
    /// </summary>
    private Dictionary<Attributes, BuffAgg> AggregateBuffs(List<EquipmentBuff> buffs)
    {
        var dict = new Dictionary<Attributes, BuffAgg>();
        if (buffs == null) return dict;

        foreach (var b in buffs)
        {
            if (!dict.TryGetValue(b.attribute, out var agg))
                agg = new BuffAgg { add = 0f, mul = 1f, hasAdd = false, hasMul = false };

            // FiringMode 用名稱顯示；這個屬性我們只吃 Add（或直接用 value）比較合理
            if (b.attribute == Attributes.FiringMode)
            {
                agg.add += b.value;
                agg.hasAdd = true;
                dict[b.attribute] = agg;
                continue;
            }

            if (b.mode == BuffApplyMode.Multiplier)
            {
                agg.mul *= (1f + b.value);
                agg.hasMul = true;
            }
            else // BuffApplyMode.Add（或未知就當 Add）
            {
                agg.add += b.value;
                agg.hasAdd = true;
            }

            dict[b.attribute] = agg;
        }

        return dict;
    }

    /// <summary>
    /// Hover 武器用：只顯示自身 buffs（不做 old/new 比較）。
    /// - Add: 顯示數字（不強制加 + 號，避免把「絕對值」看成 delta）
    /// - Multiplier: 顯示 x1.3（把所有 (1+value) 連乘）
    /// </summary>
    private string BuildOwnBuffText(List<EquipmentBuff> buffs)
    {
        var agg = AggregateBuffs(buffs);
        if (agg.Count <= 0) return "No buffs.";

        var keys = new List<Attributes>(agg.Keys);
        keys.Sort((a, b) => ((int)a).CompareTo((int)b));

        var sb = new StringBuilder();
        int shown = 0;

        foreach (var attr in keys)
        {
            var v = agg[attr];

            if (attr == Attributes.FiringMode)
            {
                int mode = Mathf.RoundToInt(v.add);
                sb.AppendLine($"Firing Mode: {GetFiringModeName(mode)}");
                shown++;
                continue;
            }

            var parts = new List<string>(2);
            if (v.hasAdd)
                parts.Add(FormatAddValue(attr, v.add));
            if (v.hasMul)
                parts.Add(FormatMulValue(v.mul));

            if (parts.Count == 0) continue;

            sb.AppendLine($"{PrettyAttrName(attr)}: {string.Join(" ", parts)}");
            shown++;
        }

        return shown > 0 ? sb.ToString().TrimEnd() : "No buffs.";
    }

    /// <summary>
    /// Hover 零件用：保留 (old -> new)，但顯示的是「零件自身修正值」；新值依照更好/更差上色。
    /// 例：Spread: &lt;color=green&gt;x1.3&lt;/color&gt; (0 -&gt; x1.3)
    /// </summary>
    private string BuildPartCompareText(List<EquipmentBuff> currentBuffs, List<EquipmentBuff> hoverBuffs)
    {
        var oldAgg = AggregateBuffs(currentBuffs);
        var newAgg = AggregateBuffs(hoverBuffs);

        // union of attributes
        var keySet = new HashSet<Attributes>();
        foreach (var k in oldAgg.Keys) keySet.Add(k);
        foreach (var k in newAgg.Keys) keySet.Add(k);

        if (keySet.Count == 0) return "No buffs.";

        var keys = new List<Attributes>(keySet);
        keys.Sort((a, b) => ((int)a).CompareTo((int)b));

        var sb = new StringBuilder();
        int shown = 0;

        foreach (var attr in keys)
        {
            oldAgg.TryGetValue(attr, out var o);
            newAgg.TryGetValue(attr, out var n);

            // FiringMode：用名稱顯示，不做好壞判斷（避免誤導）
            if (attr == Attributes.FiringMode)
            {
                string oldName = (o.hasAdd) ? GetFiringModeName(Mathf.RoundToInt(o.add)) : "0";
                string newName = (n.hasAdd) ? GetFiringModeName(Mathf.RoundToInt(n.add)) : "0";
                sb.AppendLine($"Firing Mode: {newName} ({oldName} -> {newName})");
                shown++;
                continue;
            }

            bool hasAdd = o.hasAdd || n.hasAdd;
            bool hasMul = o.hasMul || n.hasMul;
            bool both = hasAdd && hasMul;

            if (hasAdd)
            {
                float oldVal = o.hasAdd ? o.add : 0f;
                float newVal = n.hasAdd ? n.add : 0f;

                string oldDisp = FormatAddOrZero(attr, oldVal);
                string newDisp = FormatAddOrZero(attr, newVal);

                string label = both ? $"{PrettyAttrName(attr)} (Add)" : PrettyAttrName(attr);
                string coloredNew = ColorizeNewValue(attr, oldVal, newVal, newDisp, isMultiplier: false);

                if (tooltipShowZeroDiff || !Approximately(oldVal, newVal))
                {
                    sb.AppendLine($"{label}: {coloredNew} ({oldDisp} -> {newDisp})");
                    shown++;
                }
            }

            if (hasMul)
            {
                float oldFactor = o.hasMul ? o.mul : 1f;
                float newFactor = n.hasMul ? n.mul : 1f;

                string oldDisp = FormatMulOrZero(attr, oldFactor);
                string newDisp = FormatMulOrZero(attr, newFactor);

                string label = both ? $"{PrettyAttrName(attr)} (Mul)" : PrettyAttrName(attr);
                string coloredNew = ColorizeNewValue(attr, oldFactor, newFactor, newDisp, isMultiplier: true);

                if (tooltipShowZeroDiff || !Approximately(oldFactor, newFactor))
                {
                    sb.AppendLine($"{label}: {coloredNew} ({oldDisp} -> {newDisp})");
                    shown++;
                }
            }
        }

        return shown > 0 ? sb.ToString().TrimEnd() : "No buffs.";
    }

    private static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.0001f;
    }

    /// <summary>
    /// 哪些屬性是「越小越好」。用於 Tooltip 上色。
    /// </summary>
    private static bool IsSmallerBetter(Attributes attr)
    {
        return attr switch
        {
            Attributes.Spread => true,
            Attributes.ReloadTime => true,
            Attributes.TimeBetweenShooting => true,
            Attributes.TimeBetweenShots => true,
            Attributes.DashEnergyCost => true,
            Attributes.FlyEnergyCost => true,
            _ => false
        };
    }

    private string ColorizeNewValue(Attributes attr, float oldVal, float newVal, string newDisp, bool isMultiplier)
    {
        // Decide better/worse by attribute direction.
        // For multiplier, we compare factors (1.0 baseline). For add, we compare additive values.
        bool smallerBetter = IsSmallerBetter(attr);

        bool better;
        if (smallerBetter)
            better = newVal < oldVal - 0.0001f;
        else
            better = newVal > oldVal + 0.0001f;

        bool worse;
        if (smallerBetter)
            worse = newVal > oldVal + 0.0001f;
        else
            worse = newVal < oldVal - 0.0001f;

        if (better)
            return $"<color=green>{newDisp}</color>";
        if (worse)
            return $"<color=red>{newDisp}</color>";

        return newDisp;
    }

    private string FormatAddOrZero(Attributes attr, float v)
    {
        // For count-like stats, a "0" modifier means baseline is effectively x1.
        // Showing 0 here is confusing (e.g. RoundPerPull: (0 -> x6) implies "shoot 0 rounds").
        if (Mathf.Abs(v) < 0.0001f)
        {
            if (attr == Attributes.RoundPerPull || attr == Attributes.BulletPerShot)
                return "x1";
            return "0";
        }

        return FormatAddValue(attr, v);
    }

    private static string FormatMulOrZero(Attributes attr, float factor)
    {
        // Same baseline rule as above: for count-like stats, baseline should display as x1.
        if (Mathf.Abs(factor - 1f) < 0.0001f)
        {
            if (attr == Attributes.RoundPerPull || attr == Attributes.BulletPerShot)
                return "x1";
            return "0";
        }

        return $"x{FormatNumberStatic(factor)}";
    }

    private static string FormatMulValue(float factor)
    {
        return $"x{FormatNumberStatic(factor)}";
    }

    private string FormatAddValue(Attributes attr, float v)
    {
        // Add values shown as raw number with units where helpful.
        switch (attr)
        {
            case Attributes.CriticalChance:
                return FormatPercent01(v);
            case Attributes.CriticalMultiplier:
                return FormatNumber(v);
            case Attributes.ReloadTime:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
                return FormatSeconds(v);
            case Attributes.Spread:
                return $"{v:0.##}°";
            case Attributes.MagazineSize:
                return Mathf.RoundToInt(v).ToString();
            case Attributes.BulletPerShot:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            case Attributes.RoundPerPull:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            case Attributes.FiringMode:
                return Mathf.RoundToInt(v).ToString();
            default:
                return FormatNumber(v);
        }
    }

    private static string FormatNumberStatic(float v)
    {
        float r = Mathf.Round(v);
        if (Mathf.Abs(v - r) < 0.0001f)
            return ((int)r).ToString();
        return v.ToString("0.##");
    }

    private string PrettyAttrName(Attributes attr)
    {
        // 把 enum 名稱轉成較好讀的形式：PhysicalDamage -> Physical Damage
        string s = attr.ToString();
        return System.Text.RegularExpressions.Regex.Replace(s, "([a-z])([A-Z])", "$1 $2");
    }

    private string FormatAttrValue(Attributes attr, float v)
    {
        switch (attr)
        {
            case Attributes.CriticalChance:
                return FormatPercent01(v);
            case Attributes.CriticalMultiplier:
                return "x" + FormatNumber(v);
            case Attributes.ReloadTime:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
                return FormatSeconds(v);
            case Attributes.Spread:
                return $"{v:0.##}°";
            case Attributes.MagazineSize:
                return Mathf.RoundToInt(v).ToString();
            case Attributes.BulletPerShot:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            case Attributes.RoundPerPull:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            default:
                return FormatNumber(v);
        }
    }

    private string FormatDelta(Attributes attr, float delta)
    {
        // delta 一律帶 +/-
        string sign = delta >= 0f ? "+" : "";
        switch (attr)
        {
            case Attributes.CriticalChance:
                return sign + FormatPercent01(delta);
            case Attributes.CriticalMultiplier:
                return sign + FormatNumber(delta);
            case Attributes.ReloadTime:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
                return sign + FormatSeconds(delta);
            case Attributes.Spread:
                return sign + $"{delta:0.##}°";
            case Attributes.MagazineSize:
            case Attributes.BulletPerShot:
            case Attributes.RoundPerPull:
                return sign + Mathf.RoundToInt(delta).ToString();
            default:
                return sign + FormatNumber(delta);
        }
    }
    // ---- Formatting / naming helpers (moved from CraftingManager) ----
    private static string GetFiringModeName(int mode)
    {
        return mode switch
        {
            0 => "Single",
            1 => "Auto",
            2 => "Charge",
            _ => mode.ToString()
        };
    }

    private static string FormatSeconds(float v)
    {
        if (v < 0f) v = 0f;
        return $"{v:0.##}s";
    }

    private static string FormatPercent01(float v)
    {
        // 系統用 0~1（例如 0.05 = 5%）
        float pct = v * 100f;
        // 這裡不強行 clamp 上限，避免主人做 >100% 的設計時顯示被截斷
        return $"{pct:0.##}%";
    }

    private static string FormatNumber(float v)
    {
        // 盡量輸出乾淨：接近整數就不顯示小數
        float r = Mathf.Round(v);
        if (Mathf.Abs(v - r) < 0.0001f)
            return ((int)r).ToString();
        return v.ToString("0.##");
    }
}