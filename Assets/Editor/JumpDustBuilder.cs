#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 建立跳躍 / 落地塵土 prefab。
///
/// 三種風格：
///   Hybrid（預設）- 融合：塵團有清楚的團塊輪廓與柔和的兩階明暗（像手繪），
///                   邊緣帶一點雜訊散開；先維持實心、再平滑淡出；貼地塵土波；重落地才有淡淡的速度線
///   Stylized      - 動畫式三階分色塵團、硬切換淡出、衝擊環與速度線
///   Realistic     - 柔和、有雜訊邊緣的煙塵，慢慢擴散淡出，細小碎粒，沒有速度線
///
/// 開啟：Tools > Diva Ex Machina > Jump Dust Builder
///
/// 需要 {Output Folder}/Textures 裡對應風格的 JD_*.png。
/// 材質建在 {Output Folder}/Materials，prefab 存成 {Output Folder}/{Prefab Name}.prefab。
/// 重新建置會覆寫它們，所以可以改下面的數值再按一次 Build。
///
/// 勾選「Attach to selected Player」時，會在選取的玩家（有 PlayerMovement 的物件）
/// 加上 PlayerJumpDust 並指定這個 prefab。
/// </summary>
public class JumpDustBuilder : EditorWindow
{
    private string outputFolder = "Assets/VFX/JumpDust";
    private string prefabName = "JumpDust";

    private enum Style { Hybrid, Stylized, Realistic }
    private Style style = Style.Hybrid;

    private float effectScale = 1f;
    private bool softParticles = true;
    private bool attachToPlayer = true;

    [MenuItem("Tools/Diva Ex Machina/Jump Dust Builder")]
    private static void Open()
    {
        GetWindow<JumpDustBuilder>("Jump Dust");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Look", EditorStyles.boldLabel);
        style = (Style)EditorGUILayout.EnumPopup(new GUIContent("Style",
            "Hybrid：風格化與寫實的融合（預設）。Stylized：動畫式分色塵團。Realistic：柔和的煙塵。"), style);
        effectScale = EditorGUILayout.Slider(new GUIContent("Effect Scale",
            "整體大小。1 = 以身高約 2 公尺的角色為準。"), effectScale, 0.25f, 4f);
        softParticles = EditorGUILayout.ToggleLeft(new GUIContent(
            "Soft Particles（塵團貼地處淡出，需要 URP Asset 開啟 Depth Texture）"), softParticles);

        EditorGUILayout.Space();
        attachToPlayer = EditorGUILayout.ToggleLeft(
            "Attach to selected Player（在選取的玩家上加 PlayerJumpDust 並指定 prefab）", attachToPlayer);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Prefab", GUILayout.Height(32)))
            Build();

        EditorGUILayout.HelpBox(
            "起跳：PlayerMovement.JumpAction 會呼叫 PlayerJumpDust.PlayJump()。\n" +
            "落地：PlayerJumpDust 自己偵測，依下落速度決定大小。\n" +
            "顏色：依腳下地面自動取色；物件上加 DustSurface 可以指定顏色或關閉。",
            MessageType.Info);
    }

    // =====================================================================
    // Build
    // =====================================================================

    /// <summary>依風格挑數值：V(Stylized, Hybrid, Realistic)。</summary>
    private float V(float stylized, float hybrid, float realistic)
    {
        return style == Style.Stylized ? stylized : style == Style.Realistic ? realistic : hybrid;
    }

    /// <summary>依風格挑「隨機範圍」(min, max)，再乘上 scale。</summary>
    private ParticleSystem.MinMaxCurve R(Vector2 stylized, Vector2 hybrid, Vector2 realistic, float scale = 1f)
    {
        return new ParticleSystem.MinMaxCurve(
            V(stylized.x, hybrid.x, realistic.x) * scale,
            V(stylized.y, hybrid.y, realistic.y) * scale);
    }

