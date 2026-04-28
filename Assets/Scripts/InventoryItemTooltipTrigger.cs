using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ItemInstance itemInstance;

    public void Setup(ItemInstance item)
    {
        itemInstance = item;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInstance != null)
            TooltipManager.Instance.ShowTooltip(itemInstance, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}