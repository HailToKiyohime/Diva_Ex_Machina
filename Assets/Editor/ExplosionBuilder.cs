#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 建立爆炸特效 prefab（跟 Thruster Flame / Jump Dust Builder 同一套做法）。
///
/// 風格：介於風格化與寫實之間。
///   · 形狀是動畫式的：硬核心的閃光、尖銳的星芒、清楚邊界的火球團塊、俐落的衝擊環。
///   · 運動與消散是寫實的：火球先炸開再被空氣拖住、由白熱轉暗紅再熄滅，
///     煙慢慢上升膨脹、火花受重力拋落、餘燼飄散。
///
/// 開啟：Tools > Diva Ex Machina > Explosion Builder
///
/// 需要 {Output Folder}/Textures 裡的 EX_*.png。
/// 材質建在 {Output Folder}/Materials，prefab 存成 {Output Folder}/{Prefab Name}.prefab。
/// 重新建置會覆寫它們，所以可以換個名字建出不同大小 / 顏色的版本（例如 Explosion_Small、Explosion_Large）。
///
/// 圖層（時間軸）：
///   0.00s  Flash 閃光 + Spikes 星芒 + FireCore 白熱核心
///   0.00s  Fireball 火球炸開、Sparks 火花噴射、Debris 碎片拋出
///   0.02s  Shockwave 衝擊環擴散
///   0.06s  Smoke 煙從火球後方冒出，上升膨脹 1～2 秒
///   全程   Embers 餘燼飄散
/// </summary>
public class ExplosionBuilder : EditorWindow
{
    private string outputFolder = "Assets/VFX/Explosion";
    private string prefabName = "Explosion";

    private float radius = 2f;

    private Color coreColor = new Color(1f, 0.93f, 0.72f);
    private Color fireColor = new Color(1f, 0.48f, 0.14f);
    private Color smokeColor = new Color(0.27f, 0.25f, 0.24f);
    private Color sparkColor = new Color(1f, 0.72f, 0.32f);

    private bool includeSpikes = true;
    private bool includeShockwave = true;
    private bool includeSmoke = true;
    private bool includeDebris = true;
    private bool includeLight = true;
    private bool softParticles = true;

    private bool placePreview = true;

