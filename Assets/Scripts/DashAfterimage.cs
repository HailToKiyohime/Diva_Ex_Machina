using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DashAfterimage : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("讀 dash 狀態用。拖你的 PlayerMovement 進來。")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Auto-Collect Roots")]
    [Tooltip("Body 物件（圖中的 Body）。會自動抓底下所有 SkinnedMeshRenderer。留空 = 用本物件（Player）當根。")]
    [SerializeField] private Transform bodyRoot;

    [Tooltip("（可選）裝備 mesh 的容器。留空 = 自動用 BoneCombiner.Instance 的 transform。")]
    [SerializeField] private Transform equipmentRoot;

    [Tooltip("（可選）不在上述兩個根底下、想額外加入的 SkinnedMeshRenderer。")]
    [SerializeField] private SkinnedMeshRenderer[] extraRenderers;

    [Tooltip("每次 dash 都重新掃描，讓換裝後新裝上的裝備也能吃到殘影。")]
    [SerializeField] private bool rescanOnEachDash = true;

    [Header("Ghost Look")]
    [Tooltip("殘影材質：建議 URP/Unlit + Transparent（柔和）或 Additive（發光）。")]
    [SerializeField] private Material ghostMaterial;
    [Tooltip("殘影顏色（含起始 alpha），會覆蓋材質的 _BaseColor。")]
    [SerializeField] private Color ghostColor = new Color(0.4f, 0.75f, 1f, 0.5f);

    [Header("Timing")]
    [Tooltip("一次 dash 產生殘影的總時長（秒）。")]
    [SerializeField] private float burstDuration = 0.3f;
    [Tooltip("每隔多久生一張殘影，越小越密。")]
    [SerializeField] private float spawnInterval = 0.04f;
    [Tooltip("單張殘影從出現到消失的時間。")]
    [SerializeField] private float ghostLifetime = 0.3f;

    private bool _wasDashing = false;
    private float _burstUntil = 0f;
    private float _nextSpawn = 0f;

    // 收集到的渲染器（自動 + 手動，已去重）
    private readonly List<SkinnedMeshRenderer> _renderers = new List<SkinnedMeshRenderer>();
    private readonly HashSet<SkinnedMeshRenderer> _seen = new HashSet<SkinnedMeshRenderer>();
    private readonly List<SkinnedMeshRenderer> _scanBuffer = new List<SkinnedMeshRenderer>();
    private bool _collectedOnce = false;

    void Start()
    {
        if (!rescanOnEachDash)
            CollectRenderers();
    }

    void Update()
    {
        if (playerMovement == null) return;

        // 偵測 dash 起始邊沿
        bool dashing = playerMovement.IsDashActive;
        if (dashing && !_wasDashing)
        {
            if (rescanOnEachDash || !_collectedOnce)
                CollectRenderers();

            _burstUntil = Time.time + burstDuration;
            _nextSpawn = 0f; // 立刻生第一張
        }
        _wasDashing = dashing;

        if (Time.time <= _burstUntil && Time.time >= _nextSpawn)
        {
            _nextSpawn = Time.time + Mathf.Max(0.01f, spawnInterval);
            SpawnGhosts();
        }
    }

    // 換裝後若把 rescanOnEachDash 關掉，可從 EquipmentManager 手動呼叫這個刷新
    public void RefreshRenderers() => CollectRenderers();

    private void CollectRenderers()
    {
        _renderers.Clear();
        _seen.Clear();

        // 1) Body 底下所有 SMR（留空就用本物件當根 = Player）
        Transform bodySearch = (bodyRoot != null) ? bodyRoot : transform;
        AddFromRoot(bodySearch);

        // 2) 裝備容器（留空就自動用 BoneCombiner 單例）
        Transform eqSearch = equipmentRoot;
        if (eqSearch == null && BoneCombiner.Instance != null)
            eqSearch = BoneCombiner.Instance.transform;
        AddFromRoot(eqSearch);

        // 3) 額外手動指定的
        if (extraRenderers != null)
        {
            for (int i = 0; i < extraRenderers.Length; i++)
                AddOne(extraRenderers[i]);
        }

        _collectedOnce = true;
    }

    private void AddFromRoot(Transform root)
    {
        if (root == null) return;
        _scanBuffer.Clear();
        root.GetComponentsInChildren<SkinnedMeshRenderer>(true, _scanBuffer); // 含 inactive
        for (int i = 0; i < _scanBuffer.Count; i++)
            AddOne(_scanBuffer[i]);
    }

    private void AddOne(SkinnedMeshRenderer smr)
    {
        if (smr == null) return;
        if (_seen.Add(smr)) // 去重（Body 根與裝備根若有重疊也不會重複）
            _renderers.Add(smr);
    }

    private void SpawnGhosts()
    {
        if (ghostMaterial == null || _renderers.Count == 0) return;

        // 玩家目前站著的移動平台（null = 站在靜止地面）
        Transform platform = (playerMovement != null) ? playerMovement.CurrentPlatform : null;

        for (int i = 0; i < _renderers.Count; i++)
        {
            var smr = _renderers[i];
            if (smr == null || !smr.gameObject.activeInHierarchy) continue;
            if (smr.sharedMesh == null) continue;

            // 1) 烘焙當前 pose（useScale=false：scale 自己用 lossyScale 套）
            Mesh baked = new Mesh();
            smr.BakeMesh(baked, false);

            // 2) 臨時鬼影 GO，對齊世界 TRS
            var go = new GameObject("DashGhost");
            go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            go.transform.localScale = smr.transform.lossyScale;

            // ★ 若站在會動的平台上，parent 到平台，讓殘影跟著船一起平移/旋轉
            if (platform != null)
                go.transform.SetParent(platform, true); // worldPositionStays：保留當前世界姿態後再跟隨

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = baked;

            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // 每張鬼影一份材質 instance，覆蓋所有 submesh
            var matInstance = new Material(ghostMaterial);
            int subs = Mathf.Max(1, baked.subMeshCount);
            var mats = new Material[subs];
            for (int s = 0; s < subs; s++) mats[s] = matInstance;
            mr.sharedMaterials = mats;

            // 3) 淡出 + 自毀
            var fader = go.AddComponent<DashGhostFader>();
            fader.Init(matInstance, baked, ghostColor, ghostLifetime);
        }
    }
}