using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

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
    public float TopClamp = 70f;
    public float BottomClamp = -30f;

    [Header("Aiming Settings")]
    private Vector2 screenCenter;
    [SerializeField] public bool lockOn = false;
    [SerializeField] private Ray ray;
    [SerializeField] private float targetDistance;
    [SerializeField] private Vector3 targetDirection = Vector3.zero;

    [Header("Lock Settings")]
    [SerializeField] private float lockOnDistance = 50f;
    [SerializeField] private float freeAimMaxDistance = 999f;

    [Header("UI Speeds")]
    [SerializeField] private float centerLerpSpeed = 10f;
    [SerializeField] private float crosshairLerpSpeed = 18f;
    [SerializeField] private float crosshairTiltLerp = 12f;
    [SerializeField] private float resetTiltLerp = 8f;

    [Header("Aiming Point (Optional)")]
    [SerializeField] public Transform aimingPoint;

    // Some scripts in your project may reference this (note spelling)
    [Header("Auto Find / References")]
    [SerializeField] public Transform meshTransform;

    private Rigidbody currentTargetRb;

    // =========================
    // Constrained Lock state
    // =========================
    private Transform _lockedTarget;              // 當前鎖定目標（持續鎖）
    private Rigidbody _lockedTargetRb;
    private Renderer _lockedTargetRenderer;
    private bool _lockedInsideCircle;             // 這一幀目標是否在圈內（只影響 UI + 圈外速度 cap）

    [Header("Lock Follow (Exponential)")]
    [Tooltip("Exponential follow strength. Larger = snappier. (Used for AutoAim + locked follow)")]
    [SerializeField] private float lockRotateSpeed = 12f;

    // =========================
    // Auto Aim (Middle Mouse Toggle)
    // =========================
    [Header("Auto Aim")]
    [SerializeField] private Vector3 autoAimOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private bool autoAimUseBoundsCenter = true;

    [Tooltip("Smooth aim point to reduce jitter. If you prefer old hard lock feel, set 0.")]
    [SerializeField] private float autoAimPointSmoothTime = 0f;

    [Header("Auto Aim Auto Exit")]
    [SerializeField] private float autoAimAutoExitSeconds_LockArea = 3f;
    [SerializeField] private float autoAimAutoExitSeconds_Distance = 1f;

    private float _autoAimOutDistanceTimer = 0f;
    private float _autoAimOutAreaTimer = 0f;

    private bool _autoAimActive;
    private Transform _autoAimTarget;
    private Renderer _autoAimTargetRenderer;

    private Vector3 _autoAimPointVel;
    private Vector3 _autoAimPointSmoothed;

    [Header("Shoulder Offset")]
    [SerializeField] private CinemachineCamera virtualCamera;  // ✅ 改這裡
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float shoulderOffsetXLeft = -2f;
    [SerializeField] private float shoulderOffsetXRight = 2f;
    [SerializeField] private float shoulderOffsetLerpSpeed = 5f;
    [SerializeField] private float shoulderOffsetResetSpeed = 3f; // 回中速度（可比移動速度慢）
    [SerializeField] private float resistancePow = 0.5f;
    [SerializeField] private float shoulderOffsetZForward = 0f;
    [SerializeField] private float shoulderOffsetZBackward = -1.2f;
    [SerializeField] private float shoulderOffsetZDefault = -1f;
    [SerializeField] private float shoulderOffsetZLerpSpeed = 5f;
    [SerializeField] private float shoulderOffsetYDefault = 1f;
    [SerializeField] private float shoulderOffsetYJump = 0.65f;
    [SerializeField] private float shoulderOffsetYFall = 1.5f;
    [SerializeField] private float shoulderOffsetYJumpLerpSpeed = 10f;  // 跳躍：快
    [SerializeField] private float shoulderOffsetYFlyLerpSpeed = 2f;    // 飛行/下落：慢
    [SerializeField] private float shoulderOffsetYGroundLerpSpeed = 5f; // 落地回預設：正常


    private CinemachineThirdPersonFollow _thirdPersonFollow;



    // Debug hook (optional)
    private int _autoAimLastWriteFrame = -1;
    public bool IsAutoAimActiveDebug() => _autoAimActive;
    public bool DidAutoAimWriteThisFrameDebug(int frame) => _autoAimLastWriteFrame == frame;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (meshTransform == null) meshTransform = transform;
        if (mainCam == null) mainCam = Camera.main;

        if (virtualCamera != null)
            _thirdPersonFollow = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body)
                         as CinemachineThirdPersonFollow;
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.currentCameraSet != 0)
            return;

        if (_autoAimActive)
        {
            CrosshairDetect_ConstrainedLock();
            UpdateShoulderOffset(); // ✅ 加在這裡
            return;
        }

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

        CrosshairDetect_ConstrainedLock();
        UpdateShoulderOffset();
    }

    private void LateUpdate()
    {
        // AutoAim：用指數式跟隨
        if (_autoAimActive)
            LateUpdateAutoAimRotation_ExponentialWithCap();
    }

    // =========================
    // Input API
    // =========================
    public void ToggleAutoAim()
    {
        if (_autoAimActive)
        {
            EndAutoAim();
            return;
        }

        // 只允許：當下有鎖定目標時開啟
        if (_lockedTarget == null) return;

        _autoAimTarget = _lockedTarget;
        _autoAimTargetRenderer = _lockedTargetRenderer;
        _autoAimActive = true;

        _autoAimPointSmoothed = GetAutoAimPointRaw();
        _autoAimPointVel = Vector3.zero;
    }

    private void EndAutoAim()
    {
        _autoAimActive = false;
        _autoAimTarget = null;
        _autoAimTargetRenderer = null;
        _autoAimPointVel = Vector3.zero;

        _autoAimOutDistanceTimer = 0f;
        _autoAimOutAreaTimer = 0f;
    }

    // =========================
    // Constrained Lock (核心)
    // =========================
    private void CrosshairDetect_ConstrainedLock()
    {
        screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float radius = GetLockAreaPixelRadius();

        // ======================================================
        // AutoAim active: lock target is fixed to _autoAimTarget
        // ======================================================
        if (_autoAimActive)
        {
            if (_autoAimTarget == null || !_autoAimTarget.gameObject.activeInHierarchy)
            {
                // Target gone: AutoAim should end elsewhere too, but keep safe here
                ClearLock();
                return;
            }

            if (_lockedTarget != _autoAimTarget)
            {
                _lockedTarget = _autoAimTarget;
                _lockedTargetRb = _autoAimTarget.GetComponentInParent<Rigidbody>();
                _lockedTargetRenderer = _autoAimTarget.GetComponentInChildren<Renderer>();
            }
        }

        // ======================================================
        // 1) Have a locked target: maintain/update
        // ======================================================
        if (_lockedTarget != null && _lockedTarget.gameObject.activeInHierarchy)
        {
            if (_lockedTargetRenderer == null)
                _lockedTargetRenderer = _lockedTarget.GetComponentInChildren<Renderer>();

            // Only use Renderer.isVisible when NOT auto-aiming
            bool isVisible = true;
            if (!_autoAimActive)
                isVisible = (_lockedTargetRenderer != null) ? _lockedTargetRenderer.isVisible : true;

            targetDistance = Vector3.Distance(playerOrientation.transform.position, _lockedTarget.position);

            // LockOnDistance hard drop (but during AutoAim we do NOT scan for a new one)
            if (!isVisible || targetDistance > lockOnDistance)
            {
                ClearLock();
                if (_autoAimActive) return; // don't scan new target during AutoAim
            }
            else
            {
                Vector3 sp = mainCam.WorldToScreenPoint(_lockedTarget.position);

                // Behind camera: drop lock (but during AutoAim we do NOT scan for a new one)
                if (sp.z <= 0f)
                {
                    ClearLock();
                    if (_autoAimActive) return;
                }
                else
                {
                    Vector2 targetScreen = new Vector2(sp.x, sp.y);
                    Vector2 delta = targetScreen - screenCenter;

                    _lockedInsideCircle = (delta.magnitude <= radius + 0.01f);

                    // AutoAim OFF: no clamp; if outside circle -> drop lock and continue to scan/free-aim
                    if (!_autoAimActive && !_lockedInsideCircle)
                    {
                        ClearLock();
                        // IMPORTANT: do NOT return; allow scanning/free-aim below
                    }
                    else
                    {
                        // AutoAim ON: clamp when outside; inside follow target normally
                        Vector2 uiPoint = targetScreen;
                        if (_autoAimActive && !_lockedInsideCircle)
                            uiPoint = screenCenter + delta.normalized * radius;

                        // Circle-outside should look like normal crosshair (gray + no tilt)
                        DriveCrosshairTo(uiPoint, _lockedInsideCircle);

                        // Ray always follows crosshair point (needed for circle-outside firing)
                        ray = mainCam.ScreenPointToRay(uiPoint);

                        if (_lockedInsideCircle)
                        {
                            // Inside circle: TRUE lockOn (shoot target)
                            lockOn = true;
                            currentTargetRb = _lockedTargetRb;
                            targetDirection = (_lockedTarget.position - transform.position).normalized;

                            if (aimingPoint) aimingPoint.position = _lockedTarget.position;

                            if (UIManager.Instance != null)
                            {
                                UIManager.Instance.distanceText.text = targetDistance.ToString("F2");
                                UIManager.Instance.distanceText.color = UIManager.Instance.lockonColor;
                                UIManager.Instance.distanceText.fontStyle = FontStyles.Bold;
                            }
                        }
                        else
                        {
                            // Outside circle (clamped): NOT lockOn (shoot crosshair ray)
                            lockOn = false;
                            currentTargetRb = null;
                            targetDirection = Vector3.zero;

                            if (Physics.Raycast(ray, out RaycastHit hit2, freeAimMaxDistance, ~0, QueryTriggerInteraction.Ignore))
                            {
                                if (aimingPoint) aimingPoint.position = hit2.point;

                                if (UIManager.Instance != null)
                                    UIManager.Instance.distanceText.text = hit2.distance.ToString("F2");
                            }
                            else
                            {
                                if (aimingPoint) aimingPoint.position = ray.origin + ray.direction * freeAimMaxDistance;

                                if (UIManager.Instance != null)
                                    UIManager.Instance.distanceText.text = 0f.ToString("F2");
                            }

                            if (UIManager.Instance != null)
                            {
                                UIManager.Instance.distanceText.color = UIManager.Instance.normalColor;
                                UIManager.Instance.distanceText.fontStyle = FontStyles.Normal;
                            }
                        }

                        return; // handled locked-target case
                    }
                }
            }
        }

        // ======================================================
        // If AutoAim is active but lock got cleared this frame:
        // never scan for a new target (prevents target switching)
        // ======================================================
        if (_autoAimActive)
        {
            return;
        }

        // ======================================================
        // 2) No lock: scan closest enemy (must be inside circle + visible + within distance)
        // ======================================================
        GameObject closestEnemy = null;
        Vector2 closestScreenPoint = Vector2.zero;
        float closestProximity = Mathf.Infinity;

        List<GameObject> enemies = GameManager.Instance.GetEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!enemy) continue;

            Vector3 sp = mainCam.WorldToScreenPoint(enemy.transform.position);
            if (sp.z <= 0f) continue;

            Vector2 pt = new Vector2(sp.x, sp.y);
            float prox = Vector2.Distance(pt, screenCenter);

            if (prox < closestProximity)
            {
                closestProximity = prox;
                closestScreenPoint = pt;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            Renderer r = closestEnemy.GetComponentInChildren<Renderer>();
            bool isVisible = (r != null) ? r.isVisible : true;

            targetDistance = Vector3.Distance(playerOrientation.transform.position, closestEnemy.transform.position);
            bool inside = (closestProximity <= radius);
            bool ok = inside && isVisible && (targetDistance <= lockOnDistance);

            if (ok)
            {
                _lockedTarget = closestEnemy.transform;
                _lockedTargetRb = closestEnemy.GetComponentInParent<Rigidbody>();
                _lockedTargetRenderer = r;
                _lockedInsideCircle = true;

                lockOn = true;
                currentTargetRb = _lockedTargetRb;

                DriveCrosshairTo(closestScreenPoint, true);
                ray = mainCam.ScreenPointToRay(closestScreenPoint);

                if (aimingPoint) aimingPoint.position = _lockedTarget.position;
                return;
            }
        }

        // ======================================================
        // 3) Free aim (no lock)
        // ======================================================
        lockOn = false;
        _lockedInsideCircle = false;

        ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        SmoothResetCrosshairToCenter();

        if (playerOrientation && Physics.Raycast(ray, out RaycastHit hit, freeAimMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (aimingPoint) aimingPoint.position = hit.point;
            if (UIManager.Instance != null)
                UIManager.Instance.distanceText.text = hit.distance.ToString("F2");
        }
        else if (playerOrientation)
        {
            if (aimingPoint) aimingPoint.position = playerOrientation.transform.position + (playerOrientation.transform.forward * freeAimMaxDistance);
            if (UIManager.Instance != null)
                UIManager.Instance.distanceText.text = 0f.ToString("F2");
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.distanceText.color = UIManager.Instance.normalColor;
            UIManager.Instance.distanceText.fontStyle = FontStyles.Normal;
        }
    }


    private void ClearLock()
    {
        _lockedTarget = null;
        _lockedTargetRb = null;
        _lockedTargetRenderer = null;
        _lockedInsideCircle = false;

        lockOn = false;
        currentTargetRb = null;
    }

    // =========================
    // AutoAim rotation (Exponential + cap when out of circle)
    // =========================
    private void LateUpdateAutoAimRotation_ExponentialWithCap()
    {
        if (_autoAimTarget == null || playerOrientation == null || mainCam == null)
        {
            EndAutoAim();
            return;
        }

        // 如果目標被 destroy / inactive：自動關閉
        if (!_autoAimTarget.gameObject.activeInHierarchy)
        {
            EndAutoAim();
            return;
        }

        float dt = Time.deltaTime;

        // ======================================================
        // Auto exit conditions (>= autoAimAutoExitSeconds)
        // ======================================================

        // 1) Out of lockOnDistance for > N seconds
        float distToTarget = Vector3.Distance(playerOrientation.transform.position, _autoAimTarget.position);
        if (distToTarget > lockOnDistance)
            _autoAimOutDistanceTimer += dt;
        else
            _autoAimOutDistanceTimer = 0f;

        // 2) Out of LockArea (crosshair clamped) for > N seconds
        // _lockedInsideCircle is updated by CrosshairDetect_ConstrainedLock() while AutoAim is active
        if (!_lockedInsideCircle)
            _autoAimOutAreaTimer += dt;
        else
            _autoAimOutAreaTimer = 0f;

        if (_autoAimOutDistanceTimer >= autoAimAutoExitSeconds_Distance || _autoAimOutAreaTimer >= autoAimAutoExitSeconds_LockArea)
        {
            EndAutoAim();
            return;
        }

        // ======================================================
        // Aim point
        // ======================================================
        Vector3 raw = GetAutoAimPointRaw();

        if (autoAimPointSmoothTime > 0f)
            _autoAimPointSmoothed = Vector3.SmoothDamp(_autoAimPointSmoothed, raw, ref _autoAimPointVel, autoAimPointSmoothTime);
        else
            _autoAimPointSmoothed = raw;

        // desired rotation from camera position -> aim point
        Vector3 camPos = mainCam.transform.position;
        Vector3 dir = _autoAimPointSmoothed - camPos;
        if (dir.sqrMagnitude < 0.0001f)
            dir = mainCam.transform.forward;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        Vector3 e = look.eulerAngles;

        float desiredYaw = e.y;
        float desiredPitch = ClampAngle(NormalizePitch(e.x), BottomClamp, TopClamp);
        Quaternion targetRot = Quaternion.Euler(desiredPitch, desiredYaw, 0f);

        Quaternion current = playerOrientation.transform.rotation;
        float angle = Quaternion.Angle(current, targetRot);
        if (angle < 0.0001f)
            return;

        // 指數式跟隨：stepWanted = angle * (1 - exp(-k * dt))
        float k = Mathf.Max(0f, lockRotateSpeed);
        float expT = 1f - Mathf.Exp(-k * dt);
        float stepWanted = angle * expT;

        // 永遠用 AutoAimSpeed 當 max deg/s（圈內圈外都 cap）
        float maxDegPerSec = 120f;
        if (PlayerStats.Instance != null)
            maxDegPerSec = Mathf.Max(0f, PlayerStats.Instance.GetAutoAimSpeed());

        float maxStep = maxDegPerSec * dt;
        stepWanted = Mathf.Min(stepWanted, maxStep);

        float t = Mathf.Clamp01(stepWanted / angle);
        Quaternion newRot = Quaternion.Slerp(current, targetRot, t);

        playerOrientation.transform.rotation = newRot;
        _autoAimLastWriteFrame = Time.frameCount;

        // sync turn (prevents oscillation between systems)
        Vector3 applied = newRot.eulerAngles;
        turn.x = applied.y;
        turn.y = ClampAngle(NormalizePitch(applied.x), BottomClamp, TopClamp);
    }


    private Vector3 GetAutoAimPointRaw()
    {
        if (_autoAimTarget == null) return Vector3.zero;

        if (autoAimUseBoundsCenter)
        {
            if (_autoAimTargetRenderer == null)
                _autoAimTargetRenderer = _autoAimTarget.GetComponentInChildren<Renderer>();

            if (_autoAimTargetRenderer != null)
                return _autoAimTargetRenderer.bounds.center + autoAimOffset;
        }

        return _autoAimTarget.position + autoAimOffset;
    }

    // =========================
    // UI helpers
    // =========================
    private void DriveCrosshairTo(Vector2 screenPoint, bool tilt)
    {
        if (!crosshairImage) return;

        RectTransform t = crosshairImage.rectTransform;
        t.position = Vector2.Lerp(t.position, screenPoint, Time.deltaTime * crosshairLerpSpeed);

        Vector3 targetTilt = tilt ? new Vector3(0f, 0f, 45f) : Vector3.zero;
        t.rotation = Quaternion.Euler(Vector3.Lerp(t.rotation.eulerAngles, targetTilt, Time.deltaTime * crosshairTiltLerp));

        crosshairImage.color = tilt ? new Color32(24, 180, 0, 200) : new Color32(53, 53, 53, 152);
    }

    private void SmoothResetCrosshairToCenter()
    {
        if (!crosshairImage) return;

        RectTransform t = crosshairImage.rectTransform;
        t.rotation = Quaternion.Euler(Vector3.Lerp(t.rotation.eulerAngles, Vector3.zero, Time.deltaTime * resetTiltLerp));
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

    // =========================
    // Public helpers
    // =========================
    public Rigidbody GetTargetRigidbody() => currentTargetRb;
    public Ray GetRay() => ray;

    public void SetLockOnDistance(float newLockOnDistance) => lockOnDistance = newLockOnDistance;

    // pitch 0..360 -> -180..180
    private static float NormalizePitch(float xDeg)
    {
        if (xDeg > 180f) xDeg -= 360f;
        return xDeg;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
    public void SetAimAreaSize(float newSize)
    {
        if (AimAreaImage == null) return;

        AimAreaImage.rectTransform.sizeDelta = new Vector2(newSize, newSize);

        // 如果你 UIManager 入面有呢兩個，保持兼容（冇就會 null-safe）
        if (UIManager.Instance != null)
        {
            UIManager.Instance.speedInfo.anchoredPosition = new Vector2((newSize / 2) + 55, 0);
            UIManager.Instance.distanceInfo.anchoredPosition = new Vector2(-((newSize / 2) + 55), 0);
        }
    }
    private void UpdateShoulderOffset()
    {
        if (_thirdPersonFollow == null) return;

        float inputX = PlayerController.Instance != null
            ? PlayerController.Instance.LastMoveInput.x
            : 0f;

        float currentX = _thirdPersonFollow.ShoulderOffset.x;
        float newX;

        if (inputX < -0.1f || inputX > 0.1f)
        {
            // 有左右輸入：移向目標側，帶阻力
            float targetX = inputX < -0.1f ? shoulderOffsetXLeft : shoulderOffsetXRight;

            float range = Mathf.Abs(targetX);
            float distRatio = range > 0f ? Mathf.Clamp01(Mathf.Abs(targetX - currentX) / (range * 2f)) : 0f;
            float resistanceCurve = Mathf.Pow(distRatio, resistancePow);
            float dynamicSpeed = shoulderOffsetLerpSpeed * resistanceCurve;

            newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * dynamicSpeed);
        }
        else
        {
            // 無輸入：平滑回中
            newX = Mathf.Lerp(currentX, 0f, Time.deltaTime * shoulderOffsetResetSpeed);
        }

        // Z 軸：根據前後輸入調整
        float inputY = PlayerController.Instance != null
            ? PlayerController.Instance.LastMoveInput.y
            : 0f;

        float currentZ = _thirdPersonFollow.ShoulderOffset.z;
        float targetZ;

        if (inputY > 0.1f)
            targetZ = shoulderOffsetZForward;       // 向前：拉近鏡頭
        else if (inputY < -0.1f)
            targetZ = shoulderOffsetZBackward;      // 向後：推遠鏡頭
        else
            targetZ = shoulderOffsetZDefault;       // 無輸入：回預設值

        float newZ = Mathf.Lerp(currentZ, targetZ, Time.deltaTime * shoulderOffsetZLerpSpeed);

        // Y 軸：根據跳躍 / 飛行 / 下落狀態調整
        float currentY = _thirdPersonFollow.ShoulderOffset.y;
        float targetY;
        float yLerpSpeed;

        if (playerMovement != null)
        {
            float vertVel = playerMovement.VerticalVelocity;
            bool isFlying = playerMovement.IsFlyingActive;
            bool isGrounded = playerMovement.IsGrounded;

            if (isGrounded)
            {
                // 落地：回預設值，正常速度
                targetY = shoulderOffsetYDefault;
                yLerpSpeed = shoulderOffsetYGroundLerpSpeed;
            }
            else if (isFlying)
            {
                // 飛行中：固定用慢速，目標 Y 隨 vertVel 在 Jump~Fall 之間插值
                // vertVel >= 0 → Jump(0.65)，vertVel 越負 → 越接近 Fall(1.5)
                float velT = Mathf.Clamp01(-vertVel / 10f); // 10f = 下落速度參考值，可 Inspector 調
                targetY = Mathf.Lerp(shoulderOffsetYJump, shoulderOffsetYFall, velT);
                yLerpSpeed = shoulderOffsetYFlyLerpSpeed;
            }
            else
            {
                // 空中（跳躍）：目標 Y 一樣用 vertVel 插值，但 LerpSpeed 隨 vertVel 平滑過渡
                // vertVel 大（剛起跳）→ 速度快；vertVel 趨近 0（頂點）→ 速度慢
                float velT = Mathf.Clamp01(-vertVel / 10f);
                targetY = Mathf.Lerp(shoulderOffsetYJump, shoulderOffsetYFall, velT);
                // 速度：上升時用 jumpLerpSpeed，下落時漸漸過渡到 flyLerpSpeed
                float speedT = Mathf.Clamp01(-vertVel / 5f); // 過渡區間，可調
                yLerpSpeed = Mathf.Lerp(shoulderOffsetYJumpLerpSpeed, shoulderOffsetYFlyLerpSpeed, speedT);
            }
        }
        else
        {
            targetY = shoulderOffsetYDefault;
            yLerpSpeed = shoulderOffsetYGroundLerpSpeed;
        }

        float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * yLerpSpeed);

        _thirdPersonFollow.ShoulderOffset = new Vector3(
            newX,
            newY,
            newZ
        );
    }
}