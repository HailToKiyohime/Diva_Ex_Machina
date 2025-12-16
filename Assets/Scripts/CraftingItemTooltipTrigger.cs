using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 掛在 Crafting 右側背包按鈕（buttonPrefab）上：
/// Hover 時呼叫 CraftingManager 顯示 Tooltip；離開時隱藏。
/// </summary>
public class CraftingItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CraftingManager _crafting;
    private ItemInstance _item;

    public void Init(CraftingManager crafting, ItemInstance item)
    {
        _crafting = crafting;
        _item = item;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_crafting != null && _item != null)
            _crafting.ShowTooltip(_item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_crafting != null)
            _crafting.HideTooltip();
    }

    private void OnDisable()
    {
        // 清單被清空 / 物件被 Destroy 時，保底把 Tooltip 關掉，避免殘留
        if (_crafting != null)
            _crafting.HideTooltip();
    }
}
