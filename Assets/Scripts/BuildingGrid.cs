using System.Collections.Generic;
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

    // ── Ownership（執行期資料，不序列化）─────────────────────────────
    //
    // occupied 只是一張 bool 表，沒有記錄「是誰佔的」，所以建築被摧毀時
    // 沒人知道該把哪幾格還回來。這裡替每個佔用者記下它的格子，
    // 釋放時直接照著清單還 —— 不需要把 anchor / footprint / 旋轉再反推一次，
    // 放置與釋放看的是同一份資料，兩邊不可能對不上。
    //
    // 放在 BuildingGrid 上而不是建築上：格子是 grid 的資產，
    // 佔用與釋放都由 grid 自己負責，建築只要報上自己的名字。
    //
    // 不序列化：場上的建築本來就只在 Play Mode 存在，離開 Play Mode
    // 這份資料跟著消失才是對的。

    private readonly Dictionary<GameObject, OwnedCells> _owners = new Dictionary<GameObject, OwnedCells>();

    /// <summary>目前啟用中的所有 grid。給 ReleaseFromAnyGrid 用，建築不必自己找 grid。</summary>
    private static readonly List<BuildingGrid> _activeGrids = new List<BuildingGrid>();

    private class OwnedCells
    {
        public int gridIndex;
        public readonly List<Vector2Int> cells = new List<Vector2Int>();
    }

    private void OnEnable()
    {
        if (!_activeGrids.Contains(this)) _activeGrids.Add(this);
    }

    private void OnDisable()
    {
        _activeGrids.Remove(this);
    }

    /// <summary>
    /// 把一格標成佔用，並記下佔用者。owner 為 null 時等同 SetOccupied(..., true) ——
    /// 沒有佔用者的格子（例如地形本來就不能蓋的區域）永遠不會被釋放。
    /// </summary>
    public void OccupyCell(int gridIndex, int x, int y, GameObject owner)
    {
        SetOccupied(gridIndex, x, y, true);

        if (owner == null) return;

        if (!_owners.TryGetValue(owner, out OwnedCells owned))
        {
            owned = new OwnedCells { gridIndex = gridIndex };
            _owners[owner] = owned;
        }

        owned.cells.Add(new Vector2Int(x, y));
    }

    /// <summary>
    /// 把某個佔用者的格子全部還回來。沒登記過的 owner 是 no-op，
    /// 所以重複呼叫、或對事先擺在場景裡的建築呼叫，都不會出問題。
    /// </summary>
    public void ReleaseOwner(GameObject owner)
    {
        if (owner == null) return;
        if (!_owners.TryGetValue(owner, out OwnedCells owned)) return;

        for (int i = 0; i < owned.cells.Count; i++)
            SetOccupied(owned.gridIndex, owned.cells[i].x, owned.cells[i].y, false);

        _owners.Remove(owner);
    }

    /// <summary>
    /// 在所有啟用中的 grid 上釋放這個佔用者。
    /// 建築不需要記住自己是被哪一個 grid 收下的，死亡時呼叫這個就好。
    /// </summary>
    public static void ReleaseFromAnyGrid(GameObject owner)
    {
        if (owner == null) return;

        for (int i = 0; i < _activeGrids.Count; i++)
        {
            if (_activeGrids[i] != null)
                _activeGrids[i].ReleaseOwner(owner);
        }
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
