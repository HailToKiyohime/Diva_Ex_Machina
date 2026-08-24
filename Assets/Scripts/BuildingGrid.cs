using UnityEngine;

public class BuildingGrid : MonoBehaviour
{
    [System.Serializable]
    public class GridData
    {
        public float cellSize = 1f;
        public int gridSizeX = 10;
        public int gridSizeY = 10;
        public float gridHeight = 0f;

        // offset in "cell size"
        public float centreOffsetX = 0;
        public float centreOffsetY = 0;

        // occupancy, true = occupied (unusable)
        public BoolMatrix occupied;
    }

    public GridData[] grids;

    [Header("Gizmos")]
    public bool drawGrid = true;
    public bool drawOccupiedCells = true;
    public float gizmoYOffset = 0.01f;

    public void EnsureOccupancy(int gridIndex)
    {
        if (grids == null || gridIndex < 0 || gridIndex >= grids.Length) return;

        var g = grids[gridIndex];
        if (g.gridSizeX <= 0 || g.gridSizeY <= 0) return;

        if (g.occupied == null)
            g.occupied = new BoolMatrix();

        if (g.occupied.cells == null || g.occupied.width != g.gridSizeX || g.occupied.height != g.gridSizeY)
            g.occupied.Resize(g.gridSizeX, g.gridSizeY);
    }

    public bool IsOccupied(int gridIndex, int x, int y)
    {
        if (grids == null || gridIndex < 0 || gridIndex >= grids.Length) return true;

        var g = grids[gridIndex];
        if (x < 0 || x >= g.gridSizeX || y < 0 || y >= g.gridSizeY) return true;

        EnsureOccupancy(gridIndex);
        return g.occupied.Get(x, y);
    }

    public void SetOccupied(int gridIndex, int x, int y, bool value)
    {
        if (grids == null || gridIndex < 0 || gridIndex >= grids.Length) return;

        var g = grids[gridIndex];
        if (x < 0 || x >= g.gridSizeX || y < 0 || y >= g.gridSizeY) return;

        EnsureOccupancy(gridIndex);
        g.occupied.Set(x, y, value);
    }

    // anchorX/Y = hit cell, footprint (0,0) will map onto (anchorX, anchorY)
    public bool CanPlaceFootprint(int gridIndex, int anchorX, int anchorY, BoolMatrix footprint, out bool anyOutOfBounds)
    {
        anyOutOfBounds = false;

        if (footprint == null || footprint.cells == null || footprint.width <= 0 || footprint.height <= 0)
            return false;

        if (grids == null || gridIndex < 0 || gridIndex >= grids.Length)
            return false;

        var g = grids[gridIndex];
        EnsureOccupancy(gridIndex);

        bool anyCell = false;

        for (int fy = 0; fy < footprint.height; fy++)
        {
            for (int fx = 0; fx < footprint.width; fx++)
            {
                if (!footprint.Get(fx, fy)) continue;

                anyCell = true;

                int gx = anchorX + fx;
                int gy = anchorY + fy;

                if (gx < 0 || gx >= g.gridSizeX || gy < 0 || gy >= g.gridSizeY)
                {
                    anyOutOfBounds = true;
                    return false;
                }

                if (g.occupied.Get(gx, gy))
                    return false;
            }
        }

        return anyCell;
    }

    public void PlaceFootprint(int gridIndex, int anchorX, int anchorY, BoolMatrix footprint)
    {
        if (footprint == null || footprint.cells == null) return;
        if (grids == null || gridIndex < 0 || gridIndex >= grids.Length) return;

        EnsureOccupancy(gridIndex);

        for (int fy = 0; fy < footprint.height; fy++)
        {
            for (int fx = 0; fx < footprint.width; fx++)
            {
                if (!footprint.Get(fx, fy)) continue;

                int gx = anchorX + fx;
                int gy = anchorY + fy;

                SetOccupied(gridIndex, gx, gy, true);
            }
        }
    }

    public Vector3 GetCellWorldCenter(int gridIndex, int cellX, int cellY)
    {
        var g = grids[gridIndex];

        Transform t = transform;
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

        return cellMin + right * (g.cellSize * 0.5f) + forward * (g.cellSize * 0.5f) + up * gizmoYOffset;
    }

    private void OnDrawGizmos()
    {
        if (grids == null) return;

        for (int gi = 0; gi < grids.Length; gi++)
        {
            var g = grids[gi];
            if (g.cellSize <= 0f || g.gridSizeX <= 0 || g.gridSizeY <= 0) continue;

            Transform t = transform;
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

            if (drawGrid)
            {
                Gizmos.color = Color.gray;

                for (int x = 0; x <= g.gridSizeX; x++)
                {
                    Vector3 a = origin + right * (x * g.cellSize) + up * gizmoYOffset;
                    Vector3 b = a + forward * (depth);
                    Gizmos.DrawLine(a, b);
                }

                for (int y = 0; y <= g.gridSizeY; y++)
                {
                    Vector3 a = origin + forward * (y * g.cellSize) + up * gizmoYOffset;
                    Vector3 b = a + right * (width);
                    Gizmos.DrawLine(a, b);
                }
            }

            if (drawOccupiedCells)
            {
                EnsureOccupancy(gi);

                Gizmos.color = new Color(1f, 0f, 0f, 0.35f);

                for (int y = 0; y < g.gridSizeY; y++)
                {
                    for (int x = 0; x < g.gridSizeX; x++)
                    {
                        if (!g.occupied.Get(x, y)) continue;

                        Vector3 c = GetCellWorldCenter(gi, x, y);
                        Vector3 size = right * (g.cellSize) + forward * (g.cellSize);
                        Gizmos.DrawCube(c, new Vector3(
                            Mathf.Abs(Vector3.Dot(size, Vector3.right)),
                            0.001f,
                            Mathf.Abs(Vector3.Dot(size, Vector3.forward))
                        ));
                    }
                }
            }
        }
    }
}
