#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 建立爆炸特效 prefab（跟 Thruster Flame / Jump Dust Builder 同一套做法）。
///
/// 開啟：Tools > Diva Ex Machina > Explosion Builder
///
/// 類型（視窗最上面的 Type）：
///   · Standard  標準爆炸：敵人被摧毀時用的那一個。內容跟加入類型之前完全一樣。
///   · Fire      火焰（ExplosionDamage）：紅橙色火球 + 持續燃燒的火舌 + 大量餘燼。
///   · Lightning 閃電（EnergyDamage）：紫色電漿球 + 放射狀電弧 + 殘留的劈啪電弧，燈光會閃爍。
///   · Cryo      低溫（ColdDamage）：藍白色寒氣 + 炸開的冰晶尖刺 + 冰片 + 飄落的冰晶微塵。
/// 切換類型會套用該類型的預設顏色與 prefab 名稱，顏色之後仍然可以自訂。
///
/// 風格：介於風格化與寫實之間。
///   · 形狀是動畫式的：硬核心的閃光、尖銳的星芒 / 電弧 / 冰刺、俐落的衝擊環。
///   · 運動與消散是寫實的：先炸開再被空氣拖住、受重力拋落、慢慢上升或下沉散開。
///
/// 需要 {Output Folder}/Textures 裡的 EX_*.png（所有類型），以及
/// EXL_*.png（Lightning）、EXC_*.png（Cryo）。
/// 材質依類型分開建在 {Output Folder}/Materials（EX_ / EXF_ / EXL_ / EXC_ 開頭），
/// 重建某一個類型不會動到其他類型的材質。
/// prefab 存成 {Output Folder}/{Prefab Name}.prefab，重新建置會覆寫。
/// </summary>
public class ExplosionBuilder : EditorWindow
{
    private enum ExplosionType { Standard, Fire, Lightning, Cryo }

    private string outputFolder = "Assets/VFX/Explosion";
    private string prefabName = "Explosion";

    private ExplosionType type = ExplosionType.Standard;

    private float radius = 2f;

    // 每個類型的顏色欄位數量和意義不同（見 ColorLabels）
    private Color[] colors;

    // 三個「可選圖層」開關。各類型對應的圖層不同（見 ToggleLabels）：
    //   Standard / Fire : Spikes 星芒 / Smoke 煙 / Debris 碎片
    //   Lightning       : Spikes 星芒 / Static 殘留電弧 / Ion Haze 電離微光
    //   Cryo            : Glints 冰晶閃光 / Mist 寒氣 / Shards 冰片
    private bool includeSpikes = true;
    private bool includeSmoke = true;
    private bool includeDebris = true;

    private bool includeShockwave = true;
    private bool includeLight = true;
    private bool softParticles = true;

    private bool placePreview = true;

    [MenuItem("Tools/Diva Ex Machina/Explosion Builder")]
    private static void Open()
    {
        GetWindow<ExplosionBuilder>("Explosion");
    }

    // =====================================================================
    // 類型預設
    // =====================================================================

    private static string DefaultName(ExplosionType t)
    {
        switch (t)
        {
            case ExplosionType.Fire: return "Explosion_Fire";
            case ExplosionType.Lightning: return "Explosion_Lightning";
            case ExplosionType.Cryo: return "Explosion_Cryo";
            default: return "Explosion";
        }
    }

    private static string MaterialPrefix(ExplosionType t)
    {
        switch (t)
        {
            case ExplosionType.Fire: return "EXF_";
            case ExplosionType.Lightning: return "EXL_";
            case ExplosionType.Cryo: return "EXC_";
            default: return "EX_";
        }
    }

    private static Color[] DefaultColors(ExplosionType t)
    {
        switch (t)
        {
            case ExplosionType.Fire:
                return new[]
                {
                    new Color(1f, 0.86f, 0.6f),     // Core
                    new Color(1f, 0.38f, 0.08f),    // Fire（橙）
                    new Color(0.95f, 0.16f, 0.04f), // Afterburn（紅）
                    new Color(0.2f, 0.17f, 0.15f),  // Smoke
                    new Color(1f, 0.55f, 0.18f),    // Sparks
                };
            case ExplosionType.Lightning:
                return new[]
                {
                    new Color(0.94f, 0.86f, 1f),    // Core
                    new Color(0.58f, 0.24f, 1f),    // Energy
                    new Color(0.82f, 0.6f, 1f),     // Arcs
                    new Color(0.72f, 0.42f, 1f),    // Sparks
                };
            case ExplosionType.Cryo:
                return new[]
                {
                    new Color(0.92f, 0.98f, 1f),    // Core
                    new Color(0.45f, 0.76f, 1f),    // Frost
                    new Color(0.74f, 0.9f, 1f),     // Ice
                    new Color(0.82f, 0.91f, 1f),    // Mist
                };
            default:
                return new[]
                {
                    new Color(1f, 0.93f, 0.72f),    // Core
                    new Color(1f, 0.48f, 0.14f),    // Fire
                    new Color(0.27f, 0.25f, 0.24f), // Smoke
                    new Color(1f, 0.72f, 0.32f),    // Sparks
                };
        }
    }

