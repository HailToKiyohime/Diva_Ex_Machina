using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DashAfterimage : MonoBehaviour
{
    [Header("Refs")]
    //[Tooltip("Ū dash ���A�ΡC��A�� PlayerMovement �i�ӡC")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Auto-Collect Roots")]
    //[Tooltip("Body ����]�Ϥ��� Body�^�C�|�۰ʧ쩳�U�Ҧ� SkinnedMeshRenderer�C�d�� = �Υ�����]Player�^��ڡC")]
    [SerializeField] private Transform bodyRoot;

    //[Tooltip("�]�i��^�˳� mesh ���e���C�d�� = �۰ʥ� BoneCombiner.Instance �� transform�C")]
    [SerializeField] private Transform equipmentRoot;

    //[Tooltip("�]�i��^���b�W�z��Ӯک��U�B�Q�B�~�[�J�� SkinnedMeshRenderer�C")]
    [SerializeField] private SkinnedMeshRenderer[] extraRenderers;

    //[Tooltip("�C�� dash �����s���y�A�����˫�s�ˤW���˳Ƥ]��Y��ݼv�C")]
    [SerializeField] private bool rescanOnEachDash = true;

    [Header("Ghost Look")]
    //[Tooltip("�ݼv����G��ĳ URP/Unlit + Transparent�]�X�M�^�� Additive�]�o���^�C")]
    [SerializeField] private Material ghostMaterial;
    //[Tooltip("�ݼv�C��]�t�_�l alpha�^�A�|�л\���誺 _BaseColor�C")]
    [SerializeField] private Color ghostColor = new Color(0.4f, 0.75f, 1f, 0.5f);

    [Header("Timing")]
    //[Tooltip("�@�� dash ���ʹݼv���`�ɪ��]��^�C")]
    [SerializeField] private float burstDuration = 0.3f;
    //[Tooltip("�C�j�h�[�ͤ@�i�ݼv�A�V�p�V�K�C")]
    [SerializeField] private float spawnInterval = 0.04f;
    //[Tooltip("��i�ݼv�q�X�{��������ɶ��C")]
    [SerializeField] private float ghostLifetime = 0.3f;

    private bool _wasDashing = false;
    private float _burstUntil = 0f;
    private float _nextSpawn = 0f;

    // �����쪺��V���]�۰� + ��ʡA�w�h���^
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

        // ���� dash �_�l��u
        bool dashing = playerMovement.IsDashActive;
        if (dashing && !_wasDashing)
        {
            if (rescanOnEachDash || !_collectedOnce)
                CollectRenderers();

            _burstUntil = Time.time + burstDuration;
            _nextSpawn = 0f; // �ߨ�ͲĤ@�i
        }
        _wasDashing = dashing;

        if (Time.time <= _burstUntil && Time.time >= _nextSpawn)
        {
            _nextSpawn = Time.time + Mathf.Max(0.01f, spawnInterval);
            SpawnGhosts();
        }
    }

    // ���˫�Y�� rescanOnEachDash �����A�i�q EquipmentManager ��ʩI�s�o�Ө�s
    public void RefreshRenderers() => CollectRenderers();

    private void CollectRenderers()
    {
        _renderers.Clear();
        _seen.Clear();

        // 1) Body ���U�Ҧ� SMR�]�d�ŴN�Υ������� = Player�^
        Transform bodySearch = (bodyRoot != null) ? bodyRoot : transform;
        AddFromRoot(bodySearch);

        // 2) �˳Ʈe���]�d�ŴN�۰ʥ� BoneCombiner ��ҡ^
        Transform eqSearch = equipmentRoot;
        if (eqSearch == null && BoneCombiner.Instance != null)
            eqSearch = BoneCombiner.Instance.transform;
        AddFromRoot(eqSearch);

        // 3) �B�~��ʫ��w��
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
        root.GetComponentsInChildren<SkinnedMeshRenderer>(true, _scanBuffer); // �t inactive
        for (int i = 0; i < _scanBuffer.Count; i++)
            AddOne(_scanBuffer[i]);
    }

    private void AddOne(SkinnedMeshRenderer smr)
    {
        if (smr == null) return;
        if (_seen.Add(smr)) // �h���]Body �ڻP�˳ƮڭY�����|�]���|���ơ^
            _renderers.Add(smr);
    }

    private void SpawnGhosts()
    {
        if (ghostMaterial == null || _renderers.Count == 0) return;

        // ���a�ثe���۪����ʥ��x�]null = ���b�R��a���^
        Transform platform = (playerMovement != null) ? playerMovement.CurrentPlatform : null;

        for (int i = 0; i < _renderers.Count; i++)
        {
            var smr = _renderers[i];
            if (smr == null || !smr.gameObject.activeInHierarchy) continue;
            if (smr.sharedMesh == null) continue;

            // 1) �M�H��e pose�]useScale=false�Gscale �ۤv�� lossyScale �M�^
            Mesh baked = new Mesh();
            smr.BakeMesh(baked, false);

            // 2) �{�ɰ��v GO�A���@�� TRS
            var go = new GameObject("DashGhost");
            go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            go.transform.localScale = smr.transform.lossyScale;

            // �� �Y���b�|�ʪ����x�W�Aparent �쥭�x�A���ݼv��۲�@�_����/����
            if (platform != null)
                go.transform.SetParent(platform, true); // worldPositionStays�G�O�d��e�@�ɫ��A��A���H

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = baked;

            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // �C�i���v�@������ instance�A�л\�Ҧ� submesh
            var matInstance = new Material(ghostMaterial);
            int subs = Mathf.Max(1, baked.subMeshCount);
            var mats = new Material[subs];
            for (int s = 0; s < subs; s++) mats[s] = matInstance;
            mr.sharedMaterials = mats;

            // 3) �H�X + �۷�
            var fader = go.AddComponent<DashGhostFader>();
            fader.Init(matInstance, baked, ghostColor, ghostLifetime);
        }
    }
}