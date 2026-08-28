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

    // UIBehaviour.Reset() 被包在 #if UNITY_EDITOR 裡面。
    // Editor 編譯時基底有這個方法，override 成立；build player 時整個宣告被切掉，
    // 基底沒有 Reset() 可以 override → CS0115。所以這裡也要一起切掉。
    //
    // Reset() 本來就只在 Editor 有意義（Inspector 按 Reset、或第一次掛上元件時
    // 觸發），build 裡永遠不會被呼叫，切掉不損失功能。
    //
    // OnValidate() 在 UIBehaviour 裡跟 Reset() 同一個 #if UNITY_EDITOR 區塊，
    // 之後如果要 override 它，一樣要包起來。
#if UNITY_EDITOR
    protected override void Reset()
    {
        // 取得或補上 Image，並指定 targetGraphic
        var imageComponent = GetComponent<Image>();
        if (imageComponent == null)
            imageComponent = gameObject.AddComponent<Image>();

        targetGraphic = imageComponent;
    }
#endif

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