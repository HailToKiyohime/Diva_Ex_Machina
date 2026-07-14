using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Vector3 targetLocation;

    public Transform pitchTransform;
    public Transform yawTransform;

    public float pitchSpeed;//degrees per second
    public float yawSpeed;//degrees per second

    [Header("Pitch Limit")]
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 30f;

    [Header("Aim Raycast")]
    [SerializeField] private Transform muzzle;              // 射線起點（hierarchy 裡的 muzzle）
    [SerializeField] private float aimRayDistance = 500f;
    [SerializeField] private LayerMask aimRayMask = ~0;     // 預設打到所有 layer
    [SerializeField] private bool drawAimGizmo = true;

    // 最近一次 raycast 的結果（給 gizmo 和外部查詢用）
    public Vector3 AimPoint { get; private set; }
    public bool AimHasHit { get; private set; }
    public Transform AimHitTransform { get; private set; }

    public void Update()
    {
        Yaw();
        Pitch();
        AimRaycast();
    }

    // 從砲口沿槍管前方打一條射線，得出「槍管實際指向的位置」
    public void AimRaycast()
    {
        if (muzzle == null) return;

        Vector3 origin = muzzle.position;
        Vector3 dir = muzzle.forward;   // 槍管的前方

        // QueryTriggerInteraction.Ignore：不要被自己的視界 trigger 之類的東西擋住
        if (Physics.Raycast(origin, dir, out RaycastHit hit, aimRayDistance, aimRayMask, QueryTriggerInteraction.Ignore))
        {
            AimPoint = hit.point;
            AimHasHit = true;
            AimHitTransform = hit.transform;
        }
        else
        {
            // 沒打到東西：回傳射線盡頭，這樣 AimPoint 永遠有意義
            AimPoint = origin + dir * aimRayDistance;
            AimHasHit = false;
            AimHitTransform = null;
        }
    }

    public void Yaw()
    {
        if (yawTransform == null) return;

        Vector3 dir = targetLocation - yawTransform.position;

        Transform parent = yawTransform.parent;
        Vector3 localDir = parent != null ? parent.InverseTransformDirection(dir) : dir;

        float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float currentYaw = yawTransform.localEulerAngles.y;
        float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, yawSpeed * Time.deltaTime);

        Vector3 e = yawTransform.localEulerAngles;
        yawTransform.localEulerAngles = new Vector3(e.x, newYaw, e.z);
    }

    public void Pitch()
    {
        if (pitchTransform == null) return;

        Vector3 dir = targetLocation - pitchTransform.position;

        Transform parent = pitchTransform.parent;
        Vector3 localDir = parent != null ? parent.InverseTransformDirection(dir) : dir;

        float horizontal = new Vector2(localDir.x, localDir.z).magnitude;
        float targetPitch = -Mathf.Atan2(localDir.y, horizontal) * Mathf.Rad2Deg;

        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        float currentPitch = pitchTransform.localEulerAngles.x;
        float newPitch = Mathf.MoveTowardsAngle(currentPitch, targetPitch, pitchSpeed * Time.deltaTime);

        Vector3 e = pitchTransform.localEulerAngles;
        pitchTransform.localEulerAngles = new Vector3(newPitch, e.y, e.z);
    }

    private void OnDrawGizmos()
    {
        if (!drawAimGizmo || muzzle == null) return;

        // 命中 = 紅色實線到命中點 + 命中點畫球；未命中 = 黃色虛擬射線到盡頭
        if (AimHasHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(muzzle.position, AimPoint);
            Gizmos.DrawSphere(AimPoint, 0.25f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * aimRayDistance);
        }

        // 目標點畫成綠色，方便對照「槍管實際指向」vs「想指向的目標」
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetLocation, 0.5f);
    }
}