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

    // ===== Melee Dash Camera Assist =====
    [Header("Melee Dash Camera Assist")]
    [SerializeField] private bool enableMeleeDashCameraAssist = true;
    [SerializeField] private float dashFocusYawLerp = 18f;
    [SerializeField] private float dashFocusPitchLerp = 18f;
    [SerializeField] private float dashFocusMinDirSqr = 0.0001f;

    [Header("Dash Focus Target Point")]
    [SerializeField] private Vector3 dashAimWorldOffset = Vector3.zero; // 想鎖胸口就 (0, 0.9, 0) 之類
    [SerializeField] private bool dashAimUseBoundsCenter = true;

    [Header("Dash Focus Center Correction")]
    [SerializeField] private float dashCenterCorrectionYawGain = 35f;   // deg per viewport error
    [SerializeField] private float dashCenterCorrectionPitchGain = 35f; // deg per viewport error
    [SerializeField] private float dashCenterCorrectionMaxStep = 12f;   // deg per frame clamp

    private bool _dashFocusActive;
    private Transform _dashFocusTarget;

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
        // Melee dash 期間：接管相機，強制把目標拉回中央
        if (_dashFocusActive)
        {
            UpdateMeleeDashCameraFocus();
            return; // 重要：阻止滑鼠輸入覆蓋 turn.x/turn.y
        }
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
    public void BeginMeleeDashCameraFocus(Transform target)
    {
        if (!enableMeleeDashCameraAssist) return;
        if (target == null) return;
        if (playerOrientation == null) return;

        _dashFocusTarget = target;
        _dashFocusActive = true;

        // 近戰 dash 期間：強制視為 lock-on（避免 CrosshairDetect 把 lockOn 掉了）
        lockOn = true;

        // 同步 target cache（你原本就用這個給 PlayerMovement 追人）
        currentTargetRb = target.GetComponentInParent<Rigidbody>();
    }

    public void EndMeleeDashCameraFocus()
    {
        _dashFocusActive = false;
        _dashFocusTarget = null;
    }

    // 把 0~360 的 pitch 轉成 -180~180，方便 Clamp
    private static float NormalizePitch(float xDeg)
    {
        if (xDeg > 180f) xDeg -= 360f;
        return xDeg;
    }

    private void UpdateMeleeDashCameraFocus()
    {
        if (_dashFocusTarget == null || playerOrientation == null || mainCam == null)
        {
            EndMeleeDashCameraFocus();
            return;
        }

        Vector3 aimPoint = GetDashAimPoint(_dashFocusTarget);

        // 1) 用「相機位置」算出真正需要的朝向（避免 pivot vs camera 的視差）
        Vector3 camPos = mainCam.transform.position;
        Vector3 dirFromCam = (aimPoint - camPos);

        if (dirFromCam.sqrMagnitude < dashFocusMinDirSqr)
            dirFromCam = mainCam.transform.forward;

        Quaternion look = Quaternion.LookRotation(dirFromCam.normalized, Vector3.up);
        Vector3 e = look.eulerAngles;

        float desiredYaw = e.y;
        float desiredPitch = NormalizePitch(e.x);
        desiredPitch = ClampAngle(desiredPitch, BottomClamp, TopClamp);

        // 2) 先做一次平滑跟隨
        turn.x = Mathf.LerpAngle(turn.x, desiredYaw, Time.deltaTime * dashFocusYawLerp);
        turn.y = Mathf.LerpAngle(turn.y, desiredPitch, Time.deltaTime * dashFocusPitchLerp);
        turn.y = ClampAngle(turn.y, BottomClamp, TopClamp);

        playerOrientation.transform.rotation = Quaternion.Euler(turn.y, turn.x, 0f);

        // 3) 用 viewport 誤差做二次校正（保證把目標推回畫面正中）
        Vector3 vp = mainCam.WorldToViewportPoint(aimPoint);
        if (vp.z > 0f)
        {
            float errX = 0.5f - vp.x; // 目標在左 => errX 正
            float errY = 0.5f - vp.y; // 目標在下 => errY 正（代表我們看太高）

            float yawStep = Mathf.Clamp(-errX * dashCenterCorrectionYawGain, -dashCenterCorrectionMaxStep, dashCenterCorrectionMaxStep);
            float pitchStep = Mathf.Clamp(errY * dashCenterCorrectionPitchGain, -dashCenterCorrectionMaxStep, dashCenterCorrectionMaxStep);

            // yaw：目標在左 => turn.x 減少（往左轉）=> yawStep 會是正? 所以我們用 += yawStep（yawStep 已含負號）
            turn.x = Mathf.Repeat(turn.x + yawStep, 360f);

            // pitch：目標在下 => 我們要往下看 => turn.y 增加
            turn.y = ClampAngle(turn.y + pitchStep, BottomClamp, TopClamp);

            playerOrientation.transform.rotation = Quaternion.Euler(turn.y, turn.x, 0f);
        }

        // 4) 準星固定中心 + aimingPoint 追目標點
        ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        DriveCrosshairTo(screenCenter, true);
        ApplyAimingPointSmoothing(aimPoint);

        // UI（沿用 lock-on 的樣式）
        targetDistance = Vector3.Distance(playerOrientation.transform.position, aimPoint);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.distanceText.text = targetDistance.ToString("F2");
            UIManager.Instance.distanceText.color = UIManager.Instance.lockonColor;
            UIManager.Instance.distanceText.fontStyle = FontStyles.Bold;
        }
    }
    private Vector3 GetDashAimPoint(Transform t)
    {
        if (t == null) return Vector3.zero;

        Vector3 p = t.position;

        if (dashAimUseBoundsCenter)
        {
            // 優先用 Collider bounds center（最符合"目標中心"）
            var col = t.GetComponentInChildren<Collider>();
            if (col != null)
                p = col.bounds.center;
            else
            {
                var r = t.GetComponentInChildren<Renderer>();
                if (r != null) p = r.bounds.center;
            }
        }

        return p + dashAimWorldOffset;
    }
}