    [MenuItem("Tools/Diva Ex Machina/Explosion Builder")]
    private static void Open()
    {
        GetWindow<ExplosionBuilder>("Explosion");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        radius = EditorGUILayout.Slider(new GUIContent("Radius (m)",
            "火球的大約半徑（公尺）。其他圖層（煙、火花、衝擊環、燈光範圍）都依它等比例縮放。\n" +
            "參考：子彈命中 0.5～1、導彈 2～3、建築摧毀 4～6。"), radius, 0.2f, 15f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Colours (ColorPicker / EffectColorController can change these later)", EditorStyles.boldLabel);
        coreColor = EditorGUILayout.ColorField(new GUIContent("1. Core", "閃光、星芒、白熱核心，也決定閃光燈光的顏色。"), coreColor);
        fireColor = EditorGUILayout.ColorField(new GUIContent("2. Fire", "火球。會隨時間自動變暗成暗紅、熄滅。"), fireColor);
        smokeColor = EditorGUILayout.ColorField(new GUIContent("3. Smoke", "煙。"), smokeColor);
        sparkColor = EditorGUILayout.ColorField(new GUIContent("4. Sparks", "火花、餘燼、衝擊環。"), sparkColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
        includeSpikes = EditorGUILayout.ToggleLeft("Spikes 星芒（動畫感最強的一層，想偏寫實可以關掉）", includeSpikes);
        includeShockwave = EditorGUILayout.ToggleLeft("Shockwave 衝擊環", includeShockwave);
        includeSmoke = EditorGUILayout.ToggleLeft("Smoke 煙（大量同時爆炸時最吃效能的一層）", includeSmoke);
        includeDebris = EditorGUILayout.ToggleLeft("Debris 碎片", includeDebris);
        includeLight = EditorGUILayout.ToggleLeft("Flash Light 閃光燈光（照亮周圍；大量同時爆炸時建議關閉）", includeLight);
        softParticles = EditorGUILayout.ToggleLeft("Soft Particles（煙貼地處淡出，需要 URP Asset 開啟 Depth Texture）", softParticles);

        EditorGUILayout.Space();
        placePreview = EditorGUILayout.ToggleLeft("建好後在 Scene 視圖中央放一個預覽（選取它即可在 Scene 視圖播放）", placePreview);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Prefab", GUILayout.Height(32)))
            Build();

        EditorGUILayout.HelpBox(
            "給 Bullet 的 Hit Effect 用：直接拖進去即可，Bullet 會自己回收。\n" +
            "給只會生成、不會回收的程式用（例如 BuildingStats 的 Destroy Effect）：\n" +
            "在 prefab 的 ExplosionEffect 勾選 Despawn When Finished。\n" +
            "需要 Volume 裡有 Bloom，閃光和火花才會發光。",
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

        Texture2D texFlash = LoadTexture("EX_Flash.png");
        Texture2D texSpikes = LoadTexture("EX_Spikes.png");
        Texture2D texFire = LoadTexture("EX_FireSheet.png");
        Texture2D texSmoke = LoadTexture("EX_SmokeSheet.png");
        Texture2D texRing = LoadTexture("EX_Ring.png");
        Texture2D texSpark = LoadTexture("EX_Spark.png");
        Texture2D texDebris = LoadTexture("EX_DebrisSheet.png");

        if (texFlash == null || texSpikes == null || texFire == null || texSmoke == null ||
            texRing == null || texSpark == null || texDebris == null)
        {
            EditorUtility.DisplayDialog("Explosion Builder",
                $"Missing textures in {outputFolder}/Textures. See the Console for which EX_*.png is missing.", "OK");
            return;
        }

        // 發光的層用 Additive（HDR 強度 > 1 會吃到 Bloom）；有實體的層用 Alpha 混合
        Material matFlash = MakeMaterial("EX_Flash", texFlash, additive: true, intensity: 4f, soft: false);
        Material matSpikes = MakeMaterial("EX_Spikes", texSpikes, additive: true, intensity: 3f, soft: false);
        Material matCore = MakeMaterial("EX_FireCore", texFire, additive: true, intensity: 2.2f, soft: false);
        Material matFire = MakeMaterial("EX_Fire", texFire, additive: false, intensity: 1.8f, soft: false);
        Material matSmoke = MakeMaterial("EX_Smoke", texSmoke, additive: false, intensity: 1f, soft: softParticles);
        Material matRing = MakeMaterial("EX_Ring", texRing, additive: true, intensity: 1.6f, soft: false);
        Material matSpark = MakeMaterial("EX_Spark", texSpark, additive: true, intensity: 3f, soft: false);
        Material matEmber = MakeMaterial("EX_Ember", texSpark, additive: true, intensity: 2.5f, soft: false);
        Material matDebris = MakeMaterial("EX_Debris", texDebris, additive: false, intensity: 1f, soft: false);

        float R = radius;
        var root = new GameObject(prefabName);

        // ---- Flash：硬核心的白熱閃光（一瞬間） ----
        ParticleSystem flash = CreateLayer("Flash", root.transform, matFlash, local: true);
        {
            var main = flash.main;
            main.startLifetime = 0.14f;
            main.startSpeed = 0f;
            main.startSize = 2.2f * R;
            main.startColor = coreColor;
            main.maxParticles = 2;
            Burst(flash, 1, 1);
            NoShape(flash);
            SetSizeOverLife(flash, (0f, 0.5f), (0.25f, 1f), (1f, 1.15f));
            SetAlphaSmooth(flash, (1f, 0f), (0.8f, 0.4f), (0f, 1f));
            flash.GetComponent<ParticleSystemRenderer>().sortingFudge = -10f;   // 畫在最前面
        }

        // ---- Spikes：尖銳的星芒（異度神劍 2 那種一閃的光芒） ----
        ParticleSystem spikes = null;
        if (includeSpikes)
        {
            spikes = CreateLayer("Spikes", root.transform, matSpikes, local: true);
            var main = spikes.main;
            main.startLifetime = 0.12f;
            main.startSpeed = 0f;
            main.startSize = 3f * R;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = coreColor;
            main.maxParticles = 2;
            Burst(spikes, 1, 1);
            NoShape(spikes);
            SetSizeOverLife(spikes, (0f, 0.6f), (0.3f, 1f), (1f, 1.1f));
            SetAlphaSteps(spikes, (1f, 0f), (0.5f, 0.5f), (0f, 1f));
            spikes.GetComponent<ParticleSystemRenderer>().sortingFudge = -10f;
        }

        // ---- FireCore：火球中心的白熱團塊（很快就消失，露出後面的橙紅火球） ----
        ParticleSystem fireCore = CreateLayer("FireCore", root.transform, matCore, local: true);
        {
            var main = fireCore.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * R, 1.5f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f * R, 1.2f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = coreColor;
            main.maxParticles = 12;
            Burst(fireCore, 4, 6);
            SetSphere(fireCore, 0.2f * R);
            SetDrag(fireCore, 4f);
            SetSizeOverLife(fireCore, (0f, 0.6f), (0.4f, 1.1f), (1f, 0.9f));
            SetAlphaSmooth(fireCore, (1f, 0f), (0.9f, 0.5f), (0f, 1f));
            SetSheet(fireCore, 2, 2);
            fireCore.GetComponent<ParticleSystemRenderer>().sortingFudge = -4f;
        }

        // ---- Fireball：炸開的火球團塊。白熱 → 橙 → 暗紅 → 熄滅 ----
        ParticleSystem fireball = CreateLayer("Fireball", root.transform, matFire, local: true);
        {
            var main = fireball.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * R, 3f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f * R, 1.1f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = fireColor;
            main.maxParticles = 30;
            Burst(fireball, 10, 14);
            SetSphere(fireball, 0.25f * R);
            SetDrag(fireball, 5f);                       // 炸開之後很快被空氣拖住
            SetSizeOverLife(fireball, (0f, 0.5f), (0.3f, 1.2f), (1f, 1.5f));
            SetSpin(fireball, 1f);
            // 顏色隨時間變暗（乘在 Start Color 上，所以改色之後一樣會「燒完變暗」）
            SetColorAndAlpha(fireball,
                new[] { (1f, 0f), (0.9f, 0.25f), (0.42f, 0.65f), (0.18f, 1f) },
                new[] { (1f, 0f), (1f, 0.55f), (0f, 1f) },
                GradientMode.Blend);
            SetSheet(fireball, 2, 2);
            fireball.GetComponent<ParticleSystemRenderer>().sortingFudge = -2f;
        }

        // ---- Shockwave：面向鏡頭快速擴散的衝擊環 ----
        ParticleSystem ring = null;
        if (includeShockwave)
        {
            ring = CreateLayer("Shockwave", root.transform, matRing, local: true);
            var main = ring.main;
            main.startDelay = 0.02f;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = 1f * R;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = sparkColor;
            main.maxParticles = 2;
            Burst(ring, 1, 1);
            NoShape(ring);
            SetSizeOverLife(ring, (0f, 0.3f), (0.4f, 2.8f), (1f, 3.6f));
            SetAlphaSteps(ring, (0.9f, 0f), (0.5f, 0.5f), (0f, 0.85f));
            ring.GetComponent<ParticleSystemRenderer>().sortingFudge = -6f;
        }

        // ---- Sparks：高速噴射、受重力拋落的火花 ----
        ParticleSystem sparks = CreateLayer("Sparks", root.transform, matSpark, local: false);
        {
            var main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f * R, 12f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * R, 0.12f * R);
            main.startColor = sparkColor;
            main.gravityModifier = 1f;
            main.maxParticles = 60;
            Burst(sparks, 20, 30);
            SetSphere(sparks, 0.2f * R);
            SetDrag(sparks, 2.5f);
            SetAlphaSteps(sparks, (1f, 0f), (0.6f, 0.6f), (0f, 0.9f));

            var r = sparks.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 1f;
            r.velocityScale = 0.04f;
            r.cameraVelocityScale = 0f;
            r.sortingFudge = -3f;
        }

        // ---- Embers：慢慢飄散的餘燼 ----
        ParticleSystem embers = CreateLayer("Embers", root.transform, matEmber, local: false);
        {
            var main = embers.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f * R, 3f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f * R, 0.09f * R);
            main.startColor = sparkColor;
            main.gravityModifier = -0.05f;               // 熱空氣往上帶
            main.maxParticles = 30;
            Burst(embers, 8, 14);
            SetSphere(embers, 0.4f * R);
            SetDrag(embers, 1.5f);
            SetNoise(embers, 0.5f * R, 0.6f, 0.4f);
            SetAlphaSmooth(embers, (1f, 0f), (1f, 0.6f), (0f, 1f));
        }

        // ---- Smoke：火球後面冒出、慢慢上升膨脹的煙 ----
        ParticleSystem smoke = null;
        if (includeSmoke)
        {
            smoke = CreateLayer("Smoke", root.transform, matSmoke, local: false);
            var main = smoke.main;
            main.startDelay = 0.06f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * R, 1.4f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(1f * R, 1.6f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = smokeColor;
            main.gravityModifier = -0.06f;               // 慢慢往上飄
            main.maxParticles = 30;
            Burst(smoke, 8, 12);
            SetSphere(smoke, 0.4f * R);
            SetDrag(smoke, 2.5f);
            SetNoise(smoke, 0.3f * R, 0.5f, 0.3f);
            SetSizeOverLife(smoke, (0f, 0.5f), (0.35f, 1.3f), (1f, 2f));
            SetSpin(smoke, 0.3f);
            SetAlphaSmooth(smoke, (0f, 0f), (0.85f, 0.12f), (0.6f, 0.5f), (0f, 1f));
            SetSheet(smoke, 2, 2);
            smoke.GetComponent<ParticleSystemRenderer>().sortingFudge = 5f;   // 畫在火球後面
        }

        // ---- Debris：拋出去、受重力落下的碎片 ----
        if (includeDebris)
        {
            ParticleSystem debris = CreateLayer("Debris", root.transform, matDebris, local: false);
            var main = debris.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f * R, 8f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f * R, 0.18f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.25f, 0.22f, 0.2f);
            main.gravityModifier = 2f;
            main.maxParticles = 20;
            Burst(debris, 6, 10);
            SetSphere(debris, 0.2f * R);
            SetSpin(debris, 8f);
            SetAlphaSmooth(debris, (1f, 0f), (1f, 0.8f), (0f, 1f));
            SetSheet(debris, 2, 2);
        }

        // ---- Flash Light ----
        Light light = null;
        if (includeLight)
        {
            var lightGo = new GameObject("FlashLight");
            lightGo.transform.SetParent(root.transform, false);
            light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f * R;
            light.intensity = 8f;
            light.color = coreColor;
            light.shadows = LightShadows.None;
            light.enabled = false;   // ExplosionEffect 在啟用時點亮
        }

        // ---- Colour groups（EffectColorController / ColorPicker） ----
        var ecc = root.AddComponent<EffectColorController>();
        AssignColorGroups(ecc, new[]
        {
            Group(flash, spikes, fireCore),   // 1. Core
            Group(fireball),                  // 2. Fire
            Group(smoke),                     // 3. Smoke
            Group(sparks, embers, ring),      // 4. Sparks
        });

        // ---- Controller ----
        var fx = root.AddComponent<ExplosionEffect>();
        var so = new SerializedObject(fx);
        so.FindProperty("flashLight").objectReferenceValue = light;
        so.FindProperty("lightColorSource").objectReferenceValue = flash;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Save ----
        string prefabPath = $"{outputFolder}/{prefabName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
        DestroyImmediate(root);

        if (!success || prefab == null)
        {
            Debug.LogError($"ExplosionBuilder: failed to save {prefabPath}");
            return;
        }

        AssetDatabase.SaveAssets();

        if (placePreview) PlacePreview(prefab);
        else
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        Debug.Log($"ExplosionBuilder: built {prefabPath}");
    }

    /// <summary>在 Scene 視圖中央放一個預覽並選取它（選取時 Scene 視圖會播放粒子）。</summary>
    private static void PlacePreview(GameObject prefab)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null) instance.transform.position = view.pivot;