    private void Build()
    {
        AssetDatabase.Refresh();
        EnsureFolder(outputFolder);
        EnsureFolder($"{outputFolder}/Materials");

        string texPuffName = style == Style.Stylized ? "JD_PuffSheet.png" : style == Style.Realistic ? "JD_SmokeSheet.png" : "JD_HybridPuffSheet.png";
        string texRingName = style == Style.Stylized ? "JD_GroundRing.png" : style == Style.Realistic ? "JD_SoftRing.png" : "JD_HybridRing.png";
        string texDebrisName = style == Style.Stylized ? "JD_DebrisSheet.png" : "JD_GritSheet.png";

        Texture2D texPuff = LoadTexture(texPuffName);
        Texture2D texDebris = LoadTexture(texDebrisName);
        Texture2D texRing = LoadTexture(texRingName);
        Texture2D texStreak = LoadTexture("JD_Streak.png");

        if (texPuff == null || texDebris == null || texRing == null || texStreak == null)
        {
            EditorUtility.DisplayDialog("Jump Dust Builder",
                $"Missing textures in {outputFolder}/Textures. See the Console for which JD_*.png is missing.", "OK");
            return;
        }

        string suffix = style == Style.Hybrid ? "" : "_" + style;
        Material matPuff = MakeAlphaMaterial("JD_Puff" + suffix, texPuff, softParticles);
        Material matDebris = MakeAlphaMaterial("JD_Debris" + suffix, texDebris, false);
        Material matRing = MakeAlphaMaterial("JD_Ring" + suffix, texRing, softParticles);
        Material matStreak = MakeAlphaMaterial("JD_Streak", texStreak, false);

        float S = effectScale;

        var root = new GameObject(prefabName);

        // ---- Burst：貼地向外推開的一圈塵團（主體） ----
        ParticleSystem burst = CreateLayer("Burst", root.transform, matPuff, 120);
        {
            var main = burst.main;
            main.startLifetime = R(new Vector2(0.55f, 0.9f), new Vector2(0.7f, 1.1f), new Vector2(0.9f, 1.5f));
            main.startSpeed = R(new Vector2(5f, 8f), new Vector2(4.5f, 7.5f), new Vector2(4f, 7f), S);
            main.startSize = R(new Vector2(0.45f, 0.8f), new Vector2(0.55f, 0.95f), new Vector2(0.6f, 1.1f), S);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            // 錐角約 80° + 從邊緣發射 = 幾乎貼著地面往外推，略微上揚
            SetUpCone(burst, angle: V(80f, 81f, 82f), radius: 0.35f * S, fromEdge: true);
            SetDrag(burst, V(4f, 3.8f, 3.5f));                 // 先快速推開，再被空氣拖住
            SetRise(burst, V(0.6f, 0.45f, 0.35f) * S);         // 停下來之後慢慢上浮
            SetSpin(burst, V(0.7f, 0.45f, 0.3f));
            switch (style)
            {
                case Style.Stylized:
                    SetSizeOverLife(burst, (0f, 0.6f), (0.3f, 1.2f), (1f, 1.6f));
                    SetAlphaSmooth(burst, (0f, 0f), (1f, 0.06f), (0.85f, 0.5f), (0f, 1f));
                    break;
                case Style.Realistic:
                    SetSizeOverLife(burst, (0f, 0.5f), (0.25f, 1.3f), (1f, 2.4f));
                    SetAlphaSmooth(burst, (0f, 0f), (0.55f, 0.08f), (0.4f, 0.45f), (0f, 1f));
                    break;
                default:
                    // 融合：先維持實心（讀得出團塊形狀），後半段才平滑淡出
                    SetSizeOverLife(burst, (0f, 0.55f), (0.28f, 1.25f), (1f, 2.0f));
                    SetAlphaSmooth(burst, (0f, 0f), (0.95f, 0.06f), (0.85f, 0.4f), (0.35f, 0.75f), (0f, 1f));
                    break;
            }
            SetSheet(burst, 2, 2);
        }

        // ---- Cloud：中央往上翻起的大塵團 ----
        ParticleSystem cloud = CreateLayer("Cloud", root.transform, matPuff, 40);
        {
            var main = cloud.main;
            main.startLifetime = R(new Vector2(0.8f, 1.2f), new Vector2(1.0f, 1.6f), new Vector2(1.4f, 2.2f));
            main.startSpeed = R(new Vector2(1.2f, 2.4f), new Vector2(1.0f, 2.1f), new Vector2(0.8f, 1.8f), S);
            main.startSize = R(new Vector2(0.9f, 1.5f), new Vector2(1.0f, 1.7f), new Vector2(1.2f, 2.0f), S);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            SetUpCone(cloud, angle: V(40f, 48f, 55f), radius: 0.25f * S, fromEdge: false);
            SetDrag(cloud, V(2.5f, 2.2f, 2f));
            SetRise(cloud, V(0.5f, 0.45f, 0.4f) * S);
            SetSpin(cloud, V(0.4f, 0.25f, 0.15f));
            switch (style)
            {
                case Style.Stylized:
                    SetSizeOverLife(cloud, (0f, 0.7f), (0.4f, 1.25f), (1f, 1.5f));
                    SetAlphaSmooth(cloud, (0f, 0f), (0.9f, 0.08f), (0.7f, 0.5f), (0f, 1f));
                    break;
                case Style.Realistic:
                    SetSizeOverLife(cloud, (0f, 0.6f), (0.4f, 1.5f), (1f, 2.2f));
                    SetAlphaSmooth(cloud, (0f, 0f), (0.45f, 0.1f), (0.3f, 0.5f), (0f, 1f));
                    break;
                default:
                    SetSizeOverLife(cloud, (0f, 0.65f), (0.4f, 1.35f), (1f, 1.85f));
                    SetAlphaSmooth(cloud, (0f, 0f), (0.8f, 0.08f), (0.65f, 0.45f), (0f, 1f));
                    break;
            }
            SetSheet(cloud, 2, 2);

            // 大塵團畫在後面，外推的小塵團疊在它前面
            cloud.GetComponent<ParticleSystemRenderer>().sortingFudge = 5f;
        }

        // ---- Debris：被踢起、受重力落下的碎石 ----
        ParticleSystem debris = CreateLayer("Debris", root.transform, matDebris, 60);
        {
            var main = debris.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f * S, 8f * S);
            main.startSize = R(new Vector2(0.06f, 0.14f), new Vector2(0.04f, 0.1f), new Vector2(0.03f, 0.08f), S);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 2.2f;

            SetUpCone(debris, angle: 50f, radius: 0.3f * S, fromEdge: false);
            SetSpin(debris, 6f);
            if (style == Style.Stylized) SetAlphaSteps(debris, (1f, 0f), (0f, 0.85f));        // 最後直接消失
            else SetAlphaSmooth(debris, (1f, 0f), (1f, V(0.7f, 0.75f, 0.7f)), (0f, 1f));       // 落地前淡出
            SetSheet(debris, 2, 2);
            debris.GetComponent<ParticleSystemRenderer>().sortingFudge = -5f;
        }

