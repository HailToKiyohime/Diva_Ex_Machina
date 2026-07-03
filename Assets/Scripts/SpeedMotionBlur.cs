using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeedMotionBlur : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Volume volume;   // 拖你那顆 Global Volume 進來

    [Header("Dash Motion Blur")]
    [Tooltip("dash 時的模糊強度")]
    [Range(0f, 1f)][SerializeField] private float dashIntensity = 0.8f;
    [Tooltip("進入模糊的速度（越大衝進去越快）")]
    [SerializeField] private float blurInLerp = 20f;
    [Tooltip("退出模糊的速度（越小尾巴拖越長）")]
    [SerializeField] private float blurOutLerp = 8f;

    private MotionBlur _mb;

    void Start()
    {
        if (volume != null && volume.profile != null)
            volume.profile.TryGet(out _mb);
    }

    void LateUpdate()
    {
        if (_mb == null || playerMovement == null) return;

        // dash 期間 = 目標強度，其餘 = 0
        bool dashing = playerMovement.IsDashActive;
        float target = dashing ? dashIntensity : 0f;

        // 進場快、退場慢，尾巴比較好看
        float lerp = dashing ? blurInLerp : blurOutLerp;
        _mb.intensity.value = Mathf.Lerp(_mb.intensity.value, target, lerp * Time.deltaTime);
    }
}