#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds a layered, Xenoblade-2-flavoured thruster flame prefab:
/// clean anime shapes (posterized bands, hard-edged core, shock rings)
/// mixed with realistic motion (a continuous world-space ribbon trail,
/// noise turbulence, dragging sparks, soft outer flame).
///
/// The jet layers (core/flame/outer/rings) simulate in LOCAL space so they
/// stay one continuous shape at any flight speed. Only the ribbon trail and
/// sparks live in world space — a ribbon is a connected strip, so it stays a
/// line instead of breaking into dots when the player moves fast.
///
/// Already have a customised prefab? Select it in the Project window and run
/// Tools > Diva Ex Machina > Upgrade Selected Thruster Flame Prefabs — it
/// applies the same fix in place without touching your wrapper objects.
///
/// Open: Tools > Diva Ex Machina > Thruster Flame Builder
///
/// Expects the five TF_*.png textures in {Output Folder}/Textures.
/// Creates URP additive particle materials in {Output Folder}/Materials and the
/// prefab at {Output Folder}/{Prefab Name}.prefab. Re-running overwrites them,
/// so it's safe to tweak the numbers below and rebuild.
///
/// The flame shoots along the prefab's local +Z (blue arrow): place it at the
/// nozzle with +Z pointing away from the thruster.
/// </summary>
public class ThrusterFlameBuilder : EditorWindow
{
    private string outputFolder = "Assets/VFX/ThrusterFlame";
    private string prefabName = "ThrusterFlame";

    [Tooltip("Visible flame length at full throttle, in metres.")]
    private float flameLength = 0.8f;
    [Tooltip("Nozzle radius in metres. Drives the thickness of every layer.")]
    private float nozzleRadius = 0.06f;

    private Color coreColor = new Color(0.85f, 0.95f, 1f);
    private Color primaryColor = new Color(0.25f, 0.65f, 1f);
    private Color secondaryColor = new Color(0.22f, 0.3f, 1f);
    private Color sparkColor = new Color(0.65f, 0.92f, 1f);

    private bool attachToSelection = true;

