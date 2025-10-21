using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryBackgroundClickCatcher : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData e)
    {
        InventoryManager.Instance.HideAllRemoveButtons();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); // 可選：清掉選取
    }
}
