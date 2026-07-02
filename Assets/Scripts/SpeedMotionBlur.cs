using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeedMotionBlur : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Volume volume;   // 拖你那顆 Global Volume 進來

    [Header("Speed → Intensity")]
    [Tooltip("相對速度低於這個(m/s)就完全不模糊。站船上不動 => ≈0 => 0 模糊")]
    [SerializeField] private float minSpeed = 1.5f;
    [Tooltip("達到這個相對速度 = maxIntensity。60km/h ≈ 16.7 m/s")]
    [SerializeField] private float maxSpeed = 16f;
    [Range(0f, 1f)][SerializeField] private float maxIntensity = 0.8f;
    [Tooltip("Intensity 變化平滑度")]
    [SerializeField] private float lerpSpeed = 8f;

    private MotionBlur _mb;

    void Start()
    {
        if (volume != null && volume.profile != null)
            volume.profile.TryGet(out _mb);
    }

    void LateUpdate()
    {
        if (_mb == null || playerMovement == null) return;

        float speed = playerMovement.HorizontalSpeedRelativeToPlatform;

        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed); // <minSpeed=0, >maxSpeed=1
        float target = t * maxIntensity;

        _mb.intensity.value = Mathf.Lerp(_mb.intensity.value, target, lerpSpeed * Time.deltaTime);
    }
}