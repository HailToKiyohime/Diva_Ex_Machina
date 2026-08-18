using System.Text;
using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    public GameObject tooltipRoot;
    public TextMeshProUGUI equipmentText;
    public TextMeshProUGUI statText;
    public RectTransform tooltipPanel;
    public Vector2 offset = new Vector2(20f, -20f);

    private void Awake()
    {
        Instance = this;
        HideTooltip();
    }

    private void Update()
    {
        if (tooltipRoot != null && tooltipRoot.activeSelf)
            UpdateTooltipPosition();
    }

    // 依游標在螢幕上的位置自動選邊，讓面板永遠往有空間的方向展開。
    //
    // 作法是切換 pivot 而不是計算位移：pivot 決定「position 對應到面板的哪個角」，
    // 所以 pivot.x = 1 時面板整個往左長，不需要知道面板寬度。
    // 面板尺寸會隨 buff 數量變動，用 pivot 就不必每幀量它。
    // 固定往左下展開。
    //
    // pivot.x = 1 表示 position 對應到面板的右緣，所以面板整個長在游標左側 ——
    // 不需要事先知道面板寬度。面板高度會隨 buff 數量變動，用 pivot 就不必每幀去量它。
    private void UpdateTooltipPosition()
    {
        if (tooltipPanel == null) return;

        tooltipPanel.pivot = new Vector2(1f, 1f);

        // offset 取絕對值再固定為負，方向由這裡決定。
        // Inspector 上的 offset 填正填負都不影響結果。
        Vector2 finalOffset = new Vector2(
            -Mathf.Abs(offset.x),
            -Mathf.Abs(offset.y));

        tooltipPanel.position = (Vector2)Input.mousePosition + finalOffset;
    }

    public void ShowTooltip(ItemInstance itemInstance, RectTransform sourceRect = null)
    {
        if (itemInstance == null || itemInstance.item == null)
        {
            HideTooltip();
            return;
        }

        tooltipRoot.SetActive(true);
        equipmentText.text = GetDisplayName(itemInstance);
        statText.text = BuildStatText(itemInstance);


    }

    public void HideTooltip()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }

    private string GetDisplayName(ItemInstance itemInstance)
    {
        if (itemInstance is RangeWeaponInstance rwi && !string.IsNullOrEmpty(rwi.newWeaponName))
            return rwi.newWeaponName;

        if (itemInstance is MeleeWeaponInstance mwi && !string.IsNullOrEmpty(mwi.newWeaponName))
            return mwi.newWeaponName;

        if (itemInstance is ShoulderWeaponInstance swi && !string.IsNullOrEmpty(swi.newWeaponName))
            return swi.newWeaponName;

        return itemInstance.item.itemName;
    }

    private string BuildStatText(ItemInstance itemInstance)
    {
        StringBuilder sb = new StringBuilder();

        if (itemInstance is ArmorInstance ai)
        {
            AppendBuffs(sb, ai.buffs);
        }
        else if (itemInstance is RangeWeaponInstance rwi)
        {
            AppendBuffs(sb, rwi.buffs);
            if (rwi.attachment != null)
            {
                foreach (var part in rwi.attachment)
                {
                    if (part == null || part.item == null) continue;
                    sb.AppendLine();
                    sb.AppendLine($"[{part.item.itemName}]");
                    AppendBuffs(sb, part.buffs);
                }
            }
        }
        else if (itemInstance is MeleeWeaponInstance mwi)
        {
            AppendBuffs(sb, mwi.buffs);
            if (mwi.attachment != null)
            {
                foreach (var part in mwi.attachment)
                {
                    if (part == null || part.item == null) continue;
                    sb.AppendLine();
                    sb.AppendLine($"[{part.item.itemName}]");
                    AppendBuffs(sb, part.buffs);
                }
            }
        }
        else if (itemInstance is ShoulderWeaponInstance swi)
        {
            AppendBuffs(sb, swi.buffs);
            if (swi.attachment != null)
            {
                foreach (var part in swi.attachment)
                {
                    if (part == null || part.item == null) continue;
                    sb.AppendLine();
                    sb.AppendLine($"[{part.item.itemName}]");
                    AppendBuffs(sb, part.buffs);
                }
            }
        }
        else if (itemInstance is PartInstance pi)
        {
            AppendBuffs(sb, pi.buffs);
        }
        else
        {
            sb.Append("No stat data");
        }

        return sb.ToString();
    }

    private void AppendBuffs(StringBuilder sb, System.Collections.Generic.List<EquipmentBuff> buffs)
    {
        if (buffs == null || buffs.Count == 0)
        {
            sb.AppendLine("No buffs");
            return;
        }

        foreach (var buff in buffs)
        {
            string sign = buff.mode == BuffApplyMode.Multiplier
                ? $"x{1f + buff.value:0.##}"
                : $"{(buff.value >= 0 ? "+" : "")}{buff.value:0.##}";

            sb.AppendLine($"{buff.attribute}: {sign}");
        }
    }
}