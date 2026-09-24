using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lets the character's cloth (the base body's clothing mesh — NOT an
/// equippable Armor ScriptableObject) be recolored and toon-shaded the same
/// way Armor already is: Mix 3/4/5 layer colors get baked into a texture and
/// fed into a cloned UTS3 material, exactly like LayeredToonMaterialController
/// does for armor (see EquipmentManager.TryEquipFromInventory for reference).
///
/// There is no UI for this yet. Attach this component to the GameObject that
/// carries the Cloth renderer (Clothes.mat uses the same "Mix" shader as the
/// armor materials, so it works out of the box), assign a UTS3 template
/// material in the Inspector (duplicate ToonArmor.mat and tweak it, or make a
/// dedicated "ToonClothes" material), and this script does the rest at
/// runtime — no other setup required.
///
/// Two supported layouts, controlled by materialSlotIndex:
///   - 0 (default): Cloth has its own dedicated Renderer with one material,
///     same as every armor piece. This path simply reuses
///     LayeredToonMaterialController, so it stays 100% consistent with armor
///     and with ColorPicker's existing generic logic.
///   - >0: Cloth is one material slot on a Renderer shared with other body
///     parts (skin/hair/eyes). This path bakes only that slot, leaving the
///     renderer's other materials untouched.
///
/// Hooking this up to a future UI:
///   - Simplest: call ConfigureColorPicker(picker) once (e.g. when a
///     "Customize Cloth" panel opens) to point an existing ColorPicker at
///     this cloth mesh, the same way armor pieces are pointed at it today.
///   - Or: call GetLayerCount() / GetLayerColor() / SetLayerColor() directly
///     from custom UI code without going through ColorPicker at all.
///
/// Persistence is intentionally left open: initialColors is serialized so a
/// designer can bake in a default look, but saving player-chosen colors
/// across sessions (ES3, a save slot, etc.) isn't wired up yet — hook that
/// into SetLayerColor/initialColors once a save path for cosmetics exists.
/// </summary>
[DisallowMultipleComponent]
public class ClothColorController : MonoBehaviour
{
    [Header("Renderer")]
    [Tooltip("Leave empty to auto-find a Renderer on this GameObject or its children.")]
    [SerializeField] private Renderer clothRenderer;

    [Tooltip("Material slot index on clothRenderer that holds the cloth's Mix material. " +
             "0 if Cloth has its own dedicated Renderer (same setup as Armor). " +
             "Use a higher index only if Cloth shares a Renderer with Skin/Hair/Eyes.")]
    [SerializeField] private int materialSlotIndex = 0;

    [Header("Toon Shader (same role as Armor.uts3MaterialTemplate)")]
    [Tooltip("Original UTS3 material used as the runtime template. This is cloned at runtime and never modified.")]
    [SerializeField] private Material uts3MaterialTemplate;

    [Tooltip("Optional. Leave empty to let the controller find 'Hidden/MixAlbedoBaker' itself.")]
    [SerializeField] private Shader bakeShader;

    [Header("Starting Colors (optional)")]
    [Tooltip("Restored on Awake, in Base, Layer1..Layer4 order. Leave empty to keep the material's own baked-in colors.")]
    [SerializeField] private List<Color> initialColors = new List<Color>();

    // Slot 0 path: reuse the proven Armor pipeline as-is.
    private LayeredToonMaterialController toonController;

    // Slot > 0 path: same bake, done by hand against one entry of Renderer.materials.
    private Material runtimeMixMaterial;
    private Material bakerMaterial;
    private Material runtimeUTSMaterial;
    private RenderTexture bakedAlbedo;

    public Renderer ClothRenderer => clothRenderer;
    public bool UsesDedicatedRenderer => materialSlotIndex == 0;

