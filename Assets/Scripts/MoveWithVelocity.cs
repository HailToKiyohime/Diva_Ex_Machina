using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Rigidbody))]
public class MoveWithVelocity : MonoBehaviour
{
    public SplineContainer spline;
    [Range(0f, 1f)] public float t = 0f;
    public float speed = 5f;
    public bool loop = true;

    [Header("Steering")]
    public float maxAccel = 50f;
    public float lookAhead = 0.01f;

    [Header("Facing")]
    public bool faceMoveDirection = true;
    public float turnSpeed = 12f;
    public bool keepUpright = true;
    public float minSpeedToTurn = 0.05f;

    private Rigidbody rb;
    private float approxSplineLength = 10f;

    [Header("Camera Rotation Sync")]
    public Transform cameraTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        approxSplineLength = spline != null ? Mathf.Max(0.01f, spline.CalculateLength()) : 10f;
    }

    void FixedUpdate()
    {
        if (spline == null) return;

        float dt = (speed / approxSplineLength) * Time.fixedDeltaTime;
        t += dt;

        if (loop) t = Mathf.Repeat(t, 1f);
        else t = Mathf.Clamp01(t);

        float tAhead = loop ? Mathf.Repeat(t + lookAhead, 1f) : Mathf.Clamp01(t + lookAhead);

        Vector3 targetPos = spline.EvaluatePosition(tAhead);

        Vector3 toTarget = (targetPos - rb.position);
        Vector3 desiredVel = toTarget.sqrMagnitude > 0.000001f ? toTarget.normalized * speed : Vector3.zero;

        Vector3 dv = desiredVel - rb.linearVelocity;
        Vector3 accel = Vector3.ClampMagnitude(dv / Time.fixedDeltaTime, maxAccel);
        rb.AddForce(accel, ForceMode.Acceleration);

        if (faceMoveDirection)
        {
            Vector3 v = rb.linearVelocity;

            if (v.sqrMagnitude > (minSpeedToTurn * minSpeedToTurn))
            {
                Vector3 forward = v.normalized;

                if (keepUpright)
                {
                    forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
                    if (forward.sqrMagnitude < 0.000001f) return;
                }

                Quaternion oldRot = rb.rotation;
                Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
                Quaternion newRot = Quaternion.Slerp(oldRot, targetRot, turnSpeed * Time.fixedDeltaTime);

                // Apply rotation to rigidbody
                rb.MoveRotation(newRot);

                // Sync camera by the same delta rotation
                if (cameraTransform != null)
                {
                    Quaternion delta = newRot * Quaternion.Inverse(oldRot);
                    cameraTransform.rotation = delta * cameraTransform.rotation;
                }
            }
        }
    }
}