    [MenuItem("Tools/Diva Ex Machina/Thruster Flame Builder")]
    private static void Open()
    {
        GetWindow<ThrusterFlameBuilder>("Thruster Flame");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        flameLength = EditorGUILayout.Slider("Flame Length (m)", flameLength, 0.1f, 5f);
        nozzleRadius = EditorGUILayout.Slider("Nozzle Radius (m)", nozzleRadius, 0.01f, 0.5f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Default Colours (ColorPicker can change these later)", EditorStyles.boldLabel);
        coreColor = EditorGUILayout.ColorField("1. Core", coreColor);
        primaryColor = EditorGUILayout.ColorField("2. Primary Flame", primaryColor);
        secondaryColor = EditorGUILayout.ColorField("3. Outer Flame", secondaryColor);
        sparkColor = EditorGUILayout.ColorField("4. Sparks", sparkColor);

        EditorGUILayout.Space();
        attachToSelection = EditorGUILayout.ToggleLeft(
            "Also place an instance under the selected scene object (e.g. a nozzle transform)",
            attachToSelection);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Prefab", GUILayout.Height(32)))
            Build();

        EditorGUILayout.HelpBox(
            "Needs URP Bloom in your Volume profile to glow properly.\n" +
            "Flame points along the prefab's local +Z (blue arrow).",
            MessageType.Info);
    }

    // =====================================================================
    // Build
    // =====================================================================

    private void Build()
    {
        AssetDatabase.Refresh();
        EnsureFolder(outputFolder);
        EnsureFolder($"{outputFolder}/Materials");

        var texBanded = LoadTexture("TF_BandedOrb.png");
        var texCore = LoadTexture("TF_HotCore.png");
        var texGlow = LoadTexture("TF_SoftGlow.png");
        var texSpark = LoadTexture("TF_Spark.png");
        var texRing = LoadTexture("TF_Ring.png");
        var texTrail = LoadTexture("TF_TrailStrip.png");

        if (texBanded == null || texCore == null || texGlow == null || texSpark == null || texRing == null || texTrail == null)
        {
            EditorUtility.DisplayDialog("Thruster Flame Builder",
                $"Missing textures in {outputFolder}/Textures. Copy the six TF_*.png files there first.", "OK");
            return;
        }

        // Linear HDR intensities — above 1 feeds URP Bloom.
        var matCore = MakeAdditiveMaterial("TF_Core", texCore, 3.0f);
        var matFlame = MakeAdditiveMaterial("TF_Flame", texCore, 1.8f);
        var matOuter = MakeAdditiveMaterial("TF_Outer", texGlow, 1.3f);
        var matGlow = MakeAdditiveMaterial("TF_NozzleGlow", texBanded, 1.3f);
        var matSpark = MakeAdditiveMaterial("TF_Spark", texSpark, 2.5f);
        var matRing = MakeAdditiveMaterial("TF_Ring", texRing, 1.6f);
        var matTrail = MakeAdditiveMaterial("TF_Trail", texTrail, 1.4f);

        float L = flameLength;
        float R = nozzleRadius;

        // ---- Core (root): short white-hot jet, glued to the nozzle ----
        var core = CreateLayer(prefabName, null, matCore);
        {
            var main = core.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.08f;
            main.startSpeed = 0.45f * L / 0.08f;
            main.startSize = R * 1.4f;
            main.startColor = coreColor;
            main.maxParticles = 60;

            SetRate(core, 70f);
            SetCone(core, 2f, R * 0.2f);
            SetStretch(core, lengthScale: 3f, velocityScale: 0f);
            SetSizeOverLife(core, 1f, 0.55f);
            SetAlphaSteps(core, (1f, 0.55f), (0.5f, 0.85f), (0f, 1f));
        }

        // ---- Flame: main tongue. Local space = stays one continuous jet while flying ----
        var flame = CreateLayer("Flame", core.transform, matFlame);
        {
            var main = flame.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.17f);
            main.startSpeed = L / 0.16f;
            main.startSize = R * 1.9f;
            main.startColor = primaryColor;
            main.maxParticles = 160;

            // Dense + tight on purpose: sparse or wide spawns read as bubbles, not a jet.
            SetRate(flame, 160f);
            SetCone(flame, 1.5f, R * 0.2f);
            SetStretch(flame, lengthScale: 2.5f, velocityScale: 0.02f);
            SetSizeOverLife(flame, 1f, 0.5f);
            SetAlphaSteps(flame, (1f, 0.35f), (0.65f, 0.7f), (0.3f, 0.9f), (0f, 1f));
            SetNoise(flame, strength: 0.25f, frequency: 2.5f, scroll: 1.5f);
        }

        // ---- Outer: soft, wide, smooth — the "realistic" half of the mix ----
        var outer = CreateLayer("Outer", core.transform, matOuter);
        {
            var main = outer.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.3f);
            main.startSpeed = 0.9f * L / 0.26f;
            main.startSize = R * 3.0f;
            main.startColor = secondaryColor;
            main.maxParticles = 100;

            SetRate(outer, 70f);
            SetCone(outer, 5f, R * 0.6f);
            SetStretch(outer, lengthScale: 2f, velocityScale: 0.02f);
            SetSizeOverLife(outer, 0.7f, 1.2f);
            SetAlphaSmooth(outer, (0f, 0f), (0.5f, 0.1f), (0.3f, 0.6f), (0f, 1f));
            SetNoise(outer, strength: 0.6f, frequency: 1.5f, scroll: 1f);
        }

        // ---- Nozzle glow: flickering banded disc (flat anime lens-glow) ----
        var glow = CreateLayer("NozzleGlow", core.transform, matGlow);
        {
            var main = glow.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.05f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(R * 3.5f, R * 4.5f);
            main.startColor = primaryColor;
            main.maxParticles = 10;

            SetRate(glow, 45f);
            var shape = glow.shape;
            shape.enabled = false;

            var r = glow.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            SetAlphaSmooth(glow, (0.45f, 0f), (0.45f, 0.7f), (0f, 1f));
        }

        // ---- Sparks: thin fast streaks that drag and drift ----
        var sparks = CreateLayer("Sparks", core.transform, matSpark);
        {
            var main = sparks.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f * L, 7f * L);
            main.startSize = new ParticleSystem.MinMaxCurve(R * 0.18f, R * 0.3f);
            main.startColor = sparkColor;
            main.maxParticles = 80;

            SetRate(sparks, 30f);
            SetCone(sparks, 10f, R * 0.6f);
            SetStretch(sparks, lengthScale: 1f, velocityScale: 0.06f);
            SetAlphaSteps(sparks, (1f, 0.6f), (0.5f, 0.85f), (0f, 1f));
            SetNoise(sparks, strength: 1.2f, frequency: 3f, scroll: 2f);

