using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
public class BuildSystem : MonoBehaviour
{

    [SerializeField] private Toggle[] buildToggles;

    [Header("Raycast")]
    public Camera cam;
    public LayerMask buildableLayers;
    public float maxDistance = 500f;

    [Header("TopDown RawImage Input")]
    public bool useRawImageInput = true;
    public RawImage gridRawImage;         
    public Camera topDownCamera;        
    public Canvas uiCanvas;               
    [Header("Blueprint")]
    public int currentFootprintIndex = 0;
    public BuildBlueprint[] footprints;

    [Header("Preview")]
    public bool enablePreview = true;
    public bool previewAlignToGridRotation = true;
    public bool previewParentToGrid = true;
    public bool previewDisableColliders = true;
    public int previewLayer = 2;

    [Header("Placement")]
    public bool placeOnLeftClick = true;
    public bool placeAlignToGridRotation = true;
    public bool placeParentToGrid = true;

    [Header("Rotation")]
    public bool enableRotation = true;
    public float scrollSensitivity = 1f;
    public int rotationStep = 0; // 0..3 , each = 90 degrees clockwise

    [Header("Gizmo Highlight")]
    public bool drawHitCellGizmo = true;
    public bool drawFootprintPreview = true;
    public float gizmoYOffset = 0.01f;

    private BuildingGrid _hitGrid;
    private int _hitGridIndex = -1;
    private int _hitCellX = -1;
    private int _hitCellY = -1;
    private bool _hasHitCell = false;

    private GameObject _previewGO;
    private BuildBlueprint _previewBP;
    private BuildingGrid _previewGrid;
    private int _previewGridIndex;
    private int _previewCellX;
    private int _previewCellY;
    private bool _previewCanPlace;

    public bool buildModeOn = false;

    [Header("Zoom")]
    public CinemachineCamera buildingCamera;
    public CinemachinePositionComposer buildingCameraPositionComposer;
    public float maxZoomDistance = 550;
    public float minZoomDistance = 50;
    public float zoomSensitivity = 25f;
    public float panSensitivity = 0.08f;

    private Vector3 _panStartTargetOffset;
    private Vector2 _panStartMousePos;
    private bool _isPanning;
    [Header("Ghost Duplication")]
    public bool duplicateToGhost = true;
    public bool duplicateDisableRenderers = true;

    [SerializeField] NavMeshSurface ghostSurface;
    AsyncOperation _asyncBuild;
    private void Awake()    
    {
        if (!cam) cam = Camera.main;
    }

    private void OnEnable()
    {
        HookBuildToggles(true);
        EvaluateBuildTogglesNow();
    }

    private void OnDisable()
    {
        HookBuildToggles(false);
        DestroyPreview();
    }
    private void HookBuildToggles(bool hook)
    {
        if (buildToggles == null) return;

        for (int i = 0; i < buildToggles.Length; i++)
        {
            var t = buildToggles[i];
            if (!t) continue;

            if (hook) t.onValueChanged.AddListener(OnAnyBuildToggleChanged);
            else t.onValueChanged.RemoveListener(OnAnyBuildToggleChanged);
        }
    }

    private void OnAnyBuildToggleChanged(bool _)
    {
        EvaluateBuildTogglesNow();
    }

    private void EvaluateBuildTogglesNow()
    {
        if (buildToggles == null || buildToggles.Length == 0) return;

        bool allOff = true;

        for (int i = 0; i < buildToggles.Length; i++)
        {
            var t = buildToggles[i];
            if (!t) continue;

            if (t.isOn) allOff = false;
        }

        // 通常更像是「全部都 off 才清掉」
        if (allOff) ClearBuildingFootprints();
    }


    private void Update()
    {
        HandleZoomInput();
        HandlePanInput();
        if (buildModeOn) {
            UpdateHitCell();
            HandleRotateInput();

            if (enablePreview) UpdatePreview();
            else DestroyPreview();

            if (placeOnLeftClick && Input.GetMouseButtonDown(0))
                TryPlaceCurrentBlueprint();
        }
    }

    private void HandleRotateInput()
    {
        if (!enableRotation) return;

        if (!Input.GetKeyDown(KeyCode.R)) return;

        int dir = 1;
        if (Input.GetKeyDown(KeyCode.R) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            dir = -1;

        rotationStep = Mod(rotationStep + dir, 4);
    }
    private int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }

