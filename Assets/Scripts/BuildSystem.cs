using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;
    public LayerMask buildableLayers;
    public float maxDistance = 500f;

    [Header("Gizmo Highlight")]
    public bool drawHitCellGizmo = true;
    public float gizmoYOffset = 0.01f; // lift a bit to avoid z-fighting

    // cached hit cell
    private BuildingGrid _hitGrid;
    private int _hitGridIndex = -1;
    private int _hitCellX = -1;
    private int _hitCellY = -1;
    private bool _hasHitCell = false;

    // reduce log spam
    private BuildingGrid _lastGrid;
    private int _lastGridIndex = -1;
    private int _lastCellX = int.MinValue;
    private int _lastCellY = int.MinValue;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    private void Update()
    {
        _hasHitCell = false;

        if (!cam) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, buildableLayers))
            return;

        var grid = hit.collider.GetComponentInParent<BuildingGrid>();
        if (!grid) return;
        if (grid.grids == null || grid.grids.Length == 0) return;

        if (TryGetCellFromHit(grid, hit.point, out int gridIndex, out int cellX, out int cellY))
        {
            _hasHitCell = true;
            _hitGrid = grid;
            _hitGridIndex = gridIndex;
            _hitCellX = cellX;
            _hitCellY = cellY;

            bool changed =
                grid != _lastGrid ||
                gridIndex != _lastGridIndex ||
                cellX != _lastCellX ||
                cellY != _lastCellY;

            if (changed)
            {
                _lastGrid = grid;
                _lastGridIndex = gridIndex;
                _lastCellX = cellX;
                _lastCellY = cellY;

                Debug.Log($"GridIndex: {gridIndex}, CellX: {cellX}, CellY: {cellY}", grid);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawHitCellGizmo) return;
        if (!_hasHitCell) return;
        if (!_hitGrid) return;
        if (_hitGrid.grids == null || _hitGridIndex < 0 || _hitGridIndex >= _hitGrid.grids.Length) return;

        var g = _hitGrid.grids[_hitGridIndex];
        if (g.cellSize <= 0f || g.gridSizeX <= 0 || g.gridSizeY <= 0) return;
        if (_hitCellX < 0 || _hitCellX >= g.gridSizeX || _hitCellY < 0 || _hitCellY >= g.gridSizeY) return;

        Transform t = _hitGrid.transform;
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
            origin + right * (_hitCellX * g.cellSize) + forward * (_hitCellY * g.cellSize);

        Vector3 p0 = cellMin + up * gizmoYOffset;
        Vector3 p1 = cellMin + right * g.cellSize + up * gizmoYOffset;
        Vector3 p2 = cellMin + right * g.cellSize + forward * g.cellSize + up * gizmoYOffset;
        Vector3 p3 = cellMin + forward * g.cellSize + up * gizmoYOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);

        Vector3 c = (p0 + p2) * 0.5f;
        float m = Mathf.Min(g.cellSize * 0.2f, 0.5f);
        Gizmos.DrawLine(c - right * m, c + right * m);
        Gizmos.DrawLine(c - forward * m, c + forward * m);
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