    private void Awake()
    {
        if (clothRenderer == null)
            clothRenderer = GetComponent<Renderer>();
        if (clothRenderer == null)
            clothRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (clothRenderer == null)
            clothRenderer = GetComponentInChildren<MeshRenderer>(true);

        if (clothRenderer == null)
        {
            Debug.LogWarning($"{name}: ClothColorController could not find a Renderer.", this);
            return;
        }

        if (materialSlotIndex == 0)
        {
            toonController = clothRenderer.GetComponent<LayeredToonMaterialController>();
            if (toonController == null)
                toonController = clothRenderer.gameObject.AddComponent<LayeredToonMaterialController>();

            toonController.Configure(clothRenderer, uts3MaterialTemplate, bakeShader);

            if (initialColors != null && initialColors.Count > 0)
                toonController.ApplySavedColors(initialColors);
        }
        else
        {
            SetupSlotPath();

            if (initialColors != null && initialColors.Count > 0)
                ApplySavedColorsSlotPath(initialColors);
        }
    }

    // -----------------------------
    // Public color API
    // -----------------------------

    public int GetLayerCount()
    {
        return materialSlotIndex == 0
            ? (toonController != null ? toonController.GetColorSlotCount() : 0)
            : GetColorSlotCountSlotPath();
    }

    public Color GetLayerColor(int layerIndex)
    {
        var mat = GetSourceMixMaterial();
        if (mat == null) return Color.white;

        string prop = LayerColorProperty(layerIndex);
        return mat.HasProperty(prop) ? mat.GetColor(prop) : Color.white;
    }

    /// <summary>
    /// Call this from a future color-picker UI. layerIndex 0 = base coat, 1..4 = overlay layers
    /// (mirrors Mix 3/4/5's _BaseColor/_Layer1Color../_Layer4Color).
    /// </summary>
    public void SetLayerColor(int layerIndex, Color color)
    {
        var mat = GetSourceMixMaterial();
        if (mat == null) return;

        string prop = LayerColorProperty(layerIndex);
        if (!mat.HasProperty(prop))
        {
            Debug.LogWarning($"{name}: material '{mat.name}' has no property {prop}.", this);
            return;
        }

        mat.SetColor(prop, color);

        if (materialSlotIndex == 0)
            toonController?.Rebake();
        else
            RebakeSlotPath();

        while (initialColors.Count <= layerIndex) initialColors.Add(Color.white);
        initialColors[layerIndex] = color;
    }

    /// <summary>
    /// Wires an existing ColorPicker straight at this cloth mesh, the same way
    /// EquipmentManager/InventoryManager point it at an equipped Armor piece.
    /// Call this once a UI panel opens (e.g. from a "Customize Cloth" button).
    /// </summary>
    public void ConfigureColorPicker(ColorPicker picker)
    {
        if (picker == null || clothRenderer == null) return;

        picker.targetEffectColorController = null;
        picker.targetItemInstance = null; // Cloth colors aren't saved on an ItemInstance (yet).

        if (materialSlotIndex == 0)
        {
            // ColorPicker.AddTargetMaterialsToList() finds our LayeredToonMaterialController
            // automatically and edits its baked SourceMixMaterial — no extra wiring needed.
            picker.targetGameObject = clothRenderer.gameObject;
            picker.AddTargetMaterialsToList();
        }
        else
        {
            picker.targetGameObject = gameObject;
            picker.targetMaterials = new List<Material> { GetSourceMixMaterial() };
            picker.SelectDetalPart(0, 0);
        }
    }

    private static string LayerColorProperty(int layerIndex) =>
        layerIndex <= 0 ? "_BaseColor" : $"_Layer{layerIndex}Color";

    private Material GetSourceMixMaterial()
    {
        if (materialSlotIndex == 0)
            return toonController != null ? toonController.SourceMixMaterial : null;

        EnsureSlotMaterialsCreated();
        return runtimeMixMaterial;
    }

    // ---------------------------------------------------------------
    // Slot-index path: same bake logic as LayeredToonMaterialController,
    // but targeting one entry of Renderer.materials instead of assuming the
    // renderer only ever has one material.
    // ---------------------------------------------------------------

