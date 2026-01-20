using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ShoulderAimConstraintWeightDriver : MonoBehaviour
{
    [Header("Auto Find (optional)")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private MultiAimConstraint aimConstraint;

    [Header("Config")]
    [SerializeField] private bool isLeft = true;
    [Tooltip("0 = instant. Higher = faster blend.")]
    [SerializeField] private float blendSpeed = 0f;

    private const string LeftParam = "leftShoulderAttacking";
    private const string RightParam = "rightShoulderAttacking";

    private void Awake()
    {
        if (aimConstraint == null)
            aimConstraint = GetComponent<MultiAimConstraint>();

        if (playerAnimation == null)
            playerAnimation = GetComponentInParent<PlayerAnimation>(true);
    }

    private void OnDisable()
    {
        // 保險：停用時不要留下 weight=1
        if (aimConstraint != null) aimConstraint.weight = 0f;
    }

    private void Update()
    {
        if (aimConstraint == null) return;
        if (playerAnimation == null || playerAnimation.anim == null) return;

        bool attacking = playerAnimation.anim.GetBool(isLeft ? LeftParam : RightParam);
        float target = attacking ? 1f : 0f;

        if (blendSpeed <= 0f)
        {
            aimConstraint.weight = target;
        }
        else
        {
            aimConstraint.weight = Mathf.MoveTowards(
                aimConstraint.weight,
                target,
                blendSpeed * Time.deltaTime
            );
        }
    }
}
