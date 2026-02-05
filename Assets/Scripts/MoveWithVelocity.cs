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
    public float maxAccel = 50f;  // 加速度上限，越大越貼 spline
    public float lookAhead = 0.01f; // 看前一點點，減少抖動

    private Rigidbody rb;
    private float approxSplineLength = 10f;

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
        Vector3 desiredVel = toTarget.normalized * speed;

        // 用加速度上限把 velocity 拉向 desiredVel
        Vector3 dv = desiredVel - rb.linearVelocity;
        Vector3 accel = Vector3.ClampMagnitude(dv / Time.fixedDeltaTime, maxAccel);

        rb.AddForce(accel, ForceMode.Acceleration);
    }
}