    private void UpdateHitCell()
    {
        _hasHitCell = false;
        _hitGrid = null;
        _hitGridIndex = -1;
        _hitCellX = -1;
        _hitCellY = -1;

        Ray ray;

        if (useRawImageInput && TryGetRayFromRawImage(out ray))
        {
            // ok
        }
        else
        {
            if (!cam) return;
            ray = cam.ScreenPointToRay(Input.mousePosition);
        }

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, buildableLayers))
            return;

        var grid = hit.collider.GetComponentInParent<BuildingGrid>();
        if (!grid) return;
        if (grid.grids == null || grid.grids.Length == 0) return;

        if (!TryGetCellFromHit(grid, hit.point, out int gridIndex, out int cellX, out int cellY))
            return;

        _hasHitCell = true;
        _hitGrid = grid;
        _hitGridIndex = gridIndex;
        _hitCellX = cellX;
        _hitCellY = cellY;
    }

    private bool TryGetRayFromRawImage(out Ray ray)
    {
        ray = default;

        if (!gridRawImage || !topDownCamera) return false;

        RectTransform rt = gridRawImage.rectTransform;

        Camera eventCam = null;
        if (uiCanvas && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCam = uiCanvas.worldCamera;

        Vector2 screenPos = Input.mousePosition;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, eventCam))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, eventCam, out Vector2 local))
            return false;

        Rect rect = rt.rect;

        float nx = (local.x - rect.xMin) / rect.width;
        float ny = (local.y - rect.yMin) / rect.height;

        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
            return false;

        Rect uvRect = gridRawImage.uvRect;
        float u = uvRect.x + nx * uvRect.width;
        float v = uvRect.y + ny * uvRect.height;

        ray = topDownCamera.ViewportPointToRay(new Vector3(u, v, 0f));
        return true;
    }

    private BuildBlueprint GetCurrentBlueprint()
    {
        if (footprints == null || footprints.Length == 0) return null;
        if (currentFootprintIndex < 0 || currentFootprintIndex >= footprints.Length) return null;
        return footprints[currentFootprintIndex];
    }

    private BoolMatrix GetCurrentFootprint()
    {
        var bp = GetCurrentBlueprint();
        if (bp == null) return null;

        var fp = bp.footprint;
        if (fp == null || fp.cells == null || fp.width <= 0 || fp.height <= 0) return null;
        return fp;
    }

    private void TryPlaceCurrentBlueprint()
    {
        if (!_hasHitCell || !_hitGrid) return;

        var bp = GetCurrentBlueprint();
        if (bp == null || !bp.buildingPrefab) return;

        var fp = GetCurrentFootprint();
        if (fp == null) return;

        bool outOfBounds;
        bool canPlace = EvaluatePlacement(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep, out outOfBounds);
        if (!canPlace) return;

        if (!InventoryManager.Instance.DeductItem(bp.costs)) return;

        ApplyFootprintOccupancyRotated(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep);

        Vector3 pos = GetFootprintCenterWorld(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep);

        Quaternion baseRot = placeAlignToGridRotation ? _hitGrid.transform.rotation : Quaternion.identity;
        Quaternion rot = baseRot * Quaternion.Euler(0f, rotationStep * 90f, 0f);

        Transform parent = placeParentToGrid ? _hitGrid.transform : null;

        GameObject placed = Instantiate(bp.buildingPrefab, pos, rot, parent);

        if (bp.buildingMaterial != null)
            ApplyMaterialToAllRenderers(placed, bp.buildingMaterial);


        // ----------  DUPLICATE TO GHOST SHIP  ----------
        if (duplicateToGhost && TryGetShipRoots(out var realRoot, out var ghostRoot))
        {
            Vector3 gp = placed.transform.localPosition;
            var ghost = Instantiate(bp.buildingPrefab, ghostRoot);
            ghost.transform.localPosition = placed.transform.localPosition;
            ghost.transform.localRotation = placed.transform.localRotation;
            ghost.name = placed.name + "_GHOST";
            
            if (duplicateDisableRenderers)
            {
                foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                    r.enabled = false;   // 只保留 Collider + NavMeshObstacle
            }
         }
    }

    private void UpdatePreview()
    {
        if (!_hasHitCell || !_hitGrid)
        {
            DestroyPreview();
            return;
        }

        var bp = GetCurrentBlueprint();
        if (bp == null || !bp.buildingPrefab)
        {
            DestroyPreview();
            return;
        }

        var fp = GetCurrentFootprint();
        if (fp == null)
        {
            DestroyPreview();
            return;
        }

        bool outOfBounds;
        bool canPlace = EvaluatePlacement(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep, out outOfBounds);

        bool needNew =
            _previewGO == null ||
            _previewBP != bp ||
            _previewGrid != _hitGrid ||
            _previewGridIndex != _hitGridIndex;

        if (needNew)
        {
            DestroyPreview();

            _previewGO = Instantiate(bp.pendingPrefab);
            _previewGO.name = bp.pendingPrefab.name + "_PREVIEW";

            if (previewParentToGrid)
                _previewGO.transform.SetParent(_hitGrid.transform, true);

            SetLayerRecursive(_previewGO, previewLayer);

            if (previewDisableColliders)
                DisableAllColliders(_previewGO);

            _previewBP = bp;
            _previewGrid = _hitGrid;
            _previewGridIndex = _hitGridIndex;
        }

        Vector3 pos = GetFootprintCenterWorld(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep);

        Quaternion baseRot = previewAlignToGridRotation ? _hitGrid.transform.rotation : Quaternion.identity;
        Quaternion rot = baseRot * Quaternion.Euler(0f, rotationStep * 90f, 0f);

        _previewGO.transform.SetPositionAndRotation(pos, rot);

        bool changed = (_previewCellX != _hitCellX) || (_previewCellY != _hitCellY) || (_previewCanPlace != canPlace);
        _previewCellX = _hitCellX;
        _previewCellY = _hitCellY;
        _previewCanPlace = canPlace;

        if (changed)
        {
            Material m = canPlace ? bp.pendingMaterial : bp.unavailableMaterial;
            if (m != null)
                ApplyMaterialToAllRenderers(_previewGO, m);
        }
    }

    private void DestroyPreview()
    {
        if (_previewGO != null) Destroy(_previewGO);
        _previewGO = null;
        _previewBP = null;
        _previewGrid = null;
        _previewGridIndex = -1;
        _previewCellX = 0;
        _previewCellY = 0;
        _previewCanPlace = false;
    }

    private bool EvaluatePlacement(BuildingGrid grid, int gridIndex, int anchorX, int anchorY, BoolMatrix fp, int rotStep, out bool outOfBounds)
    {
        outOfBounds = false;
        if (grid == null || fp == null) return false;

        int srcW = fp.width;
        int srcH = fp.height;

        int dstW = (rotStep % 2 == 0) ? srcW : srcH;
        int dstH = (rotStep % 2 == 0) ? srcH : srcW;

        var gd = grid.grids[gridIndex];

        bool any = false;

        for (int y = 0; y < srcH; y++)
        {
            for (int x = 0; x < srcW; x++)
            {
                if (!fp.Get(x, y)) continue;
                any = true;

                RotateCell(x, y, srcW, srcH, rotStep, out int rx, out int ry);

                int gx = anchorX + rx;
                int gy = anchorY + ry;

                if (gx < 0 || gx >= gd.gridSizeX || gy < 0 || gy >= gd.gridSizeY)
                {
                    outOfBounds = true;
                    return false;
                }

                if (grid.IsOccupied(gridIndex, gx, gy))
                    return false;
            }
        }

        return any;
    }

    private void ApplyFootprintOccupancyRotated(BuildingGrid grid, int gridIndex, int anchorX, int anchorY, BoolMatrix fp, int rotStep)
    {
        int srcW = fp.width;
        int srcH = fp.height;

        for (int y = 0; y < srcH; y++)
        {
            for (int x = 0; x < srcW; x++)
            {
                if (!fp.Get(x, y)) continue;

                RotateCell(x, y, srcW, srcH, rotStep, out int rx, out int ry);

                int gx = anchorX + rx;
                int gy = anchorY + ry;

                grid.SetOccupied(gridIndex, gx, gy, true);
            }
        }
    }

    private void RotateCell(int x, int y, int w, int h, int rotStep, out int rx, out int ry)
    {
        rotStep = Mod(rotStep, 4);

        if (rotStep == 0)
        {
            rx = x; ry = y; return;
        }

        if (rotStep == 1) // 90 CW
        {
            rx = y;
            ry = (w - 1) - x;
            return;
        }

        if (rotStep == 2) // 180
        {
            rx = (w - 1) - x;
            ry = (h - 1) - y;
            return;
        }

        // 270 CW
        rx = (h - 1) - y;
        ry = x;
    }

    private void OnDrawGizmos()
    {
        if (!drawHitCellGizmo) return;
        if (!_hasHitCell || !_hitGrid) return;

        var fp = GetCurrentFootprint();
        if (fp == null) return;

        bool outOfBounds;
        bool canPlace = EvaluatePlacement(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep, out outOfBounds);

        Color c = canPlace ? Color.green : Color.red;
        DrawFootprintOutlineRotated(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep, c);
    }

    private void DrawFootprintOutlineRotated(BuildingGrid grid, int gridIndex, int anchorX, int anchorY, BoolMatrix fp, int rotStep, Color color)
    {
        int srcW = fp.width;
        int srcH = fp.height;
        var gd = grid.grids[gridIndex];

        for (int y = 0; y < srcH; y++)
        {
            for (int x = 0; x < srcW; x++)
            {
                if (!fp.Get(x, y)) continue;

                RotateCell(x, y, srcW, srcH, rotStep, out int rx, out int ry);

                int gx = anchorX + rx;
                int gy = anchorY + ry;

                if (gx < 0 || gx >= gd.gridSizeX || gy < 0 || gy >= gd.gridSizeY)
                    continue;

                Color cc = grid.IsOccupied(gridIndex, gx, gy) ? Color.red : color;
                DrawSingleCellOutline(grid, gridIndex, gx, gy, cc);
            }
        }
    }

    private void DrawSingleCellOutline(BuildingGrid grid, int gridIndex, int cellX, int cellY, Color color)
    {
        var g = grid.grids[gridIndex];

        Transform t = grid.transform;
        Vector3 right = t.right;
        Vector3 forward = t.forward;
        Vector3 up = t.up;

        Vector3 baseCenter =
            t.position + up * g.gridHeight +
            right * (g.centreOffsetX * g.cellSize) +
            forward * (g.centreOffsetY * g.cellSize);

        float width = g.gridSizeX * g.cellSize;
        float depth = g.gridSizeY * g.cellSize;

        Vector3 origin =
            baseCenter - right * (width * 0.5f) - forward * (depth * 0.5f);

        Vector3 cellMin =
            origin + right * (cellX * g.cellSize) + forward * (cellY * g.cellSize);

        Vector3 p0 = cellMin + up * gizmoYOffset;
        Vector3 p1 = cellMin + right * g.cellSize + up * gizmoYOffset;
        Vector3 p2 = cellMin + right * g.cellSize + forward * g.cellSize + up * gizmoYOffset;
        Vector3 p3 = cellMin + forward * g.cellSize + up * gizmoYOffset;

        Gizmos.color = color;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
    }

    private void ApplyMaterialToAllRenderers(GameObject root, Material mat)
    {
        if (!root || mat == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            var shared = r.sharedMaterials;
            if (shared == null || shared.Length == 0)
            {
                r.sharedMaterial = mat;
                continue;
            }

            for (int s = 0; s < shared.Length; s++)
                shared[s] = mat;

            r.sharedMaterials = shared;
        }
    }

    private void DisableAllColliders(GameObject root)
    {
        var cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;
    }

    private void SetLayerRecursive(GameObject root, int layer)
    {
        if (!root) return;

        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            if (child) SetLayerRecursive(child.gameObject, layer);
        }
    }

    private bool TryGetCellFromHit(BuildingGrid gridComp, Vector3 hitPointWorld,
        out int gridIndex, out int cellX, out int cellY)
    {
        gridIndex = -1;
        cellX = -1;
        cellY = -1;

        Transform t = gridComp.transform;
        Vector3 right = t.right;
        Vector3 forward = t.forward;
        Vector3 up = t.up;
        Vector3 basePos = t.position;

        for (int i = 0; i < gridComp.grids.Length; i++)
        {
            var g = gridComp.grids[i];
            if (g.cellSize <= 0f || g.gridSizeX <= 0 || g.gridSizeY <= 0) continue;

            Vector3 center =
                basePos + up * g.gridHeight +
                right * (g.centreOffsetX * g.cellSize) +
                forward * (g.centreOffsetY * g.cellSize);

            float width = g.gridSizeX * g.cellSize;
            float depth = g.gridSizeY * g.cellSize;

            Vector3 origin =
                center - right * (width * 0.5f) - forward * (depth * 0.5f);

            Vector3 delta = hitPointWorld - origin;

            float xUnits = Vector3.Dot(delta, right) / g.cellSize;
            float yUnits = Vector3.Dot(delta, forward) / g.cellSize;

            int x = Mathf.FloorToInt(xUnits);
            int y = Mathf.FloorToInt(yUnits);

            if (x < 0 || x >= g.gridSizeX || y < 0 || y >= g.gridSizeY)
                continue;

            gridIndex = i;
            cellX = x;
            cellY = y;
            return true;
        }

        return false;
    }

    public void SetBuildingFootprints(int index)
    {
        buildModeOn = true;
        currentFootprintIndex = index;
    }
    public void ClearBuildingFootprints()
    {
        buildModeOn = false;
        currentFootprintIndex = 0;
    }

    private void HandleZoomInput()
    {
        if (!useRawImageInput) return;
        if (!gridRawImage) return;
        if (!buildingCamera) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f)) return;

        RectTransform rt = gridRawImage.rectTransform;

        Camera eventCam = null;
        if (uiCanvas && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCam = uiCanvas.worldCamera;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, eventCam))
            return;

        var lens = buildingCamera.Lens;
        lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize - scroll * zoomSensitivity, minZoomDistance, maxZoomDistance);
        buildingCamera.Lens = lens;
    }
    private void HandlePanInput()
    {
        if (!useRawImageInput) return;
        if (!gridRawImage) return;
        if (!buildingCamera) return;

        if (!buildingCameraPositionComposer) return;

        RectTransform rt = gridRawImage.rectTransform;

        Camera eventCam = null;
        if (uiCanvas && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCam = uiCanvas.worldCamera;

        bool over = RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, eventCam);

        if (Input.GetMouseButtonDown(1) && over)
        {
            _isPanning = true;
            _panStartMousePos = Input.mousePosition;
            _panStartTargetOffset = buildingCameraPositionComposer.TargetOffset;
        }

        if (Input.GetMouseButtonUp(1))
            _isPanning = false;

        if (!_isPanning) return;

        Vector2 cur = Input.mousePosition;
        Vector2 delta = cur - _panStartMousePos;

        Vector3 off = _panStartTargetOffset;
        off.x += delta.x * panSensitivity;
        off.z += delta.y * panSensitivity;

        buildingCameraPositionComposer.TargetOffset = off;
    }

    private Vector3 GetFootprintCenterWorld(BuildingGrid grid, int gridIndex, int anchorX, int anchorY, BoolMatrix fp, int rotStep)
    {
        int srcW = fp.width;
        int srcH = fp.height;

        int dstW = (rotStep % 2 == 0) ? srcW : srcH;
        int dstH = (rotStep % 2 == 0) ? srcH : srcW;

        float centerOffsetX = (dstW - 1) * 0.5f;
        float centerOffsetY = (dstH - 1) * 0.5f;

        Vector3 anchorWorld = grid.GetCellWorldCenter(gridIndex, anchorX, anchorY);
        float cellSize = grid.grids[gridIndex].cellSize;

        Transform t = grid.transform;
        return anchorWorld + (t.right * (centerOffsetX * cellSize)) + (t.forward * (centerOffsetY * cellSize));
    }
        // -----  GHOST HELPERS  ---------------------------------------------------
    private bool TryGetShipRoots(out Transform realRoot, out Transform ghostRoot)
    {
        realRoot = null; ghostRoot = null;

        var nav = LandshipNavigation.Instance;
        if (nav == null) return false;

        realRoot  = nav.core? nav.core : null;
        ghostRoot = nav.ghostShip? nav.ghostShip : null;

        return realRoot && ghostRoot;
    }

    private Vector3 ProjectPointToGhost(Transform realRoot, Transform ghostRoot, Vector3 worldPos)
    {
        Vector3 local = realRoot.InverseTransformPoint(worldPos);
        return ghostRoot.TransformPoint(local);
    }

    private Quaternion ProjectRotToGhost(Transform realRoot, Transform ghostRoot, Quaternion worldRot)
    {
        Quaternion localRot = Quaternion.Inverse(realRoot.rotation) * worldRot;
        return ghostRoot.rotation * localRot;
    }
}