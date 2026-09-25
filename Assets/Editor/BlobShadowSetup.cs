#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One-click setup: Tools > Diva Ex Machina > Add Blob Shadow To Player.
/// Open the scene (e.g. Test.unity) first. Finds the object tagged "Player",
/// creates the material, and adds a "BlobShadow" child with everything wired.
/// Running it again just refreshes the existing shadow instead of adding another.
/// </summary>
public static class BlobShadowSetup
{
    private const string Folder = "Assets/VFX/BlobShadow";
    private const string TexturePath = Folder + "/BlobShadow.png";
    private const string MaterialPath = Folder + "/BlobShadow.mat";
    private const string ChildName = "BlobShadow";

    [MenuItem("Tools/Diva Ex Machina/Add Blob Shadow To Player")]
    private static void AddToPlayer()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Blob Shadow",
                "No GameObject tagged 'Player' in the open scene. Open Test.unity first.", "OK");
            return;
        }

        var material = GetOrCreateMaterial();
        if (material == null) return;

        // Reuse an existing shadow so running this twice doesn't stack two.
        var existing = player.transform.Find(ChildName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
            Undo.RecordObject(go, "Update Blob Shadow");
        }
        else
        {
            go = new GameObject(ChildName);
            Undo.RegisterCreatedObjectUndo(go, "Add Blob Shadow");
            go.transform.SetParent(player.transform, false);
        }

        // Note: no '??' here — Unity's fake-null objects break the null-coalescing operator.
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = Undo.AddComponent<MeshFilter>(go);
        mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = Undo.AddComponent<MeshRenderer>(go);
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        var blob = go.GetComponent<BlobShadow>();
        if (blob == null) blob = Undo.AddComponent<BlobShadow>(go);
        var so = new SerializedObject(blob);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.FindProperty("groundMask").intValue = ResolveGroundMask(player);
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log($"Blob Shadow added under '{player.name}' in scene '{player.scene.name}'. Save the scene to keep it.");
    }

    /// <summary>Same surfaces the player stands on (PlayerMovement.whatIsGround), plus obstacles/fortifications.</summary>
    private static int ResolveGroundMask(GameObject player)
    {
        int mask = 0;

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            var prop = new SerializedObject(movement).FindProperty("whatIsGround");
            if (prop != null) mask = prop.intValue;
        }

        if (mask == 0)
            mask = LayerMask.GetMask("Default", "Ground", "Mobile Platform");

        mask |= LayerMask.GetMask("Obstacle", "Defence Fortifications");
        mask &= ~LayerMask.GetMask("Player", "Ignore Raycast", "Bullet", "Detect Range");
        return mask;
    }

    private static Material GetOrCreateMaterial()
    {
        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Blob Shadow", $"Missing texture: {TexturePath}", "OK");
            return null;
        }

        if (!importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Blob Shadow", "Shader 'Universal Render Pipeline/Unlit' not found.", "OK");
            return null;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "BlobShadow" };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        mat.shader = shader;

        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.1f, 0.55f)); // BlobShadow overrides per frame

        // Transparent, alpha-blended, no depth write, double-sided.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", (float)CullMode.Off);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        // Draw before other transparents (thruster flames, etc.) so they layer on top of it.
        mat.renderQueue = (int)RenderQueue.Transparent - 50;
        mat.SetShaderPassEnabled("ShadowCaster", false);
        mat.SetShaderPassEnabled("DepthOnly", false);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
#endif
