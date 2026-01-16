using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using TMPro;
public class PlayerAiming : MonoBehaviour
{
    public static PlayerAiming Instance { get; private set; }
    [Header("Crosshair")]
    [SerializeField] private Image AimAreaImage;
    [SerializeField] private Image crosshairImage;
    [Header("Camera")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private GameObject playerOrientation;
    public Vector2 turn;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30f;
    [Header("Aiming Settings")]
    private Vector2 screenCenter;
    [SerializeField] public bool lockOn = false;
    [SerializeField] private Ray ray;
    [SerializeField] private float targetDistance;
    [SerializeField] private Vector3 targetDirection = Vector3.zero;
    [SerializeField] private Transform currentLockedTarget;
    [SerializeField] public Transform aimingPoint;
    [SerializeField] private float centerLerpSpeed = 10f;
    [SerializeField] private float crosshairTiltLerp = 12f;
    [SerializeField] private float resetTiltLerp = 8f;
    [SerializeField] private float lockOnDistance = 25f;
    [Header("UI Speeds")]
    [SerializeField] private float crosshairLerpSpeed = 30f;


    [SerializeField] private Rigidbody currentTargetRb;

    [Header("Aiming Point Smoothing")]
    [SerializeField] private float aimingPointSmoothTime = 0.06f;      // 平時追蹤速度
    [SerializeField] private float aimingPointSwitchSmoothTime = 0.10f; // 切換模式時更柔和
    [SerializeField] private float aimingPointMaxSpeed = 9999f;         // 上限，避免慢到跟不上
    [SerializeField] private float freeAimMaxDistance = 500f;           // 對齊你下面 Raycast 的 maxDistance

    private Vector3 aimingPointVelocity;
    private bool lastLockOn;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCam = mainCam != null ? mainCam : Camera.main;
        // 初始 ray + yaw
        ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (playerOrientation != null)
            turn.x = playerOrientation.transform.rotation.eulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        // Stop aiming & camera rotation when in Equipment / Crafting UI
        if (UIManager.Instance != null && UIManager.Instance.currentCameraSet != 0)
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
        {
            turn.x += mouseX;
            turn.y -= mouseY;
            turn.x = ClampAngle(turn.x, float.MinValue, float.MaxValue);
            turn.y = ClampAngle(turn.y, BottomClamp, TopClamp);

            if (playerOrientation != null)
                playerOrientation.transform.rotation = Quaternion.Euler(turn.y, turn.x, 0f);
        }
        CrosshairDetect();
        
    }
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
    private void CrosshairDetect()
    {
        List<GameObject> enemies;
        screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        GameObject closestEnemy = null;
        Vector2 closestScreenPoint = Vector2.zero;
        float closestProximity = Mathf.Infinity;
        enemies = GameManager.Instance.GetEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject enemy = enemies[i];
            if (!enemy) continue;
            Vector3 sp = mainCam.WorldToScreenPoint(enemy.transform.position);
            if (sp.z <= 0f) continue;
            Vector2 pt = new(sp.x, sp.y);
            float prox = Vector2.Distance(pt, screenCenter);
            if (prox < closestProximity)
            {
                closestProximity = prox;
                closestScreenPoint = pt;
                closestEnemy = enemy;
            }
        }
        bool isVisible = false;
        if (closestEnemy)
        {
            targetDirection = (closestEnemy.transform.position - playerOrientation.transform.position).normalized;
            currentTargetRb = closestEnemy.GetComponentInParent<Rigidbody>();
            Renderer r = closestEnemy.GetComponentInChildren<Renderer>();
            if (r) isVisible = r.isVisible;
        }
        else
        {
            targetDirection = Vector3.zero;
            closestProximity = Mathf.Infinity;
        }
        targetDistance = closestEnemy
            ? Vector3.Distance(playerOrientation.transform.position, closestEnemy.transform.position)
            : Mathf.Infinity;
        float pixelRadius = GetLockAreaPixelRadius();
        bool isInsideLockArea = closestProximity < pixelRadius;
        Vector3 desiredAimPointPos = aimingPoint ? aimingPoint.position : Vector3.zero;
        if (isInsideLockArea && targetDistance < lockOnDistance && isVisible)
        {
            DriveCrosshairTo(closestScreenPoint, true);

            // 只決定「目標位置」，不要硬切
            desiredAimPointPos = closestEnemy.transform.position;

            lockOn = true;

            UIManager.Instance.distanceText.text = targetDistance.ToString("F2");
            UIManager.Instance.distanceText.color = UIManager.Instance.lockonColor;
            UIManager.Instance.distanceText.fontStyle = FontStyles.Bold;
        }
        else
        {
            ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            SmoothResetCrosshairToCenter();

            lockOn = false;

            // Free-aim：用 Raycast hit.point 當目標（沒有命中就用 forward*distance）
            if (playerOrientation && Physics.Raycast(ray, out RaycastHit hit, freeAimMaxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                desiredAimPointPos = hit.point;
                UIManager.Instance.distanceText.text = hit.distance.ToString("F2");
            }
            else if (playerOrientation)
            {
                desiredAimPointPos = playerOrientation.transform.position + (playerOrientation.transform.forward * freeAimMaxDistance);
                UIManager.Instance.distanceText.text = 0f.ToString("F2");
            }

            UIManager.Instance.distanceText.color = UIManager.Instance.normalColor;
            UIManager.Instance.distanceText.fontStyle = FontStyles.Normal;
        }

        // 最後統一「平滑套用」
        ApplyAimingPointSmoothing(desiredAimPointPos);
    }
    private void DriveCrosshairTo(Vector2 screenPoint, bool tilt)
    {
        if (!crosshairImage) return;

        RectTransform t = crosshairImage.rectTransform;
        t.position = Vector2.Lerp(t.position, screenPoint, Time.deltaTime * crosshairLerpSpeed);

        Vector3 targetTilt = tilt ? new Vector3(0f, 0f, 45f) : Vector3.zero;
        t.rotation = Quaternion.Euler(
            Vector3.Lerp(t.rotation.eulerAngles, targetTilt, Time.deltaTime * crosshairTiltLerp)
        );

        crosshairImage.color = tilt ? new Color32(24, 180, 0, 200) : new Color32(53, 53, 53, 152);
        // 注意：ray 由 ApplyTargetCaches() 決定，不在這裡改
    }

