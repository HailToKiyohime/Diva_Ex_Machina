using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

public class AdvancedButton : Selectable, IPointerClickHandler
{
    [Header("Click Events")]
    public UnityEvent OnLeftClick;
    public UnityEvent OnRightClick;
    public UnityEvent OnMiddleClick;

    private Coroutine _resetRoutine;

    // 注意：Reset 是 Unity Message，不是 virtual，不要 override
    protected override void Reset()
    {
        // 取得或補上 Image，並指定 targetGraphic
        var imageComponent = GetComponent<Image>();
        if (imageComponent == null)
            imageComponent = gameObject.AddComponent<Image>();

        targetGraphic = imageComponent;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        DoStateTransition(SelectionState.Pressed, true);

        switch (eventData.button)
        {
            default:
            case PointerEventData.InputButton.Left:
                OnLeftClick?.Invoke();
                break;
            case PointerEventData.InputButton.Right:
                OnRightClick?.Invoke();
                break;
            case PointerEventData.InputButton.Middle:
                OnMiddleClick?.Invoke();
                break;
        }

        if (_resetRoutine != null)
            StopCoroutine(_resetRoutine);

        _resetRoutine = StartCoroutine(OnFinishSubmit());
    }

    private IEnumerator OnFinishSubmit()
    {
        var fadeTime = colors.fadeDuration;
        var elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 回到目前狀態（或主人也可以改成 SelectionState.Normal 看需求）
        DoStateTransition(currentSelectionState, false);
        _resetRoutine = null;
    }
}
