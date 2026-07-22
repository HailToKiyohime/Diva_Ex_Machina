using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathToolKit
{
    public static bool InterceptionDirection(Vector3 a, Vector3 b, Vector3 vA, float sB, out Vector3 result)
    {
        Vector3 aToB = b - a;
        float dC = aToB.magnitude;
        float alpha = Vector3.Angle(aToB, vA) * Mathf.Deg2Rad;
        float sA = vA.magnitude;
        float r = sA / sB;
        if (SolveQuadratic(1 - r * r, 2 * r * dC * Mathf.Cos(alpha), -(dC * dC), out var root1, out var root2) == 0)
        {
            result = Vector3.zero;
            return false;
        }
        float dA = Mathf.Max(root1, root2);
        float t = dA / sB;
        Vector3 c = a + vA * t;
        result = (c - b).normalized;
        return true;
    }

    public static bool InterceptionPoint(Vector3 a, Vector3 b, Vector3 vA, float sB, out Vector3 result)
    {
        Vector3 aToB = b - a;
        float dC = aToB.magnitude;
        float alpha = Vector3.Angle(aToB, vA) * Mathf.Deg2Rad;
        float sA = vA.magnitude;
        float r = sA / sB;
        if (SolveQuadratic(1 - r * r, 2 * r * dC * Mathf.Cos(alpha), -(dC * dC), out var root1, out var root2) == 0)
        {
            result = Vector3.zero;
            return false;
        }
        float dA = Mathf.Max(root1, root2);
        float t = dA / sB;
        Vector3 c = a + vA * t;
        result = c;
        return true;
    }
    public static bool GetLaunchDirection(
        Vector3 origin,
        Vector3 target,
        float muzzleSpeed,
        out Vector3 result)
    {
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float x = toTargetXZ.magnitude;
        float y = toTarget.y;
        float v2 = muzzleSpeed * muzzleSpeed;
        float g = Mathf.Abs(Physics.gravity.y);

        // discriminant
        float insideSqrt = v2 * v2 - g * (g * x * x + 2f * y * v2);
        if (insideSqrt < 0f)
        {
            result = Vector3.zero;
            return false;
        }

        float sqrt = Mathf.Sqrt(insideSqrt);

        // *** pick the LOW-arc by using (v2 - sqrt) instead of (v2 + sqrt) ***
        float tanTheta = (v2 - sqrt) / (g * x);
        float cosT = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
        float sinT = tanTheta * cosT;

        Vector3 dirXZ = toTargetXZ.normalized;
        result = dirXZ * cosT + Vector3.up * sinT;
        return true;
    }

    /// <summary>
    /// Moving-target, gravity-and-lead solver.
    /// </summary>
    public static bool GetLaunchDirection(
        Vector3 origin,
        Vector3 targetPos,
        Vector3 targetVel,
        float muzzleSpeed,
        out Vector3 result)
    {
        // �X�X your dynamic version :contentReference[oaicite:1]{index=1}
        const int maxIter = 6;
        result = Vector3.zero;

        float t = (targetPos - origin).magnitude / muzzleSpeed;
        Vector3 launchDir = Vector3.zero;

        for (int i = 0; i < maxIter; i++)
        {
            Vector3 predPos = targetPos + targetVel * t;

            // call the static-target overload
            if (!GetLaunchDirection(origin, predPos, muzzleSpeed, out launchDir))
                return false;

            float v0y = launchDir.y * muzzleSpeed;
            float g = Mathf.Abs(Physics.gravity.y);
            float deltaY = predPos.y - origin.y;
            float underSqrt = v0y * v0y + 2f * g * deltaY;
            if (underSqrt < 0f) underSqrt = 0f;
            t = (v0y + Mathf.Sqrt(underSqrt)) / g;
        }

        result = launchDir.normalized;
        return true;
    }
    public static int SolveQuadratic(float a, float b, float c, out float root1, out float root2)
    {
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
        {
            root1 = Mathf.Infinity;
            root2 = -root1;
            return 0;
        }
        root1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        root2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        return discriminant > 0 ? 2 : 1;
    }

    public static Vector3 GetPointAtTargetBack(Transform a, Transform b, float distance = 10f)
    {
        Vector3 offset = b.position - a.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.0001f) return b.position;
        return b.position + offset.normalized * distance;
    }
}
