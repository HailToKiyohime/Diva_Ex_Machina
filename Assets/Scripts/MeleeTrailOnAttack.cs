using UnityEngine;

public class MeleeTrailOnAttack : MonoBehaviour
{
    [Header("Auto Find")]
    [SerializeField] private PlayerAnimation playerAnimation;

    [Header("What to Toggle")]
    [Tooltip("手動拖進要控制的 ParticleSystem（Snow / haze / trail 粒子等）")]
    [SerializeField] private ParticleSystem[] particles;

    private void Awake()
    {
        if (playerAnimation == null)
            playerAnimation = GetComponentInParent<PlayerAnimation>(true);

        SetEffectActive(false, clear: true);
    }

    private void OnEnable()
    {
        if (playerAnimation == null) return;
        playerAnimation.OnStartAttacking += HandleStartAttack;
        playerAnimation.OnStopAttacking += HandleStopAttack;
    }

    private void OnDisable()
    {
        if (playerAnimation == null) return;
        playerAnimation.OnStartAttacking -= HandleStartAttack;
        playerAnimation.OnStopAttacking -= HandleStopAttack;
    }

    private void HandleStartAttack() => SetEffectActive(true, clear: false);
    private void HandleStopAttack() => SetEffectActive(false, clear: true);

    private void SetEffectActive(bool on, bool clear)
    {
        if (particles == null) return;

        foreach (var ps in particles)
        {
            if (ps == null) continue;

            if (on)
            {
                ps.Play(true);
            }
            else
            {
                ps.Stop(true, clear
                    ? ParticleSystemStopBehavior.StopEmittingAndClear
                    : ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
