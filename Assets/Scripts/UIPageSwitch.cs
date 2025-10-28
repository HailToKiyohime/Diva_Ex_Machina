using UnityEngine;
using UnityEngine.UI;

public class UIPageSwitch : MonoBehaviour
{
    public Color normalColor;
    public Color selectedColor;
    public GameObject[] pages;
    public Toggle[] toggles;
    private void Start()
    {
        foreach (var toggle in toggles)
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    ColorBlock cb = toggle.colors;
                    cb.normalColor = selectedColor;
                    cb.selectedColor = selectedColor;
                    cb.highlightedColor = selectedColor;
                    cb.pressedColor = selectedColor;
                    toggle.colors = cb;
                }
                else
                {
                    ColorBlock cb = toggle.colors;
                    cb.normalColor = normalColor;
                    cb.selectedColor = normalColor;
                    cb.highlightedColor = normalColor;
                    cb.pressedColor = normalColor;
                    toggle.colors = cb;
                }
            });
        }
    }
    public void SwitchPage(int pageIndex)
    {
        foreach (var page in pages)
        {
            page.SetActive(false);
        }
        pages[pageIndex].SetActive(true);
    }
}