    private static GUIContent[] ColorLabels(ExplosionType t)
    {
        switch (t)
        {
            case ExplosionType.Fire:
                return new[]
                {
                    new GUIContent("1. Core", "閃光、星芒、白熱核心，也決定閃光燈光的顏色。"),
                    new GUIContent("2. Fire", "炸開的火球（橙）。會隨時間自動變暗、熄滅。"),
                    new GUIContent("3. Afterburn", "爆炸後持續燃燒、往上竄的火舌（紅）。"),
                    new GUIContent("4. Smoke", "煙。"),
                    new GUIContent("5. Sparks", "火花、餘燼、衝擊環。"),
                };
            case ExplosionType.Lightning:
                return new[]
                {
                    new GUIContent("1. Core", "閃光、星芒，也決定閃光燈光的顏色。"),
                    new GUIContent("2. Energy", "電漿球、衝擊環、電離微光（紫）。"),
                    new GUIContent("3. Arcs", "放射狀電弧、殘留電弧。"),
                    new GUIContent("4. Sparks", "電火花、飄散的離子。"),
                };
            case ExplosionType.Cryo:
                return new[]
                {
                    new GUIContent("1. Core", "閃光、冰晶閃光、飄落的冰晶微塵，也決定閃光燈光的顏色。"),
                    new GUIContent("2. Frost", "寒氣核心、衝擊環（藍）。"),
                    new GUIContent("3. Ice", "炸開的冰刺、冰片。"),
                    new GUIContent("4. Mist", "擴散後慢慢下沉的寒氣（藍白）。"),
                };
            default:
                return new[]
                {
                    new GUIContent("1. Core", "閃光、星芒、白熱核心，也決定閃光燈光的顏色。"),
                    new GUIContent("2. Fire", "火球。會隨時間自動變暗成暗紅、熄滅。"),
                    new GUIContent("3. Smoke", "煙。"),
                    new GUIContent("4. Sparks", "火花、餘燼、衝擊環。"),
                };
        }
    }

    private static string[] ToggleLabels(ExplosionType t)
    {
        switch (t)
        {
            case ExplosionType.Lightning:
                return new[]
                {
                    "Spikes 星芒（爆開瞬間閃兩下）",
                    "Static 殘留電弧（爆炸後 0.7 秒內在周圍劈啪作響）",
                    "Ion Haze 電離微光（淡淡的紫色光暈）",
                };
            case ExplosionType.Cryo:
                return new[]
                {
                    "Glints 冰晶閃光（冰刺上一閃一閃的星芒）",
                    "Mist 寒氣（大量同時爆炸時最吃效能的一層）",
                    "Ice Shards 冰片",
                };
            default:
                return new[]
                {
                    "Spikes 星芒（動畫感最強的一層，想偏寫實可以關掉）",
                    "Smoke 煙（大量同時爆炸時最吃效能的一層）",
                    "Debris 碎片",
                };
        }
    }

    private void EnsureColors()
    {
        int n = DefaultColors(type).Length;
        if (colors == null || colors.Length != n) colors = DefaultColors(type);
    }

    // =====================================================================
    // GUI
    // =====================================================================

