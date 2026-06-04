using UnityEngine;

/// <summary>
/// ���b�ΡG���ԧ������u�����v�q Animator Event �ѽ��X�ӡC
/// �ت��G�N�� Combo2 �S���\����B�� Animation Event �|���Aattacking �]���|�d���C
/// 
/// �Ϊk�G��o�ӱ��b Player�]�Φ� Animator/PlayerAnimation ���P�@�� prefab�^�W�Y�i�C
/// ���ݭn��A�{�� PlayerMovement / PlayerController ���I�s�y�{�C
/// </summary>
public class MeleeAttackLifecycleGuard : MonoBehaviour
{
    [Header("Auto Find")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private Animator anim;

    [Header("Animator Params")]
    [SerializeField] private string attackingBoolName = "attacking";
    [SerializeField] private string leftComboIntName = "LeftHandCombo";
    [SerializeField] private string rightComboIntName = "RightHandCombo";

    [Header("Layer / State (optional)")]
    //[Tooltip("�u�b�o�� layer �ʱ� normalizedTime�]�d�� = �� layer 0�^")]
    [SerializeField] private string meleeLayerNameContains = "Melee";
    //[Tooltip("�P�w�ʵe������ normalizedTime �H�ȡC0.98 ����O�u�C")]
    [Range(0.5f, 1f)]
    [SerializeField] private float endNormalized = 0.98f;

    [Header("Failsafe")]    
    //[Tooltip("�������A�̪����\�h�[�]��^�C�W�L�N�j�� Stop�A�קK�d���C")]
    [SerializeField] private float maxAttackSeconds = 2.0f;
    //[Tooltip("�� combo2 �Q�ƶ����@���S�i�J�]������ѡ^�ɡA���\�h�[�A�j����C")]
    [SerializeField] private float queuedComboFailSafeSeconds = 1.0f;

    private int _meleeLayerIndex = 0;
    private int _hashAttacking;
    private int _hashLeftCombo;
    private int _hashRightCombo;

    private bool _wasAttacking = false;
    private float _attackStartTime = 0f;
    private float _comboQueuedTime = 0f;   

    private void Awake()
    {
        if (playerAnimation == null) playerAnimation = GetComponentInChildren<PlayerAnimation>(true);
        if (anim == null) anim = (playerAnimation != null) ? playerAnimation.GetComponent<Animator>() : null;
        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        _hashAttacking = Animator.StringToHash(attackingBoolName);
        _hashLeftCombo = Animator.StringToHash(leftComboIntName);
        _hashRightCombo = Animator.StringToHash(rightComboIntName);

        _meleeLayerIndex = 0;
        if (anim != null && !string.IsNullOrEmpty(meleeLayerNameContains))
        {
            for (int i = 0; i < anim.layerCount; i++)
            {
                string ln = anim.GetLayerName(i);
                if (!string.IsNullOrEmpty(ln) && ln.Contains(meleeLayerNameContains))
                {
                    _meleeLayerIndex = i;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (anim == null || playerAnimation == null) return;

        bool attacking = anim.GetBool(_hashAttacking);

        // rising edge�G�}�l����
        if (attacking && !_wasAttacking)
        {
            _attackStartTime = Time.time;
            _comboQueuedTime = 0f;
        }

        // falling edge�G��������]�~���w�����^
        if (!attacking && _wasAttacking)
        {
            _comboQueuedTime = 0f;
        }

        _wasAttacking = attacking;

        if (!attacking) return;

        // Ū combo ���A�]>=2 ��ܷQ�� combo2 / �Τw�b combo2�^
        int leftCombo = anim.GetInteger(_hashLeftCombo);
        int rightCombo = anim.GetInteger(_hashRightCombo);
        bool wantsCombo2 = (leftCombo >= 2) || (rightCombo >= 2);

        // �O���u�Ĥ@���Q�ƶ��v���ɶ��A�Ω󰻴��������
        if (wantsCombo2 && _comboQueuedTime <= 0f)
            _comboQueuedTime = Time.time;

        // === Failsafe 1�G��q�����W�� ===
        if (Time.time - _attackStartTime > maxAttackSeconds)
        {
            ForceStop("maxAttackSeconds");
            return;
        }

        // === Failsafe 2�G�w�ƶ� combo2 ���Ӥ[�����i�J�]�`���� spin input / transition miss�^===
        // �p�G combo2 �w�ƶ��A�ӥB�W�L�@�w�ɶ��A���ثe���A���G�����i�J�ĤG�q�A�N�j����קK�d���C
        if (wantsCombo2 && _comboQueuedTime > 0f && (Time.time - _comboQueuedTime) > queuedComboFailSafeSeconds)
        {
            // �ɶq�P�_�u���b�Ĥ@�q�v�GnormalizedTime �w���񵲧��A���٦b�P�@�� state�]�ΨS�����\�i�J�U�@�q�^
            AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(_meleeLayerIndex);
            bool nearEnd = st.normalizedTime >= endNormalized;
            bool transitioning = anim.IsInTransition(_meleeLayerIndex);

            if (nearEnd && !transitioning)
            {
                // �M combo�A�M�᦬���A�קK�U�@���Q�d��
                anim.SetInteger(_hashLeftCombo, 0);
                anim.SetInteger(_hashRightCombo, 0);
                ForceStop("queuedComboFailSafe");
                return;
            }
        }

        // === ���`�����G�ʵe�����B�S���A�ƶ��U�@�q ===
        AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(_meleeLayerIndex);
        if (!anim.IsInTransition(_meleeLayerIndex) && s.normalizedTime >= endNormalized)
        {
            // �Y�S���Q�� combo2�A�N����
            if (!wantsCombo2)
            {
                ForceStop("normalEnd");
                return;
            }

            // wantsCombo2 ���]�w����ݡB�B�S���i�J��� �� �ܥi����� miss�F�浹 failsafe 2 �Ϊ�������
            // �o�̫O�u�G�Y�w����ݥB wantsCombo2�A�� failsafe 2 ���ɶ���A�����]�קK�~����n�n�i��������p�^
        }
    }

    private void ForceStop(string reason)
    {
        // �o�̤��n�̿� Animation Event�F������ PlayerAnimation �� StopAttacking()�]�|���K SetOffAttackLayer & OnStopAttacking�^
        Debug.LogWarning($"MeleeAttackLifecycleGuard: ForceStop ({reason})");
        playerAnimation.StopAttacking();
    }
}