    private void SmoothResetCrosshairToCenter()
    {
        if (!crosshairImage) return;
        RectTransform t = crosshairImage.rectTransform;

        t.rotation = Quaternion.Euler(
            Vector3.Lerp(t.rotation.eulerAngles, Vector3.zero, Time.deltaTime * resetTiltLerp)
        );
        t.position = Vector2.Lerp(t.position, screenCenter, Time.deltaTime * centerLerpSpeed);
        crosshairImage.color = new Color32(53, 53, 53, 152);
    }

    private float GetLockAreaPixelRadius()
    {
        if (!AimAreaImage) return 0f;
        RectTransform rt = AimAreaImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return Vector3.Distance(corners[0], corners[3]) * 0.5f;
    }
    public Rigidbody GetTargetRigidbody() => currentTargetRb;

    public Ray GetRay() => ray;

    public void SetLockOnDistance(float newLockOnDistance)
    {
        lockOnDistance = newLockOnDistance;
    }
    public void SetAimAreaSize(float newSize)
    {
        AimAreaImage.rectTransform.sizeDelta = new Vector2(newSize, newSize);
        UIManager.Instance.speedInfo.anchoredPosition = new Vector2((newSize/2)+55,0);
        UIManager.Instance.distanceInfo.anchoredPosition = new Vector2(-((newSize / 2) + 55), 0);
    }

    private void ApplyAimingPointSmoothing(Vector3 desiredPos)
    {
        if (!aimingPoint) return;

        bool switching = (lockOn != lastLockOn);
        float smoothTime = switching ? aimingPointSwitchSmoothTime : aimingPointSmoothTime;

        // 切換模式時清掉速度，避免 SmoothDamp 的殘速造成奇怪甩動
        if (switching)
            aimingPointVelocity = Vector3.zero;

        aimingPoint.position = Vector3.SmoothDamp(
            aimingPoint.position,
            desiredPos,
            ref aimingPointVelocity,
            smoothTime,
            aimingPointMaxSpeed
        );

        lastLockOn = lockOn;
    }
}