    private void OnGUI()
    {
        EnsureColors();

        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel);
        var newType = (ExplosionType)EditorGUILayout.EnumPopup(new GUIContent("Type",
            "Standard：敵人被摧毀時的標準爆炸。\n" +
            "Fire：火焰傷害（ExplosionDamage），紅橙色。\n" +
            "Lightning：閃電傷害（EnergyDamage），紫色。\n" +
            "Cryo：低溫傷害（ColdDamage），藍白色。\n\n" +
            "切換時會套用該類型的預設顏色；Prefab Name 還是預設名稱時也會一起換。"), type);
        if (newType != type)
        {
            if (prefabName == DefaultName(type)) prefabName = DefaultName(newType);
            type = newType;
            colors = DefaultColors(type);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        radius = EditorGUILayout.Slider(new GUIContent("Radius (m)",
            "爆炸的大約半徑（公尺）。其他圖層（煙、火花、衝擊環、燈光範圍）都依它等比例縮放。\n" +
            "參考：子彈命中 0.5～1、火箭彈 / 導彈 2～3、建築摧毀 4～6。"), radius, 0.2f, 15f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Colours (ColorPicker / EffectColorController can change these later)", EditorStyles.boldLabel);
        GUIContent[] labels = ColorLabels(type);
        for (int i = 0; i < colors.Length; i++)
            colors[i] = EditorGUILayout.ColorField(labels[i], colors[i]);
        if (GUILayout.Button("Reset Colours"))
            colors = DefaultColors(type);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
        string[] toggles = ToggleLabels(type);
        includeSpikes = EditorGUILayout.ToggleLeft(toggles[0], includeSpikes);
        includeSmoke = EditorGUILayout.ToggleLeft(toggles[1], includeSmoke);
        includeDebris = EditorGUILayout.ToggleLeft(toggles[2], includeDebris);
        includeShockwave = EditorGUILayout.ToggleLeft("Shockwave 衝擊環", includeShockwave);
        includeLight = EditorGUILayout.ToggleLeft("Flash Light 閃光燈光（照亮周圍；大量同時爆炸時建議關閉）", includeLight);
        softParticles = EditorGUILayout.ToggleLeft("Soft Particles（煙 / 寒氣貼地處淡出，需要 URP Asset 開啟 Depth Texture）", softParticles);

        EditorGUILayout.Space();
        placePreview = EditorGUILayout.ToggleLeft("建好後在 Scene 視圖中央放一個預覽（選取它即可在 Scene 視圖播放）", placePreview);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Prefab", GUILayout.Height(32)))
            Build();

        EditorGUILayout.HelpBox(
            "給 Bullet（火箭彈等）的 Hit Effect 用：直接拖進去即可，Bullet 會自己回收。\n" +
            "給只會生成、不會回收的程式用（例如 BuildingStats 的 Destroy Effect）：\n" +
            "在 prefab 的 ExplosionEffect 勾選 Despawn When Finished。\n" +
            "需要 Volume 裡有 Bloom，閃光、電弧和火花才會發光。",
            MessageType.Info);
    }

    // =====================================================================
    // Build
    // =====================================================================

    /// <summary>各類型建好圖層之後，交給共用流程收尾（燈光、顏色分組、控制器、存檔）的資料。</summary>
    private class LayerSet
    {
        public ParticleSystem lightColorSource;
        public ParticleSystem[][] colorGroups;
        public float lightPeak = 8f;
        public float lightDuration = 0.35f;
        public float lightFlicker = 0f;
    }

    private void Build()
    {
        EnsureColors();
        AssetDatabase.Refresh();
        EnsureFolder(outputFolder);
        EnsureFolder($"{outputFolder}/Materials");

        var root = new GameObject(prefabName);

        LayerSet set;
        switch (type)
        {
            case ExplosionType.Fire: set = BuildFireFamily(root.transform, fireElement: true); break;
            case ExplosionType.Lightning: set = BuildLightning(root.transform); break;
            case ExplosionType.Cryo: set = BuildCryo(root.transform); break;
            default: set = BuildFireFamily(root.transform, fireElement: false); break;
        }

        if (set == null)
        {
            DestroyImmediate(root);
            EditorUtility.DisplayDialog("Explosion Builder",
                $"Missing textures in {outputFolder}/Textures. See the Console for which file is missing.", "OK");
            return;
        }

        // ---- Flash Light ----
        Light light = null;
        if (includeLight)
        {
            var lightGo = new GameObject("FlashLight");
            lightGo.transform.SetParent(root.transform, false);
            light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f * radius;
            light.intensity = set.lightPeak;
            light.color = colors[0];
            light.shadows = LightShadows.None;
            light.enabled = false;   // ExplosionEffect 在啟用時點亮
        }

        // ---- Colour groups（EffectColorController / ColorPicker） ----
        var ecc = root.AddComponent<EffectColorController>();
        AssignColorGroups(ecc, set.colorGroups);

        // ---- Controller ----
        var fx = root.AddComponent<ExplosionEffect>();
        var so = new SerializedObject(fx);
        so.FindProperty("flashLight").objectReferenceValue = light;
        so.FindProperty("lightColorSource").objectReferenceValue = set.lightColorSource;
        so.FindProperty("lightPeakIntensity").floatValue = set.lightPeak;
        so.FindProperty("lightDuration").floatValue = set.lightDuration;
        SerializedProperty flicker = so.FindProperty("lightFlicker");
        if (flicker != null) flicker.floatValue = set.lightFlicker;
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

        Debug.Log($"ExplosionBuilder: built {prefabPath} ({type})");
    }

    private string Mat(string suffix) => MaterialPrefix(type) + suffix;

    // =====================================================================
    // Standard / Fire
    // =====================================================================
    //
    // 時間軸：
    //   0.00s  Flash 閃光 + Spikes 星芒 + FireCore 白熱核心
    //   0.00s  Fireball 火球炸開、Sparks 火花噴射、Debris 碎片拋出
    //   0.02s  Shockwave 衝擊環擴散
    //   0.06s  Smoke 煙從火球後方冒出，上升膨脹 1～2 秒
    //   全程   Embers 餘燼飄散
    //   Fire 額外：0.08～0.4s Afterburn 火舌往上竄、持續燃燒；火球和餘燼更多

    private LayerSet BuildFireFamily(Transform root, bool fireElement)
    {
        Texture2D texFlash = LoadTexture("EX_Flash.png");
        Texture2D texSpikes = LoadTexture("EX_Spikes.png");
        Texture2D texFire = LoadTexture("EX_FireSheet.png");
        Texture2D texSmoke = LoadTexture("EX_SmokeSheet.png");
        Texture2D texRing = LoadTexture("EX_Ring.png");
        Texture2D texSpark = LoadTexture("EX_Spark.png");
        Texture2D texDebris = LoadTexture("EX_DebrisSheet.png");

        if (texFlash == null || texSpikes == null || texFire == null || texSmoke == null ||
            texRing == null || texSpark == null || texDebris == null)
            return null;

        Color coreColor = colors[0];
        Color fireColor = colors[1];
        Color afterburnColor = fireElement ? colors[2] : colors[1];
        Color smokeColor = fireElement ? colors[3] : colors[2];
        Color sparkColor = fireElement ? colors[4] : colors[3];

        // 發光的層用 Additive（HDR 強度 > 1 會吃到 Bloom）；有實體的層用 Alpha 混合
        Material matFlash = MakeMaterial(Mat("Flash"), texFlash, additive: true, intensity: 4f, soft: false);
        Material matSpikes = MakeMaterial(Mat("Spikes"), texSpikes, additive: true, intensity: 3f, soft: false);
        Material matCore = MakeMaterial(Mat("FireCore"), texFire, additive: true, intensity: 2.2f, soft: false);
        Material matFire = MakeMaterial(Mat("Fire"), texFire, additive: false, intensity: 1.8f, soft: false);
        Material matSmoke = MakeMaterial(Mat("Smoke"), texSmoke, additive: false, intensity: 1f, soft: softParticles);
        Material matRing = MakeMaterial(Mat("Ring"), texRing, additive: true, intensity: 1.6f, soft: false);
        Material matSpark = MakeMaterial(Mat("Spark"), texSpark, additive: true, intensity: 3f, soft: false);
        Material matEmber = MakeMaterial(Mat("Ember"), texSpark, additive: true, intensity: 2.5f, soft: false);
        Material matDebris = MakeMaterial(Mat("Debris"), texDebris, additive: false, intensity: 1f, soft: false);

        float R = radius;

        // ---- Flash：硬核心的白熱閃光（一瞬間） ----
        ParticleSystem flash = CreateLayer("Flash", root, matFlash, local: true);
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
            spikes = CreateLayer("Spikes", root, matSpikes, local: true);
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
        ParticleSystem fireCore = CreateLayer("FireCore", root, matCore, local: true);
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
        ParticleSystem fireball = CreateLayer("Fireball", root, matFire, local: true);
        {
            var main = fireball.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * R, 3f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f * R, 1.1f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = fireColor;
            main.maxParticles = 30;
            if (fireElement) Burst(fireball, 12, 16);
            else Burst(fireball, 10, 14);
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

        // ---- Afterburn（Fire 專屬）：爆炸後持續燃燒、往上竄的紅色火舌 ----
        ParticleSystem afterburn = null;
        if (fireElement)
        {
            afterburn = CreateLayer("Afterburn", root, matFire, local: false);
            var main = afterburn.main;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f * R, 0.6f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f * R, 0.75f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);   // 大致直立，像火舌
            main.startColor = afterburnColor;
            main.gravityModifier = -0.35f;              // 熱氣往上竄
            main.maxParticles = 30;
            Bursts(afterburn, (0.08f, 4, 6), (0.18f, 3, 5), (0.3f, 3, 4), (0.42f, 2, 3));
            SetSphere(afterburn, 0.55f * R);
            SetDrag(afterburn, 1.5f);
            SetNoise(afterburn, 0.25f * R, 0.8f, 0.6f);
            SetSizeOverLife(afterburn, (0f, 0.35f), (0.3f, 1f), (1f, 0.25f));
            SetColorAndAlpha(afterburn,
                new[] { (1f, 0f), (0.75f, 0.5f), (0.3f, 1f) },
                new[] { (0f, 0f), (1f, 0.15f), (0.8f, 0.6f), (0f, 1f) },
                GradientMode.Blend);
            SetSheet(afterburn, 2, 2);
            afterburn.GetComponent<ParticleSystemRenderer>().sortingFudge = -1f;
        }

        // ---- Shockwave：面向鏡頭快速擴散的衝擊環 ----
        ParticleSystem ring = null;
        if (includeShockwave)
        {
            ring = CreateLayer("Shockwave", root, matRing, local: true);
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
        ParticleSystem sparks = CreateLayer("Sparks", root, matSpark, local: false);
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
            StretchRenderer(sparks, 0.04f, -3f);
        }

        // ---- Embers：慢慢飄散的餘燼 ----
        ParticleSystem embers = CreateLayer("Embers", root, matEmber, local: false);
        {
            var main = embers.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f * R, 3f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f * R, 0.09f * R);
            main.startColor = sparkColor;
            main.gravityModifier = -0.05f;               // 熱空氣往上帶
            main.maxParticles = fireElement ? 40 : 30;
            if (fireElement) Burst(embers, 16, 24);
            else Burst(embers, 8, 14);
            SetSphere(embers, 0.4f * R);
            SetDrag(embers, 1.5f);
            SetNoise(embers, 0.5f * R, 0.6f, 0.4f);
            SetAlphaSmooth(embers, (1f, 0f), (1f, 0.6f), (0f, 1f));
        }

        // ---- Smoke：火球後面冒出、慢慢上升膨脹的煙 ----
        ParticleSystem smoke = null;
        if (includeSmoke)
        {
            smoke = CreateLayer("Smoke", root, matSmoke, local: false);
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
            ParticleSystem debris = CreateLayer("Debris", root, matDebris, local: false);
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

        var set = new LayerSet { lightColorSource = flash };
        if (fireElement)
        {
            set.colorGroups = new[]
            {
                Group(flash, spikes, fireCore),   // 1. Core
                Group(fireball),                  // 2. Fire
                Group(afterburn),                 // 3. Afterburn
                Group(smoke),                     // 4. Smoke
                Group(sparks, embers, ring),      // 5. Sparks
            };
            set.lightPeak = 9f;
            set.lightDuration = 0.5f;            // 火焰燒得久一點，光也留久一點
            set.lightFlicker = 0.15f;            // 火光輕微搖曳
        }
        else
        {
            set.colorGroups = new[]
            {
                Group(flash, spikes, fireCore),   // 1. Core
                Group(fireball),                  // 2. Fire
                Group(smoke),                     // 3. Smoke
                Group(sparks, embers, ring),      // 4. Sparks
            };
        }
        return set;
    }

    // =====================================================================
    // Lightning（閃電 / EnergyDamage）
    // =====================================================================
    //
    // 時間軸：
    //   0.00s        Flash 閃光 + Spikes 星芒（0.07s 再閃一次）
    //   0.00～0.35s  Plasma 電漿球頻閃膨脹、Arcs 放射狀電弧分 6 波劈出
    //   0.00s        Sparks 電火花高速噴射；Shockwave 衝擊環
    //   0.10～0.70s  Static 殘留電弧在周圍零星劈啪
    //   全程          Ions 離子微粒飄散、Ion Haze 淡紫光暈
    //   燈光會隨機閃爍（ExplosionEffect.lightFlicker）

    private LayerSet BuildLightning(Transform root)
    {
        Texture2D texFlash = LoadTexture("EX_Flash.png");
        Texture2D texSpikes = LoadTexture("EX_Spikes.png");
        Texture2D texRing = LoadTexture("EX_Ring.png");
        Texture2D texSpark = LoadTexture("EX_Spark.png");
        Texture2D texSmoke = LoadTexture("EX_SmokeSheet.png");
        Texture2D texArc = LoadTexture("EXL_ArcSheet.png");
        Texture2D texPlasma = LoadTexture("EXL_Plasma.png");

        if (texFlash == null || texSpikes == null || texRing == null || texSpark == null ||
            texSmoke == null || texArc == null || texPlasma == null)
            return null;

        Color coreColor = colors[0];
        Color energyColor = colors[1];
        Color arcColor = colors[2];
        Color sparkColor = colors[3];

        Material matFlash = MakeMaterial(Mat("Flash"), texFlash, additive: true, intensity: 4.5f, soft: false);
        Material matSpikes = MakeMaterial(Mat("Spikes"), texSpikes, additive: true, intensity: 3.5f, soft: false);
        Material matPlasma = MakeMaterial(Mat("Plasma"), texPlasma, additive: true, intensity: 2.6f, soft: false);
        Material matArc = MakeMaterial(Mat("Arc"), texArc, additive: true, intensity: 5f, soft: false);
        Material matRing = MakeMaterial(Mat("Ring"), texRing, additive: true, intensity: 2f, soft: false);
        Material matSpark = MakeMaterial(Mat("Spark"), texSpark, additive: true, intensity: 4f, soft: false);
        Material matIon = MakeMaterial(Mat("Ion"), texSpark, additive: true, intensity: 3f, soft: false);
        Material matHaze = MakeMaterial(Mat("Haze"), texSmoke, additive: true, intensity: 0.7f, soft: softParticles);

        float R = radius;

        // ---- Flash：電光一閃 ----
        ParticleSystem flash = CreateLayer("Flash", root, matFlash, local: true);
        {
            var main = flash.main;
            main.startLifetime = 0.1f;
            main.startSpeed = 0f;
            main.startSize = 2f * R;
            main.startColor = coreColor;
            main.maxParticles = 2;
            Burst(flash, 1, 1);
            NoShape(flash);
            SetSizeOverLife(flash, (0f, 0.6f), (0.2f, 1f), (1f, 1.1f));
            SetAlphaSteps(flash, (1f, 0f), (0.4f, 0.5f), (0f, 0.85f));
            flash.GetComponent<ParticleSystemRenderer>().sortingFudge = -10f;
        }

        // ---- Spikes：星芒閃兩下（電光的頻閃感） ----
        ParticleSystem spikes = null;
        if (includeSpikes)
        {
            spikes = CreateLayer("Spikes", root, matSpikes, local: true);
            var main = spikes.main;
            main.duration = 0.1f;
            main.startLifetime = 0.06f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.2f * R, 2.8f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = coreColor;
            main.maxParticles = 4;
            Bursts(spikes, (0f, 1, 1), (0.07f, 1, 1));
            NoShape(spikes);
            SetAlphaSteps(spikes, (1f, 0f), (0f, 0.9f));
            spikes.GetComponent<ParticleSystemRenderer>().sortingFudge = -10f;
        }

        // ---- Plasma：膨脹的電漿球，頻閃著消失 ----
        ParticleSystem plasma = CreateLayer("Plasma", root, matPlasma, local: true);
        {
            var main = plasma.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.38f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.4f * R, 1.8f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = energyColor;
            main.maxParticles = 3;
            Burst(plasma, 2, 2);
            NoShape(plasma);
            SetSpin(plasma, 4f);
            SetSizeOverLife(plasma, (0f, 0.3f), (0.2f, 1f), (1f, 1.25f));
            // 硬切的透明度 = 頻閃
            SetAlphaSteps(plasma, (1f, 0f), (0.45f, 0.2f), (0.95f, 0.3f), (0.35f, 0.5f), (0.75f, 0.62f), (0f, 0.8f));
            plasma.GetComponent<ParticleSystemRenderer>().sortingFudge = -4f;
        }

        // ---- Arcs：從中心放射出去的電弧，分好幾波劈出 ----
        //      貼圖是「從中心往右」的電弧，隨機旋轉 → 往四面八方放射
        ParticleSystem arcs = CreateLayer("Arcs", root, matArc, local: true);
        {
            var main = arcs.main;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(2f * R, 3f * R);   // 電弧長度 ≈ 尺寸的一半
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = arcColor;
            main.maxParticles = 30;
            Bursts(arcs, (0f, 4, 6), (0.04f, 3, 5), (0.09f, 3, 4), (0.15f, 2, 3), (0.22f, 1, 3), (0.3f, 1, 2));
            NoShape(arcs);
            SetAlphaSteps(arcs, (1f, 0f), (0.5f, 0.6f), (0f, 0.85f));
            SetSheet(arcs, 2, 2);
            arcs.GetComponent<ParticleSystemRenderer>().sortingFudge = -6f;
        }

        // ---- Static：爆炸後在周圍零星劈啪的小電弧 ----
        ParticleSystem staticArcs = null;
        if (includeSmoke)
        {
            staticArcs = CreateLayer("Static", root, matArc, local: true);
            var main = staticArcs.main;
            main.duration = 0.7f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f * R, 1.1f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = arcColor;
            main.maxParticles = 12;
            Bursts(staticArcs, (0.1f, 1, 2), (0.17f, 1, 2), (0.26f, 1, 2), (0.34f, 0, 2),
                               (0.43f, 1, 1), (0.52f, 0, 2), (0.61f, 0, 1), (0.69f, 0, 1));
            SetSphere(staticArcs, 0.9f * R);
            SetAlphaSteps(staticArcs, (1f, 0f), (0f, 0.8f));
            SetSheet(staticArcs, 2, 2);
            staticArcs.GetComponent<ParticleSystemRenderer>().sortingFudge = -6f;
        }

        // ---- Shockwave：細而快的衝擊環 ----
        ParticleSystem ring = null;
        if (includeShockwave)
        {
            ring = CreateLayer("Shockwave", root, matRing, local: true);
            var main = ring.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 0f;
            main.startSize = 1f * R;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = energyColor;
            main.maxParticles = 2;
            Burst(ring, 1, 1);
            NoShape(ring);
            SetSizeOverLife(ring, (0f, 0.3f), (0.5f, 2.6f), (1f, 3.2f));
            SetAlphaSteps(ring, (1f, 0f), (0.5f, 0.45f), (0f, 0.8f));
            ring.GetComponent<ParticleSystemRenderer>().sortingFudge = -5f;
        }

        // ---- Sparks：高速、短命、幾乎不受重力的電火花，路徑帶抖動 ----
        ParticleSystem sparks = CreateLayer("Sparks", root, matSpark, local: false);
        {
            var main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f * R, 16f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f * R, 0.1f * R);
            main.startColor = sparkColor;
            main.gravityModifier = 0.2f;
            main.maxParticles = 60;
            Burst(sparks, 25, 40);
            SetSphere(sparks, 0.2f * R);
            SetDrag(sparks, 4f);
            SetNoise(sparks, 1.5f * R, 3f, 2f);          // 高頻抖動 → 鋸齒狀的軌跡
            SetAlphaSteps(sparks, (1f, 0f), (0.6f, 0.5f), (0f, 0.85f));
            StretchRenderer(sparks, 0.05f, -3f);
        }

        // ---- Ions：慢慢飄散、一明一暗的離子微粒 ----
        ParticleSystem ions = CreateLayer("Ions", root, matIon, local: false);
        {
            var main = ions.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f * R, 2.5f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f * R, 0.08f * R);
            main.startColor = sparkColor;
            main.maxParticles = 24;
            Burst(ions, 10, 16);
            SetSphere(ions, 0.5f * R);
            SetDrag(ions, 2f);
            SetNoise(ions, 0.6f * R, 1.2f, 0.8f);
            SetAlphaSteps(ions, (1f, 0f), (0.3f, 0.2f), (1f, 0.35f), (0.4f, 0.55f), (0.9f, 0.7f), (0f, 0.88f));
        }