    private void SetupSlotPath()
    {
        EnsureSlotMaterialsCreated();
        RebakeSlotPath();
    }

    private void EnsureSlotMaterialsCreated()
    {
        if (runtimeMixMaterial != null) return;
        if (clothRenderer == null) return;

        var materials = clothRenderer.materials; // instantiates per-instance runtime copies
        if (materialSlotIndex < 0 || materialSlotIndex >= materials.Length)
        {
            Debug.LogWarning(
                $"{name}: materialSlotIndex {materialSlotIndex} is out of range for '{clothRenderer.name}' " +
                $"({materials.Length} slot(s)).", this);
            return;
        }

        runtimeMixMaterial = materials[materialSlotIndex];

        if (bakeShader == null)
            bakeShader = Shader.Find("Hidden/MixAlbedoBaker");

        if (bakeShader == null)
        {
            Debug.LogError($"{name}: cannot find shader 'Hidden/MixAlbedoBaker'. Is MixAlbedoBaker.shader in the project?", this);
            return;
        }

        bakerMaterial = new Material(bakeShader)
        {
            name = $"{name}_ClothAlbedoBaker_Runtime",
            hideFlags = HideFlags.DontSave
        };

        if (uts3MaterialTemplate != null)
        {
            runtimeUTSMaterial = new Material(uts3MaterialTemplate)
            {
                name = $"{uts3MaterialTemplate.name}_{name}_Runtime",
                hideFlags = HideFlags.DontSave
            };

            materials[materialSlotIndex] = runtimeUTSMaterial;
            clothRenderer.materials = materials;
        }
        else
        {
            Debug.LogWarning(
                $"{name}: no UTS3 Material Template assigned. Cloth will keep using its Mix material until one is set.",
                this);
        }
    }

    private int GetColorSlotCountSlotPath()
    {
        if (runtimeMixMaterial == null) return 0;

        if (runtimeMixMaterial.HasProperty("_Layer4") || runtimeMixMaterial.shader.name.Contains("Mix 5")) return 5;
        if (runtimeMixMaterial.HasProperty("_Layer3") || runtimeMixMaterial.shader.name.Contains("Mix 4")) return 4;
        if (runtimeMixMaterial.HasProperty("_Layer2") || runtimeMixMaterial.shader.name.Contains("Mix 3")) return 3;
        return 1;
    }

    private void ApplySavedColorsSlotPath(List<Color> colors)
    {
        if (runtimeMixMaterial == null || colors == null) return;

        int count = Mathf.Min(colors.Count, GetColorSlotCountSlotPath());
        for (int i = 0; i < count; i++)
        {
            string prop = LayerColorProperty(i);
            if (runtimeMixMaterial.HasProperty(prop))
                runtimeMixMaterial.SetColor(prop, colors[i]);
        }

        RebakeSlotPath();
    }

    private void RebakeSlotPath()
    {
        if (runtimeMixMaterial == null || bakerMaterial == null || runtimeUTSMaterial == null) return;

        Texture baseTexture = runtimeMixMaterial.HasProperty("_Base") ? runtimeMixMaterial.GetTexture("_Base") : null;
        int width = baseTexture != null ? baseTexture.width : 1024;
        int height = baseTexture != null ? baseTexture.height : 1024;

        if (bakedAlbedo == null || bakedAlbedo.width != width || bakedAlbedo.height != height)
        {
            ReleaseBakedAlbedo();

            RenderTextureReadWrite rw = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? RenderTextureReadWrite.sRGB
                : RenderTextureReadWrite.Default;

            bakedAlbedo = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, rw)
            {
                name = $"{name}_ClothBakedAlbedo",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                useMipMap = true,
                autoGenerateMips = false,
                hideFlags = HideFlags.DontSave
            };
            bakedAlbedo.Create();
        }

