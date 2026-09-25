using UnityEngine;

/// <summary>
/// Simple blob shadow: a flat quad under the target, found by raycasting
/// straight down. It sits on the hit surface (tilted to its normal), and
/// shrinks + fades as the target climbs, so the player can still read their
/// height while flying.
///
/// Put this on a child GameObject that has a MeshFilter (Quad) and a
/// MeshRenderer with the BlobShadow material. The easiest way is
/// Tools > Diva Ex Machina > Add Blob Shadow To Player, which sets all of it up.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[DisallowMultipleComponent]
public class BlobShadow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object casting the shadow. Defaults to the parent.")]
    [SerializeField] private Transform target;

    [Tooltip("Distance from the target's pivot down to its feet. " +
             "Negative = auto-detect from a CapsuleCollider on the target.")]
    [SerializeField] private float pivotToFeet = -1f;

    [Header("Ground Detection")]
    [Tooltip("Surfaces the shadow can land on. Keep the Player layer out of this.")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float maxDistance = 80f;
    [Tooltip("Lift off the surface to avoid z-fighting.")]
    [SerializeField] private float surfaceOffset = 0.03f;

    [Header("Look")]
    [Tooltip("Shadow diameter (metres) when standing on the ground.")]
    [SerializeField] private float groundSize = 1.0f;
    [SerializeField] private Color shadowColor = new Color(0.04f, 0.04f, 0.1f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float groundOpacity = 0.55f;

    [Header("Height Falloff")]
    [Tooltip("Height above ground where the shadow starts shrinking/fading.")]
    [SerializeField] private float fadeStartHeight = 1f;
    [Tooltip("Height above ground where the shadow reaches its minimum.")]
    [SerializeField] private float fadeEndHeight = 40f;
    [Range(0f, 1f)]
    [SerializeField] private float sizeAtFadeEnd = 0.45f;
    [Range(0f, 1f)]
    [SerializeField] private float opacityAtFadeEnd = 0.15f;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>Height of the target's feet above the ground this frame (or -1 if no ground found).</summary>
    public float HeightAboveGround { get; private set; } = -1f;

    private void Awake()
    {
        if (target == null) target = transform.parent;
        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();

        if (pivotToFeet < 0f)
            pivotToFeet = DetectPivotToFeet();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 origin = target.position;
        bool hitSomething = Physics.Raycast(
            origin, Vector3.down, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (!hitSomething)
        {
            HeightAboveGround = -1f;
            meshRenderer.enabled = false;
            return;
        }

        HeightAboveGround = Mathf.Max(0f, hit.distance - pivotToFeet);
        float k = Mathf.InverseLerp(fadeStartHeight, fadeEndHeight, HeightAboveGround);
        float size = groundSize * Mathf.Lerp(1f, sizeAtFadeEnd, k);
        float opacity = groundOpacity * Mathf.Lerp(1f, opacityAtFadeEnd, k);

        if (opacity <= 0.001f)
        {
            meshRenderer.enabled = false;
            return;
        }
        meshRenderer.enabled = true;

        // Lie flat on the surface. Unity's Quad faces -Z, so point -Z along the surface normal.
        Vector3 n = hit.normal;
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, n);
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(Vector3.forward, n);

        transform.SetPositionAndRotation(
            hit.point + n * surfaceOffset,
            Quaternion.LookRotation(-n, forward));
        transform.localScale = Vector3.one;
        SetWorldScale(size);

        Color c = shadowColor;
        c.a = opacity;
        block.SetColor(BaseColorId, c);
        meshRenderer.SetPropertyBlock(block);
    }

    private void SetWorldScale(float size)
    {
        // Counteract any parent scale so 'size' is in world metres.
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            size / Mathf.Max(1e-4f, Mathf.Abs(parentScale.x)),
            size / Mathf.Max(1e-4f, Mathf.Abs(parentScale.y)),
            size / Mathf.Max(1e-4f, Mathf.Abs(parentScale.z)));
    }

    private float DetectPivotToFeet()
    {
        var capsule = target != null ? target.GetComponent<CapsuleCollider>() : null;
        if (capsule == null || capsule.direction != 1) return 0f;

        float bottomLocal = capsule.center.y - capsule.height * 0.5f;
        return Mathf.Max(0f, -bottomLocal * target.lossyScale.y);
    }
}
