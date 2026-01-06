using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    public GameObject targetGameObject;

    // When set, ColorPicker writes colors back to this instance.
    // (InventoryManager already uses this pattern.)
    public ItemInstance targetItemInstance;

    [Header("Effect (Optional)")]
    [Tooltip("If the target is a VFX prefab (ParticleSystems), ColorPicker will control this controller instead of materials.")]
    public EffectColorController targetEffectColorController;

    public List<Material> targetMaterials = new List<Material>();
    public int currentMaterialIndex = -1;
    public int currentTextureIndex = -1;

    [Header("UI Elements")]
    public RawImage hueBar;
    public RawImage colorArea;
    public RectTransform hueCursor;
    public RectTransform colorCursor;

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;
    public Transform colorDetailButtonParent;
    public ToggleGroup toggleGroup;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.white;

    [Header("Texture Settings")]
    public int hueTextureWidth = 256;
    public int hueTextureHeight = 16;
    public int colorTextureWidth = 256;
    public int colorTextureHeight = 256;

    private Texture2D hueTexture;
    private Texture2D colorTexture;

    private bool draggingHueBar = false;
    private bool draggingColorArea = false;
    private float currentHue = 0f; // 0..1

    private bool HasEffectTarget => targetEffectColorController != null;

    private void Awake()
    {
        hueTexture = new Texture2D(hueTextureWidth, hueTextureHeight);
        hueTexture.wrapMode = TextureWrapMode.Clamp;
        GenerateHueTexture();
        hueTexture.Apply();
        if (hueBar != null) hueBar.texture = hueTexture;

        colorTexture = new Texture2D(colorTextureWidth, colorTextureHeight);
        colorTexture.wrapMode = TextureWrapMode.Clamp;
        GenerateColorTexture();
        colorTexture.Apply();
        if (colorArea != null) colorArea.texture = colorTexture;

        UpdateHexColorFromCursor();
        UpdateHueCursor(); // initialize cursor + regenerate color texture once
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (hueBar != null && IsPointerOverRect(hueBar.rectTransform)) draggingHueBar = true;
            else if (colorArea != null && IsPointerOverRect(colorArea.rectTransform)) draggingColorArea = true;
        }

        if (Input.GetMouseButton(0))
        {
            if (draggingHueBar) UpdateHueCursor();
            if (draggingColorArea) UpdateColorCursor();
        }

        if (Input.GetMouseButtonUp(0))
        {
            draggingHueBar = false;
            draggingColorArea = false;
        }
    }

    // -----------------------------
    // Public API
    // -----------------------------
    public void CreateButtons()
    {
        if (targetGameObject == null) return;

        ClearnButton();

        // Effect target: one toggle per color entry in EffectColorController.colors
        if (ResolveEffectTarget())
        {
            // Ensure cache exists
            if (targetEffectColorController.colors == null)
                targetEffectColorController.colors = new List<Color>();

            if (targetEffectColorController.colors.Count == 0)
                targetEffectColorController.CacheColorsFromGroups();

            int count = targetEffectColorController.colors.Count;
            for (int i = 0; i < count; i++)
            {
                var button = Instantiate(buttonPrefab, colorDetailButtonParent);
                var label = button.transform.Find("Detail Index")?.GetComponent<TMPro.TMP_Text>();
                if (label) label.text = (i + 1).ToString();

                var t = button.GetComponent<Toggle>();
                if (t == null) continue;

                int colorIndex = i;
                t.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        SetToggleSelectedVisual(t, true);
                        SelectDetalPart(0, colorIndex);

                        var colors = targetEffectColorController.colors;
                        Color c = (colors != null && colorIndex >= 0 && colorIndex < colors.Count) ? colors[colorIndex] : Color.white;
                        CursorToColor(c);
                    }
                    else
                    {
                        SetToggleSelectedVisual(t, false);
                    }
                });

                t.group = toggleGroup;
            }

            AutoSelectFirstToggle();
            return;
        }

        // Material target: build buttons based on shader type
        if (targetMaterials == null) targetMaterials = new List<Material>();
        for (int matIndex = 0; matIndex < targetMaterials.Count; matIndex++)
        {
            var mat = targetMaterials[matIndex];
            if (mat == null) continue;

            string shaderName = mat.shader != null ? mat.shader.name : string.Empty;
            int layerCount = GetLayerCountFromShaderName(shaderName);

            for (int texIndex = 0; texIndex < layerCount; texIndex++)
            {
                var button = Instantiate(buttonPrefab, colorDetailButtonParent);
                var label = button.transform.Find("Detail Index")?.GetComponent<TMPro.TMP_Text>();
                if (label) label.text = (matIndex + texIndex + 1).ToString();

                var t = button.GetComponent<Toggle>();
                if (t == null) continue;

                int capturedMatIndex = matIndex;
                int capturedTexIndex = texIndex;

                t.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        SetToggleSelectedVisual(t, true);
                        SelectDetalPart(capturedMatIndex, capturedTexIndex);

                        string propName = capturedTexIndex == 0 ? "_BaseColor" : $"_Layer{capturedTexIndex}Color";
                        Color c = Color.white;
                        if (targetMaterials != null &&
                            capturedMatIndex >= 0 && capturedMatIndex < targetMaterials.Count &&
                            targetMaterials[capturedMatIndex] != null &&
                            targetMaterials[capturedMatIndex].HasProperty(propName))
                        {
                            c = targetMaterials[capturedMatIndex].GetColor(propName);
                        }

                        CursorToColor(c);
                    }
                    else
                    {
                        SetToggleSelectedVisual(t, false);
                    }
                });

                t.group = toggleGroup;
            }
        }

        AutoSelectFirstToggle();
    }

    public void ClearnButton()
    {
        if (colorDetailButtonParent != null)
        {
            for (int i = colorDetailButtonParent.childCount - 1; i >= 0; i--)
                Destroy(colorDetailButtonParent.GetChild(i).gameObject);
        }

        if (toggleGroup != null)
            toggleGroup.SetAllTogglesOff();
    }

    public void SelectDetalPart(int materialIndex, int textureIndex)
    {
        currentMaterialIndex = materialIndex;
        currentTextureIndex = textureIndex;
    }

    // Call this after setting targetGameObject.
    // For VFX: it will discover EffectColorController automatically.
    public void AddTargetMaterialsToList()
    {
        if (targetMaterials == null) targetMaterials = new List<Material>();
        targetMaterials.Clear();

        // If this is an effect prefab, we don't use materials.
        if (ResolveEffectTarget())
            return;

        // Helper: add ONLY element 0
        void AddElement0(Renderer renderer)
        {
            if (renderer == null) return;

            // Use .materials (not .sharedMaterials) to keep the same behavior as your current code:
            // modifying color won't permanently change the asset material.
            var mats = renderer.materials;
            if (mats != null && mats.Length > 0 && mats[0] != null)
                targetMaterials.Add(mats[0]);
        }

        var smr = targetGameObject != null ? targetGameObject.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
        if (smr != null) { AddElement0(smr); return; }

        var mr = targetGameObject != null ? targetGameObject.GetComponentInChildren<MeshRenderer>(true) : null;
        if (mr != null) { AddElement0(mr); return; }

        // Fallback: any renderer
        var r = targetGameObject != null ? targetGameObject.GetComponentInChildren<Renderer>(true) : null;
        if (r != null) AddElement0(r);
    }


    public Color UpdateHexColorFromCursor()
    {
        if (colorArea == null || colorCursor == null) return Color.white;

        Rect rect = colorArea.rectTransform.rect;
        Vector2 pos = colorCursor.anchoredPosition;

        float saturation = (pos.x - rect.xMin) / rect.width;
        float brightness = (pos.y - rect.yMin) / rect.height;
        return Color.HSVToRGB(currentHue, saturation, brightness);
    }

    public void CursorToColor(Color targetColor)
    {
        if (hueBar == null || hueCursor == null || colorArea == null || colorCursor == null) return;

        Color.RGBToHSV(targetColor, out float h, out float s, out float v);
        currentHue = h;

        Rect hueRect = hueBar.rectTransform.rect;
        float hueX = hueRect.xMin + h * hueRect.width;
        Vector2 hueCursorPos = hueCursor.anchoredPosition;
        hueCursorPos.x = hueX;
        hueCursor.anchoredPosition = hueCursorPos;

        GenerateColorTexture();
        colorTexture.Apply();

        Rect colorRect = colorArea.rectTransform.rect;
        float colorX = colorRect.xMin + s * colorRect.width;
        float colorY = colorRect.yMin + v * colorRect.height;
        colorCursor.anchoredPosition = new Vector2(colorX, colorY);
    }

    public void ChangeTargetMaterialColor(Color color, int materialIndex, int textureIndex)
    {
        ApplyColorToCurrentTarget(color, materialIndex, textureIndex);
    }

    public void ApplyCurrentColorToImage(Image buttonImage)
    {
        if (buttonImage == null)
        {
            Debug.LogWarning("ColorPicker.ApplyCurrentColorToImage: buttonImage is null.");
            return;
        }

        Color currentColor = UpdateHexColorFromCursor();
        buttonImage.color = currentColor;

        var btn = buttonImage.GetComponent<AdvancedButton>();
        if (btn != null)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = currentColor;
            cb.highlightedColor = currentColor;
            cb.pressedColor = currentColor;
            cb.selectedColor = currentColor;
            btn.colors = cb;
        }
    }

    public void SetCurrentColorFromImage(Image buttonImage)
    {
        if (buttonImage == null)
        {
            Debug.LogWarning("ColorPicker.SetCurrentColorFromImage: buttonImage is null.");
            return;
        }

        CursorToColor(buttonImage.color);
        ApplyColorToCurrentTarget(UpdateHexColorFromCursor(), currentMaterialIndex, currentTextureIndex);
    }

    // -----------------------------
    // Internal: drag logic
    // -----------------------------
    private bool IsPointerOverRect(RectTransform rectTransform)
    {
        if (rectTransform == null) return false;

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, Input.mousePosition, null, out localMousePos);
        return rectTransform.rect.Contains(localMousePos);
    }

    private void UpdateHueCursor()
    {
        if (hueBar == null || hueCursor == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hueBar.rectTransform, Input.mousePosition, null, out localPoint);
        Rect rect = hueBar.rectTransform.rect;

        float clampedX = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);

        Vector2 cursorPos = hueCursor.anchoredPosition;
        cursorPos.x = clampedX;
        hueCursor.anchoredPosition = cursorPos;

        currentHue = (clampedX - rect.xMin) / rect.width;

        GenerateColorTexture();
        colorTexture.Apply();

        ApplyColorToCurrentTarget(UpdateHexColorFromCursor(), currentMaterialIndex, currentTextureIndex);
    }

    private void UpdateColorCursor()
    {
        if (colorArea == null || colorCursor == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            colorArea.rectTransform, Input.mousePosition, null, out localPoint);
        Rect rect = colorArea.rectTransform.rect;

        float clampedX = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
        float clampedY = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);
        colorCursor.anchoredPosition = new Vector2(clampedX, clampedY);

        ApplyColorToCurrentTarget(UpdateHexColorFromCursor(), currentMaterialIndex, currentTextureIndex);
    }

    private void GenerateHueTexture()
    {
        for (int x = 0; x < hueTextureWidth; x++)
        {
            float h = (float)x / (hueTextureWidth - 1);
            Color col = Color.HSVToRGB(h, 1f, 1f);
            for (int y = 0; y < hueTextureHeight; y++)
                hueTexture.SetPixel(x, y, col);
        }
    }

    private void GenerateColorTexture()
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

    // -----------------------------
    // Internal: apply + persist
    // -----------------------------
    private void ApplyColorToCurrentTarget(Color color, int materialIndex, int textureIndex)
    {
        if (textureIndex < 0) return;

        // Effect first
        if (ResolveEffectTarget())
        {
            ApplyEffectColor(color, textureIndex);
            return;
        }

        // Materials
        if (targetMaterials == null || materialIndex < 0 || materialIndex >= targetMaterials.Count) return;

        var mat = targetMaterials[materialIndex];
        if (mat == null) return;

        string propName = textureIndex == 0 ? "_BaseColor" : $"_Layer{textureIndex}Color";
        if (mat.HasProperty(propName))
            mat.SetColor(propName, color);
        else
            Debug.LogWarning($"{mat.name} missing property: {propName}");

        WriteColorBackToItemInstance(color, materialIndex, textureIndex);
    }

    private void ApplyEffectColor(Color color, int colorIndex)
    {
        if (targetEffectColorController == null) return;

        if (targetEffectColorController.colors == null)
            targetEffectColorController.colors = new List<Color>();

        while (targetEffectColorController.colors.Count <= colorIndex)
            targetEffectColorController.colors.Add(Color.white);

        targetEffectColorController.colors[colorIndex] = color;

        // Apply to particle systems
        targetEffectColorController.ApplyFromColorsList();

        // Persist
        WriteColorBackToItemInstance(color, 0, colorIndex);
    }

    private void WriteColorBackToItemInstance(Color color, int materialIndex, int textureIndex)
    {
        if (textureIndex < 0) return;
        if (targetItemInstance == null) return;

        List<Color> colorsList = null;
        string shaderName = null;

        if (targetItemInstance is ArmorInstance ai)
        {
            colorsList = ai.colors;
            shaderName = ai.shaderName;
        }
        else if (targetItemInstance is RangeWeaponInstance rwi)
        {
            colorsList = rwi.colors;
            shaderName = rwi.shaderName;
        }
        else if (targetItemInstance is MeleeWeaponInstance mwi)
        {
            colorsList = mwi.colors;
            shaderName = mwi.shaderName;
        }
        else if (targetItemInstance is PartInstance pi)
        {
            colorsList = pi.colors;
            shaderName = pi.shaderName;
        }
        else
        {
            return;
        }

        if (colorsList == null) colorsList = new List<Color>();

        // For material targets, try to fill shaderName when missing.
        if (!HasEffectTarget && string.IsNullOrEmpty(shaderName))
        {
            if (materialIndex >= 0 && targetMaterials != null &&
                materialIndex < targetMaterials.Count && targetMaterials[materialIndex] != null)
            {
                shaderName = targetMaterials[materialIndex].shader.name;

                if (targetItemInstance is ArmorInstance ai2) ai2.shaderName = shaderName;
                else if (targetItemInstance is RangeWeaponInstance rwi2) rwi2.shaderName = shaderName;
                else if (targetItemInstance is MeleeWeaponInstance mwi2) mwi2.shaderName = shaderName;
                else if (targetItemInstance is PartInstance pi2) pi2.shaderName = shaderName;
            }
        }

        // Required length:
        // - Material shaders: follow Mix3/4/5
        // - Effect: store by index (textureIndex == colorIndex)
        int requiredCount = 1;
        if (HasEffectTarget)
        {
            requiredCount = Mathf.Max(1, textureIndex + 1);
        }
        else if (!string.IsNullOrEmpty(shaderName))
        {
            if (shaderName.Contains("Mix 5")) requiredCount = 5;
            else if (shaderName.Contains("Mix 4")) requiredCount = 4;
            else if (shaderName.Contains("Mix 3")) requiredCount = 3;
            else requiredCount = Mathf.Max(requiredCount, textureIndex + 1);
        }
        else
        {
            requiredCount = Mathf.Max(requiredCount, textureIndex + 1);
        }

        while (colorsList.Count < requiredCount) colorsList.Add(Color.white);
        while (colorsList.Count <= textureIndex) colorsList.Add(Color.white);

        colorsList[textureIndex] = color;

        if (targetItemInstance is ArmorInstance ai3) ai3.colors = colorsList;
        else if (targetItemInstance is RangeWeaponInstance rwi3) rwi3.colors = colorsList;
        else if (targetItemInstance is MeleeWeaponInstance mwi3) mwi3.colors = colorsList;
        else if (targetItemInstance is PartInstance pi3) pi3.colors = colorsList;
    }

    // -----------------------------
    // Helpers
    // -----------------------------
    private bool ResolveEffectTarget()
    {
        if (targetGameObject == null)
        {
            targetEffectColorController = null;
            return false;
        }

        // 只把「目標本體」或「其父系」當作 effect 來源
        // 目標是 weapon root 時：通常自己沒有 ParticleSystem / ParticleSystemRenderer -> false
        bool targetIsParticleObject =
            targetGameObject.GetComponent<ParticleSystem>() != null ||
            targetGameObject.GetComponent<ParticleSystemRenderer>() != null;

        if (!targetIsParticleObject)
        {
            targetEffectColorController = null;
            return false;
        }

        // 只找自己或父系的 controller（不要找 children）
        targetEffectColorController = targetGameObject.GetComponentInParent<EffectColorController>(true);
        return targetEffectColorController != null;
    }


    private int GetLayerCountFromShaderName(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName)) return 1;
        if (shaderName.Contains("Mix 5")) return 5;
        if (shaderName.Contains("Mix 4")) return 4;
        if (shaderName.Contains("Mix 3")) return 3;
        return 1;
    }

    private void AutoSelectFirstToggle()
    {
        if (colorDetailButtonParent == null) return;
        if (colorDetailButtonParent.childCount <= 0) return;

        var firstToggle = colorDetailButtonParent.GetChild(0).GetComponent<Toggle>();
        if (firstToggle != null) firstToggle.isOn = true;
    }

    private void SetToggleSelectedVisual(Toggle t, bool isSelected)
    {
        if (t == null) return;

        Color c = isSelected ? selectedColor : normalColor;
        ColorBlock cb = t.colors;
        cb.normalColor = c;
        cb.selectedColor = c;
        cb.highlightedColor = c;
        cb.pressedColor = c;
        t.colors = cb;
    }
}