        CopyTexture("_Base");
        CopyTexture("_Layer1");
        CopyTexture("_Layer2");
        CopyTexture("_Layer3");
        CopyTexture("_Layer4");

        CopyColor("_BaseColor");
        CopyColor("_Layer1Color");
        CopyColor("_Layer2Color");
        CopyColor("_Layer3Color");
        CopyColor("_Layer4Color");

        bakerMaterial.SetFloat("_LayerCount", Mathf.Max(0, GetColorSlotCountSlotPath() - 1));
        Graphics.Blit(Texture2D.whiteTexture, bakedAlbedo, bakerMaterial, 0);
        if (bakedAlbedo.useMipMap) bakedAlbedo.GenerateMips();

        ApplyBakedTextureToUTSSlotPath();
    }

    private void CopyTexture(string property)
    {
        if (!runtimeMixMaterial.HasProperty(property) || !bakerMaterial.HasProperty(property)) return;

        Texture tex = runtimeMixMaterial.GetTexture(property);
        bakerMaterial.SetTexture(property, tex);
        bakerMaterial.SetTextureScale(property, runtimeMixMaterial.GetTextureScale(property));
        bakerMaterial.SetTextureOffset(property, runtimeMixMaterial.GetTextureOffset(property));
    }

    private void CopyColor(string property)
    {
        if (!bakerMaterial.HasProperty(property)) return;
        Color color = runtimeMixMaterial.HasProperty(property) ? runtimeMixMaterial.GetColor(property) : Color.white;
        bakerMaterial.SetColor(property, color);
    }

    private void ApplyBakedTextureToUTSSlotPath()
    {
        if (runtimeUTSMaterial == null || bakedAlbedo == null) return;

        if (runtimeUTSMaterial.HasProperty("_BaseMap")) runtimeUTSMaterial.SetTexture("_BaseMap", bakedAlbedo);
        if (runtimeUTSMaterial.HasProperty("_MainTex")) runtimeUTSMaterial.SetTexture("_MainTex", bakedAlbedo);

        // UTS3 three-color shading: reuse the baked albedo for the 1st/2nd shade maps too,
        // same as LayeredToonMaterialController does for armor/weapons.
        if (runtimeUTSMaterial.HasProperty("_1st_ShadeMap")) runtimeUTSMaterial.SetTexture("_1st_ShadeMap", bakedAlbedo);
        if (runtimeUTSMaterial.HasProperty("_2nd_ShadeMap")) runtimeUTSMaterial.SetTexture("_2nd_ShadeMap", bakedAlbedo);
        if (runtimeUTSMaterial.HasProperty("_Use_BaseAs1st")) runtimeUTSMaterial.SetFloat("_Use_BaseAs1st", 1f);
        if (runtimeUTSMaterial.HasProperty("_Use_1stAs2nd")) runtimeUTSMaterial.SetFloat("_Use_1stAs2nd", 1f);

        if (runtimeUTSMaterial.HasProperty("_BaseColor")) runtimeUTSMaterial.SetColor("_BaseColor", Color.white);
        if (runtimeUTSMaterial.HasProperty("_Color")) runtimeUTSMaterial.SetColor("_Color", Color.white);
    }

    private void ReleaseBakedAlbedo()
    {
        if (bakedAlbedo == null) return;
        if (bakedAlbedo.IsCreated()) bakedAlbedo.Release();
        DestroyRuntimeObject(bakedAlbedo);
        bakedAlbedo = null;
    }

    private void OnDestroy()
    {
        ReleaseBakedAlbedo();
        DestroyRuntimeObject(bakerMaterial);
        bakerMaterial = null;
        DestroyRuntimeObject(runtimeUTSMaterial);
        runtimeUTSMaterial = null;
        DestroyRuntimeObject(runtimeMixMaterial);
        runtimeMixMaterial = null;
    }

    private static void DestroyRuntimeObject(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
