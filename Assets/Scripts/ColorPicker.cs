using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{

    public GameObject targetGameObject;
    public List<Material> targetMaterials;
    public int currentMaterialIndex = -1;
    public int currentTextureIndex = -1;
    [Header("UI Elements")]
    public RawImage hueBar;              // UI RawImage for the hue bar
    public RawImage colorArea;           // UI RawImage for the color area
    public RectTransform hueCursor;      // Cursor for the hue bar (horizontal movement only)
    public RectTransform colorCursor;    // Cursor for the color area
    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;
    public Transform colorDetailButtonParent;
    public ToggleGroup toggleGroup;
    public Color normalColor;
    public Color selectedColor;
    [Header("Texture Settings")]
    public int hueTextureWidth = 256;
    public int hueTextureHeight = 16;
    public int colorTextureWidth = 256;
    public int colorTextureHeight = 256;

    private Texture2D hueTexture;
    private Texture2D colorTexture;

    // Flags to determine if the user is currently dragging the Hue Bar or Color Area.
    private bool draggingHueBar = false;
    private bool draggingColorArea = false;
    private float currentHue = 0f; // current hue value (0 to 1)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Create and assign the hue bar texture.
        hueTexture = new Texture2D(hueTextureWidth, hueTextureHeight);
        hueTexture.wrapMode = TextureWrapMode.Clamp;
        GenerateHueTexture();
        hueTexture.Apply();
        hueBar.texture = hueTexture;

        // Create and assign the color area texture.
        colorTexture = new Texture2D(colorTextureWidth, colorTextureHeight);
        colorTexture.wrapMode = TextureWrapMode.Clamp;
        GenerateColorTexture();
        colorTexture.Apply();
        colorArea.texture = colorTexture;

        // Set initial HEX color code based on the current color area cursor position.
        UpdateHexColorFromCursor();

        UpdateHueCursor();

        /*
        // Add listener to the TMP InputField for when the user enters a HEX code.
        if (hexInputField != null)
        {
            hexInputField.onEndEdit.AddListener(OnHexInputChanged);
        }*/
    }
    // Update is called once per frame
    void Update()
    {
        // On mouse button down, determine which element is being clicked.
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverRect(hueBar.rectTransform))
            {
                draggingHueBar = true;
            }
            else if (IsPointerOverRect(colorArea.rectTransform))
            {
                draggingColorArea = true;
            }
        }

        // While the mouse button is held, update the corresponding cursor even if the pointer leaves the element.
        if (Input.GetMouseButton(0))
        {
            if (draggingHueBar)
            {
                UpdateHueCursor();
            }
            if (draggingColorArea)
            {
                UpdateColorCursor();
            }
        }

        // Reset dragging flags when the mouse button is released.
        if (Input.GetMouseButtonUp(0))
        {
            draggingHueBar = false;
            draggingColorArea = false;
        }
    }

    public void CreateButtons()
    {
        if (targetGameObject == null) return;

        // 先清空舊按鈕
        ClearnButton();

        // 依 shader 產生細節用按鈕
        for (int x = 0; x < targetMaterials.Count; x++)
        {
            string shaderName = targetMaterials[x].shader.name;
            Debug.Log("shaderName:" + shaderName);

            if (shaderName.Contains("Mix 3"))
            {
                for (int i = 0; i < 3; i++)
                {
                    var button = Instantiate(buttonPrefab, colorDetailButtonParent);
                    var label = button.transform.Find("Detail Index")?.GetComponent<TMPro.TMP_Text>();
                    int num = x + i + 1;
                    if (label) label.text = "" + num;

                    var btn = button.GetComponent<Toggle>();
                    if (btn != null)
                    {
                        int materialIdx = x;
                        int textureIdx = i;
                        btn.onValueChanged.AddListener(isOn =>
                        {
                            if (isOn)
                            {
                                ColorBlock cb = btn.colors;
                                cb.normalColor = selectedColor;
                                cb.selectedColor = selectedColor;
                                cb.highlightedColor = selectedColor;
                                cb.pressedColor = selectedColor;
                                btn.colors = cb;
                                SelectDetalPart(materialIdx, textureIdx);
                            }
                            else
                            {
                                ColorBlock cb = btn.colors;
                                cb.normalColor = normalColor;
                                cb.selectedColor = normalColor;
                                cb.highlightedColor = normalColor;
                                cb.pressedColor = normalColor;
                                btn.colors = cb;
                            }
                        });
                        btn.group = toggleGroup;
                    }
                }
            }
            else if (shaderName.Contains("Mix 4"))
            {
                for (int i = 0; i < 4; i++)
                {
                    var button = Instantiate(buttonPrefab, colorDetailButtonParent);
                    var label = button.transform.Find("Detail Index")?.GetComponent<TMPro.TMP_Text>();
                    int num = x + i + 1;
                    if (label) label.text = "" + num;

                    var btn = button.GetComponent<Toggle>();
                    if (btn != null)
                    {
                        int materialIdx = x;
                        int textureIdx = i;
                        btn.onValueChanged.AddListener(isOn =>
                        {
                            if (isOn)
                            {
                                ColorBlock cb = btn.colors;
                                cb.normalColor = selectedColor;
                                cb.selectedColor = selectedColor;
                                cb.highlightedColor = selectedColor;
                                cb.pressedColor = selectedColor;
                                btn.colors = cb;
                                SelectDetalPart(materialIdx, textureIdx);
                            }
                            else
                            {
                                ColorBlock cb = btn.colors;
                                cb.normalColor = normalColor;
                                cb.selectedColor = normalColor;
                                cb.highlightedColor = normalColor;
                                cb.pressedColor = normalColor;
                                btn.colors = cb;
                            }
                        });
                        btn.group = toggleGroup;
                    }
                }
            }
            else if (shaderName.Contains("Mix 5"))
            {
                for (int i = 0; i < 5; i++)
                {
                    var button2 = Instantiate(buttonPrefab, colorDetailButtonParent);
                    var label = button2.transform.Find("Detail Index")?.GetComponent<TMPro.TMP_Text>();
                    int num = x + i + 1;
                    if (label) label.text = "" + num;

                    var btn = button2.GetComponent<Toggle>();
                    if (btn != null)
                    {
                        int materialIdx = x;
                        int textureIdx = i;
                        btn.onValueChanged.AddListener(isOn =>
                        {
                            if (isOn)
                            {
                                ColorBlock cb = btn.colors;
                                cb.normalColor = selectedColor;
                                cb.selectedColor = selectedColor;
                                cb.highlightedColor = selectedColor;
                                cb.pressedColor = selectedColor;
                                btn.colors = cb;
                                SelectDetalPart(materialIdx, textureIdx);
                            }
                            else
                            {
                                ColorBlock cb = btn.colors;
                                cb.normalColor = normalColor;
                                cb.selectedColor = normalColor;
                                cb.highlightedColor = normalColor;
                                cb.pressedColor = normalColor;
                                btn.colors = cb;
                            }
                        });
                        btn.group = toggleGroup;
                    }
                }
            }
        }

        // ★ 在這裡自動勾第一顆按鈕
        if (colorDetailButtonParent.childCount > 0)
        {
            var firstToggle = colorDetailButtonParent.GetChild(0).GetComponent<Toggle>();
            if (firstToggle != null)
            {
                firstToggle.isOn = true;   // 會觸發 onValueChanged → SelectDetalPart()
            }
        }
    }

    public void ClearnButton()
    {
        for (int i = colorDetailButtonParent.childCount - 1; i >= 0; i--)
            Destroy(colorDetailButtonParent.GetChild(i).gameObject);

        // 確保不再觸發舊按鈕的選取狀態
        if (toggleGroup != null)
            toggleGroup.SetAllTogglesOff();
    }

    public void SelectDetalPart(int materialIndex, int textureIndex)
    {
        currentMaterialIndex = materialIndex;
        currentTextureIndex = textureIndex;
    }

    // Generates the Hue Bar texture using a gradient across hues.
    void GenerateHueTexture()
    {
        for (int x = 0; x < hueTextureWidth; x++)
        {
            float h = (float)x / (hueTextureWidth - 1);
            Color col = Color.HSVToRGB(h, 1f, 1f);
            for (int y = 0; y < hueTextureHeight; y++)
            {
                hueTexture.SetPixel(x, y, col);
            }
        }
    }
    // Generates the Color Area texture based on the current hue.
    void GenerateColorTexture()
    {
        for (int x = 0; x < colorTextureWidth; x++)
        {
            for (int y = 0; y < colorTextureHeight; y++)
            {
                float saturation = (float)x / (colorTextureWidth - 1);
                float brightness = (float)y / (colorTextureHeight - 1);
                Color col = Color.HSVToRGB(currentHue, saturation, brightness);
                colorTexture.SetPixel(x, y, col);
            }
        }
    }
    // Calculates the selected color based on the color cursor's position and updates the HEX string and input field.
    public Color UpdateHexColorFromCursor()
    {
        Rect rect = colorArea.rectTransform.rect;
        Vector2 pos = colorCursor.anchoredPosition;
        float saturation = (pos.x - rect.xMin) / rect.width;
        float brightness = (pos.y - rect.yMin) / rect.height;
        Color selectedColor = Color.HSVToRGB(currentHue, saturation, brightness);
        return selectedColor;
    }
    // Helper: Converts the mouse screen position into local coordinates of the given RectTransform.
    bool IsPointerOverRect(RectTransform rectTransform)
    {
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, Input.mousePosition, null, out localMousePos);
        return rectTransform.rect.Contains(localMousePos);
    }
    void UpdateHueCursor()
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hueBar.rectTransform, Input.mousePosition, null, out localPoint);
        Rect rect = hueBar.rectTransform.rect;

        // Clamp the x position to stay within the hue bar.
        float clampedX = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);

        // Update the hue cursor's anchored position (horizontal only).
        Vector2 cursorPos = hueCursor.anchoredPosition;
        cursorPos.x = clampedX;
        hueCursor.anchoredPosition = cursorPos;

        // Calculate the current hue.
        float normalizedHue = (clampedX - rect.xMin) / rect.width;
        currentHue = normalizedHue;

        // Regenerate the color area texture with the updated hue.
        GenerateColorTexture();
        colorTexture.Apply();

        // Update the HEX color code.
        UpdateHexColorFromCursor();
    }
    // Updates the color area cursor position based on the mouse, clamped to the area.
    void UpdateColorCursor()
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            colorArea.rectTransform, Input.mousePosition, null, out localPoint);
        Rect rect = colorArea.rectTransform.rect;

        // Clamp the position so the cursor stays within the color area.
        float clampedX = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
        float clampedY = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);
        colorCursor.anchoredPosition = new Vector2(clampedX, clampedY);

        // Update the HEX color code based on the new cursor position.
        ChangeTargetMaterialColor(UpdateHexColorFromCursor(), currentMaterialIndex, currentTextureIndex);
    }
    public void ChangeTargetMaterialColor(Color color, int materialIndex, int textureIndex)
    {
        if (targetMaterials == null || materialIndex < 0 || materialIndex >= targetMaterials.Count)
            return;

        var mat = targetMaterials[materialIndex];
        if (mat == null) return;

        string propName = textureIndex switch
        {
            0 => "_BaseColor",
            _ => $"_Layer{textureIndex}Color"
        };

        if (mat.HasProperty(propName))
            mat.SetColor(propName, color);
        else
            Debug.LogWarning($"{mat.name} missing property: {propName}");

        WriteColorBackToArmorInstance(color, materialIndex, textureIndex);
    }
    public void AddTargetMaterialsToList()
    {
        targetMaterials.Clear(); // ← 加這行
        if (targetGameObject.GetComponent<SkinnedMeshRenderer>())
        {
            targetMaterials.Add(targetGameObject.GetComponent<SkinnedMeshRenderer>().materials[0]);
            foreach (Transform child in targetGameObject.transform)
                targetMaterials.Add(child.GetComponent<SkinnedMeshRenderer>().materials[0]);
        }else if (targetGameObject.GetComponent<MeshRenderer>())
        {
            targetMaterials.Add(targetGameObject.GetComponent<MeshRenderer>().materials[0]);
            foreach (Transform child in targetGameObject.transform)
                targetMaterials.Add(child.GetComponent<MeshRenderer>().materials[0]);
        }
    }

    public void WriteColorBackToArmorInstance(Color color, int materialIndex, int textureIndex)
    {
        // 基本防呆
        if (textureIndex < 0) return;
        if (InventoryManager.Instance == null || EquipmentManager.Instance == null) return;

        int slotIndex = InventoryManager.Instance.GetSelectedSlotIndex();
        var slots = EquipmentManager.Instance.equipmentSlots;
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        // 只處理已裝備且為 Armor 的情況
        if (slots[slotIndex].item is not ArmorInstance ai) return;

        // 依 shaderName 推斷需要的顏色數量；若未設則以當前目標材質的 shader 推斷
        string shaderName = ai.shaderName;
        if (string.IsNullOrEmpty(shaderName))
        {
            if (materialIndex >= 0 && targetMaterials != null &&
                materialIndex < targetMaterials.Count && targetMaterials[materialIndex] != null)
            {
                shaderName = targetMaterials[materialIndex].shader.name;
                ai.shaderName = shaderName; // 補記
            }
        }

        int requiredCount = 1;
        if (!string.IsNullOrEmpty(shaderName))
        {
            if (shaderName.Contains("Mix 5")) requiredCount = 5;
            else if (shaderName.Contains("Mix 4")) requiredCount = 4;
            else if (shaderName.Contains("Mix 3")) requiredCount = 3;
            else requiredCount = Mathf.Max(requiredCount, textureIndex + 1); // 非 Mix 系列時，最少容納到當前索引
        }
        else
        {
            requiredCount = Mathf.Max(requiredCount, textureIndex + 1);
        }

        // 確保 list 長度足夠（以白色填充）
        while (ai.colors.Count < requiredCount)
            ai.colors.Add(Color.white);

        // 若仍不足以覆寫到指定 index，擴充到 textureIndex
        while (ai.colors.Count <= textureIndex)
            ai.colors.Add(Color.white);

        // 回寫
        ai.colors[textureIndex] = color;
    }
}