        // ---- Streaks：貼地往外射的速度線（強落地才出現） ----
        ParticleSystem streaks = CreateLayer("Streaks", root.transform, matStreak, 60);
        {
            var main = streaks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f * S, 16f * S);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * S, 0.12f * S);

            SetUpCone(streaks, angle: 88f, radius: 0.3f * S, fromEdge: true);
            SetDrag(streaks, 6f);
            if (style == Style.Stylized) SetAlphaSteps(streaks, (1f, 0f), (0.6f, 0.5f), (0f, 0.85f));
            else SetAlphaSmooth(streaks, (0.8f, 0f), (0.5f, 0.5f), (0f, 1f));

            var r = streaks.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 1f;
            r.velocityScale = 0.06f;
            r.cameraVelocityScale = 0f;
            r.sortingFudge = -2f;
        }

        // ---- Ring：貼著地面擴散的塵土波 / 衝擊環 ----
        ParticleSystem ring = CreateLayer("Ring", root.transform, matRing, 10);
        {
            // 平躺在地面：Billboard + Local 對齊時，面片朝向本地 +Z → 把 +Z 轉成朝上
            ring.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            var main = ring.main;
            main.startLifetime = V(0.35f, 0.55f, 0.9f);
            main.startSpeed = 0f;
            main.startSize = 1f * S;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var shape = ring.shape;
            shape.enabled = false;

            switch (style)
            {
                case Style.Stylized:
                    SetSizeOverLife(ring, (0f, 0.4f), (0.35f, 2.3f), (1f, 3.2f));
                    SetAlphaSteps(ring, (0.85f, 0f), (0.5f, 0.45f), (0f, 0.9f));
                    break;
                case Style.Realistic:
                    SetSizeOverLife(ring, (0f, 0.5f), (0.3f, 2.2f), (1f, 3.4f));
                    SetAlphaSmooth(ring, (0f, 0f), (0.35f, 0.08f), (0.2f, 0.5f), (0f, 1f));
                    break;
                default:
                    SetSizeOverLife(ring, (0f, 0.45f), (0.3f, 2.2f), (1f, 3.3f));
                    SetAlphaSmooth(ring, (0f, 0f), (0.7f, 0.06f), (0.45f, 0.45f), (0f, 1f));
                    break;
            }

            var r = ring.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.Local;
            r.sortingFudge = 10f;   // 畫在所有塵團後面
        }

        // ---- Controller ----
        var fx = root.AddComponent<JumpDustEffect>();
        var so = new SerializedObject(fx);
        so.FindProperty("burst").objectReferenceValue = burst;
        so.FindProperty("cloud").objectReferenceValue = cloud;
        so.FindProperty("debris").objectReferenceValue = debris;
        so.FindProperty("streaks").objectReferenceValue = streaks;
        so.FindProperty("ring").objectReferenceValue = ring;

        // 各風格的數量與顏色（Stylized 用 JumpDustEffect 的預設值）
        if (style == Style.Realistic)
        {
            // 沒有速度線；塵團半透明；碎粒少一點
            so.FindProperty("streakCount").intValue = 0;
            so.FindProperty("debrisCount").intValue = 6;
            so.FindProperty("burstCount").intValue = 12;
            so.FindProperty("cloudCount").intValue = 5;
            so.FindProperty("ringMinIntensity").floatValue = 0.2f;
            so.FindProperty("burstTint").colorValue = new Color(1f, 1f, 1f, 0.85f);
            so.FindProperty("cloudTint").colorValue = new Color(0.95f, 0.95f, 0.95f, 0.7f);
            so.FindProperty("debrisTint").colorValue = new Color(0.45f, 0.42f, 0.38f, 1f);
            so.FindProperty("ringTint").colorValue = new Color(1f, 1f, 1f, 0.6f);
            so.FindProperty("colorVariation").floatValue = 0.9f;
        }
        else if (style == Style.Hybrid)
        {
            // 速度線只在重落地出現、而且很淡；塵團接近實心
            so.FindProperty("streakCount").intValue = 6;
            so.FindProperty("streakMinIntensity").floatValue = 0.6f;
            so.FindProperty("debrisCount").intValue = 8;
            so.FindProperty("burstCount").intValue = 13;
            so.FindProperty("cloudCount").intValue = 5;
            so.FindProperty("ringMinIntensity").floatValue = 0.25f;
            so.FindProperty("burstTint").colorValue = new Color(1f, 1f, 1f, 0.95f);
            so.FindProperty("cloudTint").colorValue = new Color(0.95f, 0.95f, 0.95f, 0.8f);
            so.FindProperty("debrisTint").colorValue = new Color(0.5f, 0.47f, 0.42f, 1f);
            so.FindProperty("streakTint").colorValue = new Color(1.1f, 1.1f, 1.1f, 0.6f);
            so.FindProperty("ringTint").colorValue = new Color(1.05f, 1.05f, 1.05f, 0.7f);
            so.FindProperty("colorVariation").floatValue = 0.9f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Save ----
        string prefabPath = $"{outputFolder}/{prefabName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
        DestroyImmediate(root);

        if (!success || prefab == null)
        {
            Debug.LogError($"JumpDustBuilder: failed to save {prefabPath}");
            return;
        }

        AssetDatabase.SaveAssets();

        JumpDustEffect prefabEffect = prefab.GetComponent<JumpDustEffect>();
        RelinkSceneReferences(prefabEffect);   // 重新建置後，場景裡既有的 PlayerJumpDust 自動改指新的 prefab
        if (attachToPlayer) AttachToSelectedPlayer(prefabEffect);

        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"JumpDustBuilder: built {prefabPath}");
    }

    /// <summary>
    /// 覆寫 prefab 之後，舊的元件參考可能會斷掉。把開啟中場景裡、
    /// 參考是空的或指向同一個 prefab 的 PlayerJumpDust 都重新指定一次。
    /// </summary>
    private static void RelinkSceneReferences(JumpDustEffect prefabEffect)
    {
        string prefabPath = AssetDatabase.GetAssetPath(prefabEffect);
        var all = Object.FindObjectsByType<PlayerJumpDust>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayerJumpDust d in all)
        {
            var so = new SerializedObject(d);
            SerializedProperty prop = so.FindProperty("dustPrefab");
            Object current = prop.objectReferenceValue;
            if (current == prefabEffect) continue;
            if (current != null && AssetDatabase.GetAssetPath(current) != prefabPath) continue;   // 刻意指向別的 prefab

            prop.objectReferenceValue = prefabEffect;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(d);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(d.gameObject.scene);
        }
    }

    private static void AttachToSelectedPlayer(JumpDustEffect prefabEffect)
    {
        Transform sel = Selection.activeTransform;
        PlayerMovement movement = sel != null ? sel.GetComponentInParent<PlayerMovement>(true) : null;
        if (movement == null)
        {
            Debug.Log("JumpDustBuilder: 沒有選取玩家（有 PlayerMovement 的物件），所以沒有自動掛上 PlayerJumpDust。");
            return;
        }

        PlayerJumpDust dust = movement.GetComponent<PlayerJumpDust>();
        if (dust == null) dust = Undo.AddComponent<PlayerJumpDust>(movement.gameObject);

        var so = new SerializedObject(dust);
        so.FindProperty("dustPrefab").objectReferenceValue = prefabEffect;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(dust);
        Debug.Log($"JumpDustBuilder: PlayerJumpDust 已掛到 {movement.name}。記得存場景 / prefab。", dust);
    }

    // =====================================================================
    // Particle helpers
    // =====================================================================

    private static ParticleSystem CreateLayer(string name, Transform parent, Material material, int maxParticles)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;              // 一直在「播放」但不發射，只靠 Emit() 噴出
        main.duration = 1f;
        main.playOnAwake = true;
        main.prewarm = false;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = maxParticles;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.startColor = new Color(0.78f, 0.70f, 0.56f, 1f);

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

    /// <summary>朝上（沿地面法線）的錐形發射。fromEdge = 從錐底邊緣發射，方向固定在錐面上。</summary>
    private static void SetUpCone(ParticleSystem ps, float angle, float radius, bool fromEdge)
    {
        var s = ps.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Cone;
        s.angle = angle;
        s.radius = radius;
        s.radiusThickness = fromEdge ? 0f : 1f;
        s.arc = 360f;
        s.rotation = new Vector3(-90f, 0f, 0f);   // 錐的 +Z 轉成朝上
    }

    private static void SetDrag(ParticleSystem ps, float drag)
    {
        var lv = ps.limitVelocityOverLifetime;
        lv.enabled = true;
        lv.limit = 1000f;
        lv.drag = drag;
        lv.multiplyDragByParticleSize = false;
        lv.multiplyDragByParticleVelocity = false;
    }

    /// <summary>沿地面法線（本地 +Y）緩慢上浮。</summary>
    private static void SetRise(ParticleSystem ps, float speed)
    {
        var v = ps.velocityOverLifetime;
        v.enabled = true;
        v.space = ParticleSystemSimulationSpace.Local;
        // 三個軸必須是同一種模式（Random Between Two Constants），否則 Unity 會報錯
        v.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        v.y = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
        v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    private static void SetSizeOverLife(ParticleSystem ps, params (float time, float value)[] keys)
    {
        var curve = new AnimationCurve();
        foreach (var k in keys) curve.AddKey(k.time, k.value);
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    /// <summary>隨機自轉，radiansPerSecond 為最大值（正負隨機）。</summary>
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
        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);   // 每顆粒子固定一個隨機格
        tsa.cycleCount = 1;
    }

    /// <summary>硬切換的透明度（GradientMode.Fixed）—— 動畫式分段。keys = (alpha, time)。</summary>
    private static void SetAlphaSteps(ParticleSystem ps, params (float alpha, float time)[] keys)
    {
        SetAlpha(ps, GradientMode.Fixed, keys);
    }

    /// <summary>平滑的透明度 —— 寫實的淡出。keys = (alpha, time)。</summary>
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

    // =====================================================================
    // Asset helpers
    // =====================================================================

    private Texture2D LoadTexture(string fileName)
    {
        string path = $"{outputFolder}/Textures/{fileName}";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"JumpDustBuilder: texture not found at {path}");
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

    /// <summary>URP Particles/Unlit，半透明 Alpha 混合（塵土不發光，不能用 Additive）。</summary>
    private Material MakeAlphaMaterial(string name, Texture texture, bool soft)
    {
        string path = $"{outputFolder}/Materials/{name}.mat";

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("JumpDustBuilder: URP Particles/Unlit not found, falling back to built-in particles.");
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
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
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);

        // URP transparent + alpha blend（跟 URP 材質 Inspector 寫入的值相同）
        SetFloatIfExists(mat, "_Surface", 1f);
        SetFloatIfExists(mat, "_Blend", 0f);
        SetFloatIfExists(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloatIfExists(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfExists(mat, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloatIfExists(mat, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfExists(mat, "_ZWrite", 0f);
        SetFloatIfExists(mat, "_Cull", (float)CullMode.Off);
        SetFloatIfExists(mat, "_ColorMode", 0f);          // 乘上粒子顏色

        // Soft particles：塵團跟地面相交的地方淡出，不會切出一條硬邊
        const float near = 0f, far = 0.4f;
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