            var lv = sparks.limitVelocityOverLifetime;
            lv.enabled = true;
            lv.limit = 1000f;
            lv.drag = 2f;
            lv.multiplyDragByParticleSize = false;
            lv.multiplyDragByParticleVelocity = false;
        }

        // ---- Shock rings: anime flair at high throttle / on Boost() ----
        var rings = CreateLayer("ShockRings", core.transform, matRing);
        {
            var main = rings.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.2f;
            main.startSpeed = 0.55f * L / 0.2f;
            main.startSize = R * 2.2f;
            main.startColor = primaryColor;
            main.maxParticles = 20;

            SetRate(rings, 0f); // ThrusterFlameController drives this
            SetCone(rings, 0f, 0.0001f);
            SetSizeOverLife(rings, 0.8f, 1.8f);
            SetAlphaSteps(rings, (0.9f, 0.4f), (0.45f, 0.8f), (0f, 1f));

            var r = rings.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.Local; // faces along +Z, like a ring around the jet
        }

        // ---- Motion trail: world-space ribbon, the "realistic" streak left behind in flight ----
        var trail = CreateLayer("MotionTrail", core.transform, matTrail);
        ConfigureMotionTrail(trail, matTrail, R, primaryColor);

        // ---- Colour groups for EffectColorController / ColorPicker ----
        var ecc = core.gameObject.AddComponent<EffectColorController>();
        AssignColorGroups(ecc, new[]
        {
            new[] { core },                 // 1. Core
            new[] { flame, glow, rings, trail }, // 2. Primary
            new[] { outer },                // 3. Outer
            new[] { sparks },               // 4. Sparks
        });

        // ---- Throttle / Boost controller ----
        var ctrl = core.gameObject.AddComponent<ThrusterFlameController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("core").objectReferenceValue = core;
        so.FindProperty("flame").objectReferenceValue = flame;
        so.FindProperty("outer").objectReferenceValue = outer;
        so.FindProperty("nozzleGlow").objectReferenceValue = glow;
        so.FindProperty("sparks").objectReferenceValue = sparks;
        so.FindProperty("rings").objectReferenceValue = rings;
        so.FindProperty("trail").objectReferenceValue = trail;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Save ----
        string prefabPath = $"{outputFolder}/{prefabName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(core.gameObject, prefabPath, out bool success);
        DestroyImmediate(core.gameObject);

        if (!success || prefab == null)
        {
            Debug.LogError($"ThrusterFlameBuilder: failed to save {prefabPath}");
            return;
        }

        AssetDatabase.SaveAssets();

        bool selectionInPrefabStage = Selection.activeTransform != null &&
            UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(Selection.activeTransform.gameObject) != null;

        if (attachToSelection && selectionInPrefabStage)
        {
            // Placing an instance inside an open prefab would nest a second flame into it.
            Debug.LogWarning("ThrusterFlameBuilder: selection is inside Prefab Mode, so no instance was placed. " +
                             "Select the nozzle in the scene instead.");
        }
        else if (attachToSelection && Selection.activeTransform != null &&
            !EditorUtility.IsPersistent(Selection.activeTransform))
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Selection.activeTransform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(instance, "Place Thruster Flame");
        }

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"ThrusterFlameBuilder: built {prefabPath}");
    }

    // =====================================================================
    // Motion trail (shared by Build and Upgrade)
    // =====================================================================

    private static void ConfigureMotionTrail(ParticleSystem ps, Material trailMaterial, float R, Color color)
    {
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 0.25f;          // streak length = flight speed x lifetime
        main.startSpeed = 0.5f;              // slight drift away from the nozzle
        main.startSize = 0.0001f;            // the particles themselves are invisible; only the ribbon draws
        main.startColor = color;
        main.maxParticles = 200;

        var e = ps.emission;
        e.enabled = true;
        e.rateOverTime = 20f;                // keeps the ribbon alive while hovering
        e.rateOverDistance = 12f;            // adds points as the player moves => no gaps at speed

        var shape = ps.shape;
        shape.enabled = false;               // emit on the jet axis

        // Anime banding along the streak (particle colour feeds the ribbon).
        SetAlphaSteps(ps, (1f, 0.3f), (0.6f, 0.65f), (0.25f, 0.9f), (0f, 1f));

        var t = ps.trails;
        t.enabled = true;
        t.mode = ParticleSystemTrailMode.Ribbon;
        t.ribbonCount = 1;
        t.attachRibbonsToTransform = true;   // ribbon head stays glued to the nozzle
        t.textureMode = ParticleSystemTrailTextureMode.Stretch;
        t.sizeAffectsWidth = false;
        t.sizeAffectsLifetime = false;
        t.inheritParticleColor = true;
        t.dieWithParticles = true;
        t.colorOverTrail = Color.white;
        t.widthOverTrail = new ParticleSystem.MinMaxCurve(R * 2.4f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = trailMaterial;
        r.trailMaterial = trailMaterial;
    }

    // =====================================================================
    // Upgrade existing prefabs in place
    // =====================================================================

    [MenuItem("Tools/Diva Ex Machina/Upgrade Selected Thruster Flame Prefabs")]
    private static void UpgradeSelectedPrefabs()
    {
        var builder = CreateInstance<ThrusterFlameBuilder>();
        try
        {
            var texTrail = builder.LoadTexture("TF_TrailStrip.png");
            if (texTrail == null)
            {
                EditorUtility.DisplayDialog("Thruster Flame Upgrade",
                    $"Missing {builder.outputFolder}/Textures/TF_TrailStrip.png.", "OK");
                return;
            }
            var matTrail = builder.MakeAdditiveMaterial("TF_Trail", texTrail, 1.4f);

            int prefabs = 0, controllers = 0;
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var ctrls = root.GetComponentsInChildren<ThrusterFlameController>(true);
                    foreach (var c in ctrls)
                    {
                        if (c.transform.parent != null &&
                            c.transform.parent.GetComponentInParent<ThrusterFlameController>(true) != null)
                        {
                            Debug.LogWarning(
                                $"Thruster Flame Upgrade: '{path}' has a ThrusterFlame nested inside another one " +
                                $"('{c.name}' under '{c.transform.parent.name}'). That doubles the flame — you probably want to delete the inner copy.");
                        }

                        UpgradeController(c, matTrail);
                        controllers++;
                    }

                    if (ctrls.Length > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabs++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Thruster Flame Upgrade: updated {controllers} flame(s) in {prefabs} prefab(s).");
            if (prefabs == 0)
                EditorUtility.DisplayDialog("Thruster Flame Upgrade",
                    "Select one or more thruster flame prefabs in the Project window first.", "OK");
        }
        finally
        {
            DestroyImmediate(builder);
        }
    }

    private static void UpgradeController(ThrusterFlameController ctrl, Material matTrail)
    {
        var so = new SerializedObject(ctrl);

        // 1) Jet layers to local space so they stay continuous in flight.
        foreach (var prop in new[] { "core", "flame", "outer", "rings" })
        {
            var ps = so.FindProperty(prop).objectReferenceValue as ParticleSystem;
            if (ps == null) continue;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        // 2) Add the ribbon trail if this flame doesn't have one yet.
        var trailProp = so.FindProperty("trail");
        if (trailProp.objectReferenceValue == null)
        {
            var flame = so.FindProperty("flame").objectReferenceValue as ParticleSystem;
            float R = flame != null ? flame.main.startSizeMultiplier / 1.9f : 0.06f;
            Color color = flame != null ? flame.main.startColor.color : new Color(0.25f, 0.65f, 1f);

            var trail = CreateLayer("MotionTrail", ctrl.transform, matTrail);
            ConfigureMotionTrail(trail, matTrail, R, color);
            trailProp.objectReferenceValue = trail;

            // Put it in the Primary colour group so ColorPicker recolours it with the flame.
            var ecc = ctrl.GetComponent<EffectColorController>();
            if (ecc != null)
            {
                var eso = new SerializedObject(ecc);
                var groups = eso.FindProperty("particleSystems");
                if (groups != null && groups.arraySize > 1)
                {
                    var list = groups.GetArrayElementAtIndex(1).FindPropertyRelative("particleSystem");
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = trail;
                    eso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // Particle helpers
    // =====================================================================

    private static ParticleSystem CreateLayer(string name, Transform parent, Material material)
    {
        var go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.duration = 1f;
        main.playOnAwake = true;
        main.prewarm = false;
        main.gravityModifier = 0f;
        main.startRotation = 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return ps;
    }

    private static void SetRate(ParticleSystem ps, float rate)
    {
        var e = ps.emission;
        e.enabled = true;
        e.rateOverTime = rate;
    }

    private static void SetCone(ParticleSystem ps, float angle, float radius)
    {
        var s = ps.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Cone;
        s.angle = angle;
        s.radius = radius;
        s.radiusThickness = 1f;
    }

    private static void SetStretch(ParticleSystem ps, float lengthScale, float velocityScale)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = lengthScale;
        r.velocityScale = velocityScale;
        r.cameraVelocityScale = 0f;
    }

    private static void SetSizeOverLife(ParticleSystem ps, float start, float end)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
    }

    private static void SetNoise(ParticleSystem ps, float strength, float frequency, float scroll)
    {
        var n = ps.noise;
        n.enabled = true;
        n.strength = strength;
        n.frequency = frequency;
        n.scrollSpeed = scroll;
        n.damping = true;
        n.quality = ParticleSystemNoiseQuality.Medium;
    }

    /// <summary>Hard-stepped alpha (GradientMode.Fixed) — the posterized anime look. Keys are (alpha, time).</summary>
    private static void SetAlphaSteps(ParticleSystem ps, params (float alpha, float time)[] keys)
    {
        SetAlpha(ps, GradientMode.Fixed, keys);
    }

    /// <summary>Smooth alpha — used on the soft, realistic layers. Keys are (alpha, time).</summary>
    private static void SetAlphaSmooth(ParticleSystem ps, params (float alpha, float time)[] keys)
    {
        SetAlpha(ps, GradientMode.Blend, keys);
    }

    private static void SetAlpha(ParticleSystem ps, GradientMode mode, (float alpha, float time)[] keys)
    {
        var alphaKeys = new GradientAlphaKey[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            alphaKeys[i] = new GradientAlphaKey(keys[i].alpha, keys[i].time);

        var g = new Gradient { mode = mode };
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    private static void AssignColorGroups(EffectColorController ecc, ParticleSystem[][] groups)
    {
        var so = new SerializedObject(ecc);
        var arr = so.FindProperty("particleSystems");
        arr.arraySize = groups.Length;

        for (int i = 0; i < groups.Length; i++)
        {
            var list = arr.GetArrayElementAtIndex(i).FindPropertyRelative("particleSystem");
            list.arraySize = groups[i].Length;
            for (int j = 0; j < groups[i].Length; j++)
                list.GetArrayElementAtIndex(j).objectReferenceValue = groups[i][j];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // Asset helpers
    // =====================================================================

    private Texture2D LoadTexture(string fileName)
    {
        string path = $"{outputFolder}/Textures/{fileName}";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"ThrusterFlameBuilder: texture not found at {path}");
            return null;
        }

        bool dirty =
            importer.textureType != TextureImporterType.Default ||
            !importer.alphaIsTransparency ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            !importer.mipmapEnabled ||
            !importer.sRGBTexture ||
            importer.alphaSource != TextureImporterAlphaSource.FromInput;

        if (dirty)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private Material MakeAdditiveMaterial(string name, Texture texture, float intensity)
    {
        string path = $"{outputFolder}/Materials/{name}.mat";

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("ThrusterFlameBuilder: URP Particles/Unlit not found, falling back to built-in particles.");
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        SetTextureIfExists(mat, "_BaseMap", texture);
        SetTextureIfExists(mat, "_MainTex", texture);

        // SetVector (not SetColor) so the HDR intensity isn't gamma-converted a second time.
        var tint = new Vector4(intensity, intensity, intensity, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetVector("_BaseColor", tint);
        if (mat.HasProperty("_TintColor")) mat.SetVector("_TintColor", new Vector4(0.5f, 0.5f, 0.5f, 0.5f) * intensity);

        // URP transparent + additive (same values the URP material inspector writes).
        SetFloatIfExists(mat, "_Surface", 1f);
        SetFloatIfExists(mat, "_Blend", 2f);
        SetFloatIfExists(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloatIfExists(mat, "_DstBlend", (float)BlendMode.One);
        SetFloatIfExists(mat, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloatIfExists(mat, "_DstBlendAlpha", (float)BlendMode.One);
        SetFloatIfExists(mat, "_ZWrite", 0f);
        SetFloatIfExists(mat, "_Cull", (float)CullMode.Off);
        SetFloatIfExists(mat, "_ColorMode", 0f);          // multiply by particle colour
        SetFloatIfExists(mat, "_SoftParticlesEnabled", 0f);

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHAMODULATE_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetShaderPassEnabled("ShadowCaster", false);
        mat.SetShaderPassEnabled("DepthOnly", false);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SetTextureIfExists(Material m, string prop, Texture t)
    {
        if (m.HasProperty(prop)) m.SetTexture(prop, t);
    }

    private static void SetFloatIfExists(Material m, string prop, float v)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, v);
    }

    private static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