        // ---- Ion Haze：淡淡的紫色光暈（加法混合，不會像煙那樣擋住畫面） ----
        ParticleSystem haze = null;
        if (includeDebris)
        {
            haze = CreateLayer("IonHaze", root, matHaze, local: false);
            var main = haze.main;
            main.startDelay = 0.03f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f * R, 0.8f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f * R, 2.2f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = energyColor;
            main.maxParticles = 6;
            Burst(haze, 3, 4);
            SetSphere(haze, 0.3f * R);
            SetDrag(haze, 2f);
            SetSpin(haze, 0.4f);
            SetSizeOverLife(haze, (0f, 0.6f), (1f, 1.3f));
            SetAlphaSmooth(haze, (0f, 0f), (0.5f, 0.12f), (0f, 1f));
            SetSheet(haze, 2, 2);
            haze.GetComponent<ParticleSystemRenderer>().sortingFudge = 5f;
        }

        return new LayerSet
        {
            lightColorSource = flash,
            colorGroups = new[]
            {
                Group(flash, spikes),               // 1. Core
                Group(plasma, ring, haze),          // 2. Energy
                Group(arcs, staticArcs),            // 3. Arcs
                Group(sparks, ions),                // 4. Sparks
            },
            lightPeak = 10f,
            lightDuration = 0.45f,
            lightFlicker = 0.7f,                    // 電光忽明忽暗
        };
    }

    // =====================================================================
    // Cryo（低溫 / ColdDamage）
    // =====================================================================
    //
    // 時間軸：
    //   0.00s        Flash 冷白閃光、FrostCore 寒氣核心
    //   0.00～0.12s  Crystals 冰刺從中心「啪」地刺出，撐住 0.3 秒後淡出（像瞬間結冰再碎掉）
    //   0.00s        Shards 冰片拋出落下；Shockwave 衝擊環
    //   0.08～0.45s  Glints 冰刺上一閃一閃的星芒
    //   0.03s～      Mist 寒氣往外擴散後被拖住、慢慢往下沉（冷空氣比較重）
    //   全程          Diamond Dust 冰晶微塵閃爍著飄落

    private LayerSet BuildCryo(Transform root)
    {
        Texture2D texFlash = LoadTexture("EX_Flash.png");
        Texture2D texSpikes = LoadTexture("EX_Spikes.png");
        Texture2D texFire = LoadTexture("EX_FireSheet.png");
        Texture2D texSmoke = LoadTexture("EX_SmokeSheet.png");
        Texture2D texRing = LoadTexture("EX_Ring.png");
        Texture2D texSpark = LoadTexture("EX_Spark.png");
        Texture2D texCrystal = LoadTexture("EXC_CrystalSheet.png");
        Texture2D texShard = LoadTexture("EXC_ShardSheet.png");

        if (texFlash == null || texSpikes == null || texFire == null || texSmoke == null ||
            texRing == null || texSpark == null || texCrystal == null || texShard == null)
            return null;

        Color coreColor = colors[0];
        Color frostColor = colors[1];
        Color iceColor = colors[2];
        Color mistColor = colors[3];

        Material matFlash = MakeMaterial(Mat("Flash"), texFlash, additive: true, intensity: 3.5f, soft: false);
        Material matGlint = MakeMaterial(Mat("Glint"), texSpikes, additive: true, intensity: 3.5f, soft: false);
        Material matFrostCore = MakeMaterial(Mat("FrostCore"), texFire, additive: true, intensity: 1.6f, soft: false);
        Material matCrystal = MakeMaterial(Mat("Crystal"), texCrystal, additive: false, intensity: 1.5f, soft: false);
        Material matShard = MakeMaterial(Mat("Shard"), texShard, additive: false, intensity: 1.4f, soft: false);
        Material matRing = MakeMaterial(Mat("Ring"), texRing, additive: true, intensity: 1.6f, soft: false);
        Material matMist = MakeMaterial(Mat("Mist"), texSmoke, additive: false, intensity: 1.2f, soft: softParticles);
        Material matDust = MakeMaterial(Mat("Dust"), texSpark, additive: true, intensity: 2.5f, soft: false);

        float R = radius;

        // ---- Flash：冷白色閃光 ----
        ParticleSystem flash = CreateLayer("Flash", root, matFlash, local: true);
        {
            var main = flash.main;
            main.startLifetime = 0.14f;
            main.startSpeed = 0f;
            main.startSize = 2f * R;
            main.startColor = coreColor;
            main.maxParticles = 2;
            Burst(flash, 1, 1);
            NoShape(flash);
            SetSizeOverLife(flash, (0f, 0.5f), (0.25f, 1f), (1f, 1.1f));
            SetAlphaSmooth(flash, (1f, 0f), (0.8f, 0.35f), (0f, 1f));
            flash.GetComponent<ParticleSystemRenderer>().sortingFudge = -10f;
        }

        // ---- FrostCore：中心一團發光的寒氣，很快消散 ----
        ParticleSystem frostCore = CreateLayer("FrostCore", root, matFrostCore, local: true);
        {
            var main = frostCore.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * R, 1.2f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f * R, 1.2f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = frostColor;
            main.maxParticles = 8;
            Burst(frostCore, 3, 5);
            SetSphere(frostCore, 0.2f * R);
            SetDrag(frostCore, 4f);
            SetSizeOverLife(frostCore, (0f, 0.6f), (0.4f, 1.1f), (1f, 1.2f));
            SetAlphaSmooth(frostCore, (1f, 0f), (0.7f, 0.5f), (0f, 1f));
            SetSheet(frostCore, 2, 2);
            frostCore.GetComponent<ParticleSystemRenderer>().sortingFudge = -4f;
        }

        // ---- Crystals：從中心「啪」地刺出的冰刺，撐住一下再淡出 ----
        //      貼圖是「從中心往右」的冰刺，隨機旋轉 → 往四面八方刺出
        ParticleSystem crystals = CreateLayer("Crystals", root, matCrystal, local: true);
        {
            var main = crystals.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.6f * R, 2.4f * R);   // 冰刺長度 ≈ 尺寸的一半
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = iceColor;
            main.maxParticles = 12;
            Burst(crystals, 6, 9);
            NoShape(crystals);
            SetSizeOverLife(crystals, (0f, 0f), (0.12f, 1f), (1f, 1.05f));   // 一瞬間長出來
            SetAlphaSmooth(crystals, (1f, 0f), (1f, 0.55f), (0f, 1f));
            SetSheet(crystals, 2, 2);
            crystals.GetComponent<ParticleSystemRenderer>().sortingFudge = -2f;
        }

        // ---- Glints：冰刺上一閃一閃的小星芒 ----
        ParticleSystem glints = null;
        if (includeSpikes)
        {
            glints = CreateLayer("Glints", root, matGlint, local: true);
            var main = glints.main;
            main.duration = 0.45f;
            main.startLifetime = 0.14f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f * R, 0.7f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = coreColor;
            main.maxParticles = 8;
            Bursts(glints, (0.08f, 1, 2), (0.18f, 1, 2), (0.3f, 1, 1), (0.42f, 0, 1));
            SetSphere(glints, 0.9f * R);
            SetSizeOverLife(glints, (0f, 0.2f), (0.4f, 1f), (1f, 0f));
            glints.GetComponent<ParticleSystemRenderer>().sortingFudge = -8f;
        }

        // ---- Shockwave：寒氣衝擊環 ----
        ParticleSystem ring = null;
        if (includeShockwave)
        {
            ring = CreateLayer("Shockwave", root, matRing, local: true);
            var main = ring.main;
            main.startDelay = 0.02f;
            main.startLifetime = 0.35f;
            main.startSpeed = 0f;
            main.startSize = 1f * R;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = frostColor;
            main.maxParticles = 2;
            Burst(ring, 1, 1);
            NoShape(ring);
            SetSizeOverLife(ring, (0f, 0.3f), (0.4f, 2.6f), (1f, 3.2f));
            SetAlphaSteps(ring, (0.9f, 0f), (0.5f, 0.5f), (0f, 0.85f));
            ring.GetComponent<ParticleSystemRenderer>().sortingFudge = -6f;
        }

        // ---- Shards：拋出去、受重力落下、高速旋轉的冰片 ----
        ParticleSystem shards = null;
        if (includeDebris)
        {
            shards = CreateLayer("Shards", root, matShard, local: false);
            var main = shards.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f * R, 10f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f * R, 0.22f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = iceColor;
            main.gravityModifier = 1.6f;
            main.maxParticles = 24;
            Burst(shards, 10, 16);
            SetSphere(shards, 0.2f * R);
            SetDrag(shards, 1f);
            SetSpin(shards, 10f);
            SetAlphaSmooth(shards, (1f, 0f), (1f, 0.75f), (0f, 1f));
            SetSheet(shards, 2, 2);
        }

        // ---- Mist：往外擴散後被拖住、慢慢往下沉的寒氣 ----
        ParticleSystem mist = null;
        if (includeSmoke)
        {
            mist = CreateLayer("Mist", root, matMist, local: false);
            var main = mist.main;
            main.startDelay = 0.03f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * R, 3f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(1f * R, 1.6f * R);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = mistColor;
            main.gravityModifier = 0.03f;               // 冷空氣比較重，慢慢往下沉
            main.maxParticles = 24;
            Burst(mist, 10, 14);
            SetSphere(mist, 0.4f * R);
            SetDrag(mist, 3f);
            SetNoise(mist, 0.25f * R, 0.4f, 0.2f);
            SetSizeOverLife(mist, (0f, 0.5f), (0.3f, 1.3f), (1f, 2.2f));
            SetSpin(mist, 0.25f);
            SetAlphaSmooth(mist, (0f, 0f), (0.7f, 0.1f), (0.45f, 0.5f), (0f, 1f));
            SetSheet(mist, 2, 2);
            mist.GetComponent<ParticleSystemRenderer>().sortingFudge = 5f;
        }

        // ---- Diamond Dust：閃爍著慢慢飄落的冰晶微塵 ----
        ParticleSystem dust = CreateLayer("DiamondDust", root, matDust, local: false);
        {
            var main = dust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * R, 2f * R);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f * R, 0.06f * R);
            main.startColor = coreColor;
            main.gravityModifier = 0.08f;
            main.maxParticles = 40;
            Burst(dust, 20, 30);
            SetSphere(dust, 0.6f * R);
            SetDrag(dust, 1.5f);
            SetNoise(dust, 0.3f * R, 0.6f, 0.3f);
            // 硬切的透明度 = 一閃一閃
            SetAlphaSteps(dust, (1f, 0f), (0.3f, 0.15f), (1f, 0.3f), (0.4f, 0.5f), (1f, 0.65f), (0f, 0.9f));
        }

        return new LayerSet
        {
            lightColorSource = flash,
            colorGroups = new[]
            {
                Group(flash, glints, dust),         // 1. Core
                Group(frostCore, ring),             // 2. Frost
                Group(crystals, shards),            // 3. Ice
                Group(mist),                        // 4. Mist
            },
            lightPeak = 7f,
            lightDuration = 0.4f,
            lightFlicker = 0f,
        };
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

    /// <summary>
    /// 多個時間點的 Burst。(time, min, max)。
    /// ⚠ 最後一個時間點必須在 main.duration 之內，否則不會發射。
    /// </summary>
    private static void Bursts(ParticleSystem ps, params (float, int, int)[] bursts)
    {
        var arr = new ParticleSystem.Burst[bursts.Length];
        for (int i = 0; i < bursts.Length; i++)
            arr[i] = new ParticleSystem.Burst(bursts[i].Item1, (short)bursts[i].Item2, (short)bursts[i].Item3);

        var e = ps.emission;
        e.SetBursts(arr);
    }

    /// <summary>沿速度方向拉長（火花類）。</summary>
    private static void StretchRenderer(ParticleSystem ps, float velocityScale, float sortingFudge)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 1f;
        r.velocityScale = velocityScale;
        r.cameraVelocityScale = 0f;
        r.sortingFudge = sortingFudge;
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
