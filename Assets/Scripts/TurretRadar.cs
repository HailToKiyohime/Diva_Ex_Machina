using System.Collections.Generic;
using UnityEngine;

public class TurretRadar : MonoBehaviour
{
    public FirearmControlSystem firearmControl;
    public float radarRange = 50f;

    [Header("Timing")]
    public float scanInterval = 0.1f;

    [Header("Angle")]
    [Tooltip("If turretRotationLimit is 360 or <= 0, use this half-angle (degrees) for target assignment.")]
    public float defaultAssignHalfAngleDeg = 180f;

    [Tooltip("How close the barrel must be aiming at its current aim point before firing (degrees).")]
    public float fireConeDeg = 6f;

    [Header("Behavior")]
    public bool preferUniqueTargets = true;
    public bool ignoreDeadObjects = true;

    private float _scanTimer;

    void Update()
    {
        if (firearmControl == null) return;
        if (firearmControl.firearms == null || firearmControl.firearms.Length == 0) return;

        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            _scanTimer = Mathf.Max(0.01f, scanInterval);
            AssignTargetsFromGameManager();
        }

        firearmControl.RequestFireAll(fireConeDeg);
    }

    private void AssignTargetsFromGameManager()
    {
        List<GameObject> enemies = null;

        if (GameManager.Instance != null)
        {
            enemies = GameManager.Instance.GetEnemies();
        }

        if (enemies == null)
        {
            enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
        }

        var used = preferUniqueTargets ? new HashSet<int>() : null;

        for (int i = 0; i < firearmControl.firearms.Length; i++)
        {
            var fa = firearmControl.firearms[i];
            if (fa == null)
                continue;

            Transform aimFrom = GetYawReference(fa);
            if (aimFrom == null)
            {
                fa.target = null;
                fa.targetRigidbody = null;
                continue;
            }

            float halfAngle = GetAssignHalfAngle(fa);

            Transform bestT = null;
            Rigidbody bestRb = null;
            float bestDistSqr = float.PositiveInfinity;

            for (int e = 0; e < enemies.Count; e++)
            {
                var go = enemies[e];
                if (go == null) continue;
                if (ignoreDeadObjects && !go.activeInHierarchy) continue;

                int id = go.GetInstanceID();
                if (preferUniqueTargets && used != null && used.Contains(id))
                    continue;

                Vector3 to = go.transform.position - transform.position;
                to.y = 0f;
                float distSqr = to.sqrMagnitude;

                if (distSqr > radarRange * radarRange)
                    continue;

                if (!IsWithinYawHalfAngle(aimFrom, go.transform.position, halfAngle))
                    continue;

                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    bestT = go.transform;
                    bestRb = go.GetComponent<Rigidbody>();
                }
            }

            if (bestT == null && preferUniqueTargets)
            {
                for (int e = 0; e < enemies.Count; e++)
                {
                    var go = enemies[e];
                    if (go == null) continue;
                    if (ignoreDeadObjects && !go.activeInHierarchy) continue;

                    Vector3 to = go.transform.position - transform.position;
                    to.y = 0f;
                    float distSqr = to.sqrMagnitude;

                    if (distSqr > radarRange * radarRange)
                        continue;

                    if (!IsWithinYawHalfAngle(aimFrom, go.transform.position, halfAngle))
                        continue;

                    if (distSqr < bestDistSqr)
                    {
                        bestDistSqr = distSqr;
                        bestT = go.transform;
                        bestRb = go.GetComponent<Rigidbody>();
                    }
                }
            }

            fa.target = bestT;
            fa.targetRigidbody = bestRb;

            if (preferUniqueTargets && used != null && bestT != null)
                used.Add(bestT.gameObject.GetInstanceID());
        }
    }

    private static Transform GetYawReference(Firearm fa)
    {
        if (fa == null) return null;

        if (fa.turret != null) return fa.turret.transform;
        if (fa.muzzleCenterPoint != null) return fa.muzzleCenterPoint;
        if (fa.barrel != null) return fa.barrel.transform;

        return null;
    }

    private float GetAssignHalfAngle(Firearm fa)
    {
        if (fa == null) return Mathf.Clamp(defaultAssignHalfAngleDeg, 0f, 180f);

        float lim = fa.turretRotationLimit;
        if (lim > 0f && lim < 360f)
            return Mathf.Clamp(lim * 0.5f, 0f, 180f);

        return Mathf.Clamp(defaultAssignHalfAngleDeg, 0f, 180f);
    }

    private static bool IsWithinYawHalfAngle(Transform yawRef, Vector3 targetPos, float halfAngleDeg)
    {
        if (yawRef == null) return false;

        Vector3 fwd = yawRef.forward;
        fwd.y = 0f;

        Vector3 to = targetPos - yawRef.position;
        to.y = 0f;

        if (fwd.sqrMagnitude < 0.0001f) return false;
        if (to.sqrMagnitude < 0.0001f) return true;

        float ang = Vector3.Angle(fwd.normalized, to.normalized);
        return ang <= Mathf.Max(0f, halfAngleDeg);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, radarRange));
    }
}