        instance.name = prefab.name + " (Preview)";
        Undo.RegisterCreatedObjectUndo(instance, "Place Explosion Preview");
        Selection.activeGameObject = instance;
    }

    // =====================================================================
    // Particle helpers
    // =====================================================================

    /// <summary>一次性圖層：不循環、啟用時自動播放、只在 0 秒 Burst。</summary>
    private static ParticleSystem CreateLayer(string name, Transform parent, Material material, bool local)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.duration = 0.1f;
        main.playOnAwake = true;
        main.prewarm = false;
        main.gravityModifier = 0f;
        main.startDelay = 0f;
        // 中心的閃光 / 火球用 Local：打在移動的船上時跟著船走。
        // 會飛散、會殘留的火花 / 煙 / 碎片用 World：留在空中，看起來才自然。
        main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.stopAction = ParticleSystemStopAction.None;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var e = ps.emission;
        e.enabled = true;
        e.rateOverTime = 0f;
        e.rateOverDistance = 0f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortMode = ParticleSystemSortMode.Distance;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return ps;
    }

    private static void Burst(ParticleSystem ps, int min, int max)
    {
        var e = ps.emission;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)min, (short)max) });
    }

    /// <summary>從原點發射（不用形狀）。</summary>
    private static void NoShape(ParticleSystem ps)
    {
        var s = ps.shape;
        s.enabled = false;
    }

    private static void SetSphere(ParticleSystem ps, float radius)
    {
        var s = ps.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Sphere;
        s.radius = radius;
        s.radiusThickness = 1f;
    }

    private static void SetDrag(ParticleSystem ps, float drag)
    {
        var lv = ps.limitVelocityOverLifetime;
        lv.enabled = true;
        lv.limit = 10000f;
        lv.drag = drag;
        lv.multiplyDragByParticleSize = false;
        lv.multiplyDragByParticleVelocity = false;
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

    private static void SetSizeOverLife(ParticleSystem ps, params (float, float)[] keys)   // (time, value)
    {
        var curve = new AnimationCurve();
        foreach (var k in keys) curve.AddKey(k.Item1, k.Item2);
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void SetSpin(ParticleSystem ps, float radiansPerSecond)
    {
        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
    }

    /// <summary>cols × rows 的貼圖表，每顆粒子隨機挑一格（不播放動畫）。</summary>
    private static void SetSheet(ParticleSystem ps, int cols, int rows)
    {
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Grid;
        tsa.numTilesX = cols;
        tsa.numTilesY = rows;
        tsa.animation = ParticleSystemAnimationType.WholeSheet;
        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        tsa.cycleCount = 1;
    }

    /// <summary>硬切換的透明度（動畫式）。keys = (alpha, time)。</summary>
    private static void SetAlphaSteps(ParticleSystem ps, params (float, float)[] keys)   // (alpha, time)
    {
        SetColorAndAlpha(ps, new[] { (1f, 0f), (1f, 1f) }, keys, GradientMode.Fixed);
    }

    /// <summary>平滑的透明度（寫實）。keys = (alpha, time)。</summary>
    private static void SetAlphaSmooth(ParticleSystem ps, params (float, float)[] keys)   // (alpha, time)
    {
        SetColorAndAlpha(ps, new[] { (1f, 0f), (1f, 1f) }, keys, GradientMode.Blend);
    }

    /// <summary>
    /// Color over Lifetime。brightnessKeys = (亮度, time)，乘在 Start Color 上；alphaKeys = (alpha, time)。
    /// 只用灰階亮度，不寫死顏色 —— 這樣 ColorPicker 改了 Start Color 之後，變暗的過程一樣成立。
    /// </summary>
    private static void SetColorAndAlpha(ParticleSystem ps, (float, float)[] brightnessKeys,
                                         (float, float)[] alphaKeys, GradientMode mode)
    {
        var colorKeys = new GradientColorKey[brightnessKeys.Length];
        for (int i = 0; i < brightnessKeys.Length; i++)
        {
            float b = brightnessKeys[i].Item1;
            colorKeys[i] = new GradientColorKey(new Color(b, b, b), brightnessKeys[i].Item2);
        }

        var aKeys = new GradientAlphaKey[alphaKeys.Length];
        for (int i = 0; i < alphaKeys.Length; i++)
            aKeys[i] = new GradientAlphaKey(alphaKeys[i].Item1, alphaKeys[i].Item2);

        var g = new Gradient { mode = mode };
        g.SetKeys(colorKeys, aKeys);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    /// <summary>把存在的圖層組成一組（關掉的圖層是 null，略過）。</summary>
    private static ParticleSystem[] Group(params ParticleSystem[] layers)
    {
        var list = new List<ParticleSystem>();
        foreach (var ps in layers)
            if (ps != null) list.Add(ps);
        return list.ToArray();
    }

    private static void AssignColorGroups(EffectColorController ecc, ParticleSystem[][] groups)
    {
        // 空的組（例如關掉 Smoke）不放進去，避免 ColorPicker 出現一個沒作用的顏色
        var valid = new List<ParticleSystem[]>();
        foreach (var g in groups)
            if (g != null && g.Length > 0) valid.Add(g);

        var so = new SerializedObject(ecc);
        var arr = so.FindProperty("particleSystems");
        arr.arraySize = valid.Count;

        for (int i = 0; i < valid.Count; i++)
        {
            var list = arr.GetArrayElementAtIndex(i).FindPropertyRelative("particleSystem");
            list.arraySize = valid[i].Length;
            for (int j = 0; j < valid[i].Length; j++)
                list.GetArrayElementAtIndex(j).objectReferenceValue = valid[i][j];
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
            Debug.LogError($"ExplosionBuilder: texture not found at {path}");
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

    /// <summary>URP Particles/Unlit。additive = 發光（加法混合）；否則為一般半透明（Alpha 混合）。</summary>
    private Material MakeMaterial(string name, Texture texture, bool additive, float intensity, bool soft)
    {
        string path = $"{outputFolder}/Materials/{name}.mat";

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("ExplosionBuilder: URP Particles/Unlit not found, falling back to built-in particles.");
            shader = Shader.Find(additive ? "Legacy Shaders/Particles/Additive" : "Legacy Shaders/Particles/Alpha Blended");
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

        // SetVector（不是 SetColor）：HDR 強度不會被再做一次 Gamma 轉換。> 1 會吃到 Bloom。
        if (mat.HasProperty("_BaseColor")) mat.SetVector("_BaseColor", new Vector4(intensity, intensity, intensity, 1f));

        SetFloatIfExists(mat, "_Surface", 1f);
        SetFloatIfExists(mat, "_Blend", additive ? 2f : 0f);
        SetFloatIfExists(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloatIfExists(mat, "_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfExists(mat, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloatIfExists(mat, "_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfExists(mat, "_ZWrite", 0f);
        SetFloatIfExists(mat, "_Cull", (float)CullMode.Off);
        SetFloatIfExists(mat, "_ColorMode", 0f);

        const float near = 0f, far = 0.6f;
        SetFloatIfExists(mat, "_SoftParticlesEnabled", soft ? 1f : 0f);
        SetFloatIfExists(mat, "_SoftParticlesNearFadeDistance", near);
        SetFloatIfExists(mat, "_SoftParticlesFarFadeDistance", far);
        if (mat.HasProperty("_SoftParticleFadeParams"))
            mat.SetVector("_SoftParticleFadeParams", new Vector4(near, 1f / (far - near), 0f, 0f));
        if (soft) mat.EnableKeyword("_SOFTPARTICLES_ON");
        else mat.DisableKeyword("_SOFTPARTICLES_ON");

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
