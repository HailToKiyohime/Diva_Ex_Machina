using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{

    public static ColorPicker Instance; // Singleton instance

    public GameObject TargetGameObject;
    public Material[] TargetMaterials;
    public int currentMaterialIndex;
    public int currentTextureIndex;
    [Header("UI Elements")]
    public RawImage hueBar;              // UI RawImage for the hue bar
    public RawImage colorArea;           // UI RawImage for the color area
    public RectTransform hueCursor;      // Cursor for the hue bar (horizontal movement only)
    public RectTransform colorCursor;    // Cursor for the color area

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
    void Awake()
    {

        // Implement the singleton.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

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
        /*
        if ()
        {
            ChangeTargetMaterialColor
        }*/
        /*
        if (targetMaterial != null && !string.IsNullOrEmpty(targetColorProperty))
        {
            UpdateTargetMaterialColor(GetCurrentColor());
        }*/
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
    void UpdateHexColorFromCursor()
    {
        Rect rect = colorArea.rectTransform.rect;
        Vector2 pos = colorCursor.anchoredPosition;
        float saturation = (pos.x - rect.xMin) / rect.width;
        float brightness = (pos.y - rect.yMin) / rect.height;
        Color selectedColor = Color.HSVToRGB(currentHue, saturation, brightness);
        /*hexColorCode = "#" + ColorUtility.ToHtmlStringRGB(selectedColor);
        if (hexInputField != null)
        {
            hexInputField.text = hexColorCode;
        }*/
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
        UpdateHexColorFromCursor();
    }
    public void ChangeTargetMaterialColor(Color color, int materialIndex, int textureIndex)
    {
        if (TargetMaterials == null || materialIndex < 0 || materialIndex >= TargetMaterials.Length)
            return;

        var mat = TargetMaterials[materialIndex];
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
    }
}
