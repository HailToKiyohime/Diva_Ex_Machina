using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIPageSwitch : MonoBehaviour
{
    public Color normalColor;
    public Color selectedColor;
    public GameObject[] pages;
    public List<Toggle> toggles;

    // 記住每個 Toggle 綁過的 handler，避免 UpdateToggles 重複疊加
    private readonly Dictionary<Toggle, UnityAction<bool>> _handlers = new();

    private void Start()
    {
        UpdateToggles();
    }

    public void SwitchPage(int pageIndex)
    {
        SFXManager.Instance.PlayFeedback("ClickFeedback");
        foreach (var page in pages) page.SetActive(false);
        pages[pageIndex].SetActive(true);
    }

    public void UpdateToggles()
    {
        if (toggles == null) return;

        // 1) 移除 Missing (null) 參考
        toggles.RemoveAll(t => t == null);

        // 2) 綁定/更新 listener（避免重複 AddListener）
        foreach (var t in toggles)
        {
            if (t == null) continue;

            if (_handlers.TryGetValue(t, out var oldHandler) && oldHandler != null)
                t.onValueChanged.RemoveListener(oldHandler);

            UnityAction<bool> newHandler = isOn => ApplyColor(t, isOn);
            _handlers[t] = newHandler;
            t.onValueChanged.AddListener(newHandler);

            // 3) 立刻刷新一次顏色（確保一開始狀態就正確）
            ApplyColor(t, t.isOn);
        }

        // 4) 清掉字典裡已不存在/被銷毀的 toggle
        var keys = new List<Toggle>(_handlers.Keys);
        foreach (var key in keys)
        {
            if (key == null || !toggles.Contains(key))
                _handlers.Remove(key);
        }
    }

    private void ApplyColor(Toggle toggle, bool isOn)
    {
        var cb = toggle.colors;
        var c = isOn ? selectedColor : normalColor;
        cb.normalColor = c;
        cb.selectedColor = c;
        cb.highlightedColor = c;
        cb.pressedColor = c;
        toggle.colors = cb;
    }
}
