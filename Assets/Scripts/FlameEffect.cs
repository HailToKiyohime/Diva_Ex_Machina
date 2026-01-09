using UnityEngine;

public class FlameEffect : MonoBehaviour
{
    public enum FlameKind
    {
        Normal,
        Boosted,
        Melee
    }
    [Header("Dash VFX")]
    [SerializeField] private float dashOffDelay = 0.2f; // 主人要的延遲時間
    private float _dashLingerUntil = 0f;
    [Header("Type")]
    [SerializeField] private FlameKind flameKind = FlameKind.Normal;

    [Header("Auto Find")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("What to Toggle")]
    [Tooltip("拖入此火焰要控制的所有 ParticleSystem（同一組一起開關）")]
    [SerializeField] private ParticleSystem[] particles;

    [Header("Tuning")]
    [Tooltip("下落判定：y 速度低於此值才算 falling，避免微小抖動切換")]
    [SerializeField] private float fallingVelocityThreshold = -0.1f;

    private bool _isOn;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>(true);

        SetEffectActive(false, clear: true);
        _isOn = false;
    }

    private void OnEnable()
    {
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>(true);

        // 啟用時先關一次，避免 prefab PlayOnAwake 造成剛生成就亮錯
        SetEffectActive(false, clear: true);
        _isOn = false;
    }

    private void Update()
    {
        if (playerMovement == null)
        {
            // 角色層級若變動，給一次自動補救
            playerMovement = GetComponentInParent<PlayerMovement>(true);
            if (playerMovement == null) { EnsureOff(); return; }
        }

        // 狀態
        bool grounded = playerMovement.IsGrounded;
        bool flying = playerMovement.IsFlyingActive;

        bool dash = playerMovement.IsDashActive;
        bool meleeDash = playerMovement.IsMeleeDashActive;
        if (dash)
        {
            _dashLingerUntil = Time.time + dashOffDelay;
        }
        bool dashWithLinger = dash || Time.time < _dashLingerUntil;

        float vy = playerMovement.VerticalVelocity;
        bool falling = !grounded && !flying && vy < fallingVelocityThreshold;

        // 規矩（優先級由高到低）：
        // 1) Dash / MeleeDash：只開 Melee flame
        // 2) 地上：全關
        // 3) 飛行：只開 Boosted flame
        // 4) 下落：只開 Normal flame
        bool shouldOn = false;

        if (dashWithLinger || meleeDash)
        {
            shouldOn = (flameKind == FlameKind.Melee);
        }
        else if (grounded)
        {
            shouldOn = false;
        }
        else if (flying)
        {
            shouldOn = (flameKind == FlameKind.Boosted);
        }
        else if (falling)
        {
            shouldOn = (flameKind == FlameKind.Normal);
        }
        else
        {
            shouldOn = false;
        }

        ApplyDesired(shouldOn);
    }

    private void ApplyDesired(bool on)
    {
        if (_isOn == on) return;
        _isOn = on;

        // off 時 clear，避免殘留
        SetEffectActive(on, clear: !on);
    }

    private void EnsureOff()
    {
        if (!_isOn) return;
        _isOn = false;
        SetEffectActive(false, clear: true);
    }

    private void SetEffectActive(bool on, bool clear)
    {
        if (particles == null) return;

        foreach (var ps in particles)
        {
            if (ps == null) continue;

            if (on)
            {
                if (!ps.isPlaying) ps.Play(true);
            }
            else
            {
                if (ps.isPlaying)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                else if (clear)
                {
                    //ps.Clear(true);
                }
            }
        }
    }
}
