using UnityEngine;

public class DashGhostFader : MonoBehaviour
{
    private Material _mat;
    private Mesh _mesh;
    private float _life;
    private float _elapsed;
    private Color _startColor;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public void Init(Material mat, Mesh mesh, Color color, float life)
    {
        _mat = mat;
        _mesh = mesh;
        _startColor = color;
        _life = Mathf.Max(0.01f, life);
        ApplyColor(color);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float k = Mathf.Clamp01(_elapsed / _life);

        Color c = _startColor;
        c.a = _startColor.a * (1f - k); // 線性淡出；想尾段更快可用 (1f - k*k)
        ApplyColor(c);

        if (k >= 1f)
            Destroy(gameObject); // 清理交給 OnDestroy，涵蓋所有銷毀路徑
    }

    void OnDestroy()
    {
        // baked mesh 與 material instance 都是 Unity 物件，不清會洩漏
        if (_mat != null) Destroy(_mat);
        if (_mesh != null) Destroy(_mesh);
    }

    private void ApplyColor(Color c)
    {
        if (_mat == null) return;
        if (_mat.HasProperty(BaseColorId)) _mat.SetColor(BaseColorId, c);
        else _mat.color = c;
    }
}