using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;
    public LayerMask buildableLayers;
    public float maxDistance = 500f;

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

    private void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    private void Update()
    {
        UpdateHitCell();
        HandleRotateInput();

        if (enablePreview) UpdatePreview();
        else DestroyPreview();

        if (placeOnLeftClick && Input.GetMouseButtonDown(0))
            TryPlaceCurrentBlueprint();
    }

    private void HandleRotateInput()
    {
        if (!enableRotation) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        int dir = scroll > 0f ? 1 : -1;
        int steps = Mathf.RoundToInt(Mathf.Abs(scroll) * scrollSensitivity);
        if (steps < 1) steps = 1;

        rotationStep = Mod(rotationStep + dir * steps, 4);
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

        if (!cam) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

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

        ApplyFootprintOccupancyRotated(_hitGrid, _hitGridIndex, _hitCellX, _hitCellY, fp, rotationStep);

        Vector3 pos = _hitGrid.GetCellWorldCenter(_hitGridIndex, _hitCellX, _hitCellY);

        Quaternion baseRot = placeAlignToGridRotation ? _hitGrid.transform.rotation : Quaternion.identity;
        Quaternion rot = baseRot * Quaternion.Euler(0f, rotationStep * 90f, 0f);

        Transform parent = placeParentToGrid ? _hitGrid.transform : null;

        GameObject placed = Instantiate(bp.buildingPrefab, pos, rot, parent);

        if (bp.buildingMaterial != null)
            ApplyMaterialToAllRenderers(placed, bp.buildingMaterial);
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

        Vector3 pos = _hitGrid.GetCellWorldCenter(_hitGridIndex, _hitCellX, _hitCellY);

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
}