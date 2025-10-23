using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    private Toggle toggle;

    public Color onColor;
    public Color offColor;
    void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    public void OnToggleValueChanged(bool isOn)
    {
        ColorBlock cb = toggle.colors;
        if (isOn)
        {
            cb.normalColor = onColor;
            cb.selectedColor = onColor;
        }
        else
        {
            cb.normalColor = offColor;
            cb.selectedColor = offColor;
        }
        toggle.colors = cb;
    }
}
