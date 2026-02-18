using UnityEngine;

[ExecuteAlways]
public class BuildingGrid : MonoBehaviour
{
    [System.Serializable]
    public struct GridData
    {
        [Min(0.01f)] public float cellSize;
        [Min(1)] public int gridSizeX;
        [Min(1)] public int gridSizeY;

        // Height offset from transform.position, along transform.up
        public float gridHeight;

        // Center offset in cell units
        public float centreOffsetX; // + along transform.right
        public float centreOffsetY; // + along transform.forward
    }

    [Header("Grids")]
    public GridData[] grids;

    private Vector3 XAxisWorld => transform.right;
    private Vector3 YAxisWorld => transform.forward;

    private void OnDrawGizmos()
    {
        if (grids == null) return;

        for (int i = 0; i < grids.Length; i++)
        {
            DrawGrid(grids[i]);
        }
    }

    private void DrawGrid(GridData g)
    {
        if (g.cellSize <= 0f || g.gridSizeX <= 0 || g.gridSizeY <= 0) return;

        Vector3 center = GetGridCenterWorld(g);
        Vector3 origin = GetGridOriginWorld(g, center);

        // Draw vertical lines
        for (int x = 0; x <= g.gridSizeX; x++)
        {
            Vector3 start = origin + XAxisWorld * (x * g.cellSize);
            Vector3 end = start + YAxisWorld * (g.gridSizeY * g.cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw horizontal lines
        for (int y = 0; y <= g.gridSizeY; y++)
        {
            Vector3 start = origin + YAxisWorld * (y * g.cellSize);
            Vector3 end = start + XAxisWorld * (g.gridSizeX * g.cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw center marker
        float m = Mathf.Min(g.cellSize * 0.25f, 0.5f);
        Gizmos.DrawLine(center, center + XAxisWorld * m);
        Gizmos.DrawLine(center, center + YAxisWorld * m);
        Gizmos.DrawLine(center, center + transform.up * m);
    }

    private Vector3 GetGridCenterWorld(GridData g)
    {
        Vector3 baseCenter = transform.position + transform.up * g.gridHeight;
        Vector3 offset = XAxisWorld * (g.centreOffsetX * g.cellSize)
                       + YAxisWorld * (g.centreOffsetY * g.cellSize);
        return baseCenter + offset;
    }

    private Vector3 GetGridOriginWorld(GridData g, Vector3 center)
    {
        float width = g.gridSizeX * g.cellSize;
        float depth = g.gridSizeY * g.cellSize;

        return center
               - XAxisWorld * (width * 0.5f)
               - YAxisWorld * (depth * 0.5f);
    }

    public Vector3 GetCellCenterWorld(int gridIndex, int x, int y)
    {
        if (grids == null || grids.Length == 0) return transform.position;

        gridIndex = Mathf.Clamp(gridIndex, 0, grids.Length - 1);
        GridData g = grids[gridIndex];

        x = Mathf.Clamp(x, 0, g.gridSizeX - 1);
        y = Mathf.Clamp(y, 0, g.gridSizeY - 1);

        Vector3 center = GetGridCenterWorld(g);
        Vector3 origin = GetGridOriginWorld(g, center);

        return origin
               + XAxisWorld * ((x + 0.5f) * g.cellSize)
               + YAxisWorld * ((y + 0.5f) * g.cellSize);
    }
}
