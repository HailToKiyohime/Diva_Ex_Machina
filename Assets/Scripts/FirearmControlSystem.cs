using UnityEngine;

[System.Serializable]
public class FireArm {
    public GameObject turret;
    public GameObject barrel;

    public FiringMode firingMode; //Salvo: Every time FireWeapon called, fire ALL Barrel at once, ONLY consumes 1 round per call; ShootingInTurn: Each time FireWeapon called, fire 1 Barrel(consumes 1 round), then next call fires next Barrel, and so on.

    public float turretRotationSpeed;
    public float barrelElevationSpeed;
    public float turretRotationLimit; // if angle is 360, no limit
    public float barrelElevationLimitUp;
    public float barrelElevationLimitDown;

    public Transform[] muzzles; // array of muzzle transforms for firing projectiles
    public Transform muzzleCenterPoint;// central point of muzzles for calculating intersection with targets

    public GameObject bulletPrefab; // prefab of the bullet to be fired
    public float bulletSpeed; // speed of the bullet
    public float bulletSpread; // spread angle for inaccuracy
    public float bulletPerRound = 1; // number of bullets fired per Round, e.g., shotgun pellets, PS: does not affect the number of rounds fired
    public float roundsPerFire = 1; // number of round per firing action, e.g., burst fire, for single shot set to 1,PS : This will affect the number of rounds fired
    public float timeBetweenShots = 0; // time delay between shots
    public float timeBetweenShooting = 1f;
    public float reloadTime = 5f;   
    public float reloadTimer = 0f;
    public int magazineSize = 5; // number of rounds per magazine
}


public class FirearmControlSystem : MonoBehaviour
{
    public Transform target;
    public FireArm[] fireArms;

    [Header("Target velocity source")]
    public Rigidbody targetRigidbody; // optional; if null will estimate from position delta

    private Vector3 _lastTargetPos;
    private Vector3 _targetVel;

    private struct Runtime
    {
        public Quaternion turretInitialLocalRot;
        public Quaternion barrelInitialLocalRot;

        // yaw/pitch deltas from initial pose
        public float yawDeltaDeg;
        public float pitchDeg; // positive = up

        public bool inited;
    }

    private Runtime[] _rt;

    private void Awake()
    {
        if (fireArms == null) fireArms = new FireArm[0];
        _rt = new Runtime[fireArms.Length];
    }

    private void Start()
    {
        if (target != null) _lastTargetPos = target.position;

        for (int i = 0; i < fireArms.Length; i++)
        {
            var fa = fireArms[i];
            if (fa == null || fa.turret == null || fa.barrel == null) continue;

            _rt[i].turretInitialLocalRot = fa.turret.transform.localRotation;
            _rt[i].barrelInitialLocalRot = fa.barrel.transform.localRotation;
            _rt[i].yawDeltaDeg = 0f;
            _rt[i].pitchDeg = 0f;
            _rt[i].inited = true;
        }
    }

    private void Update()
    {
        if (target == null || fireArms == null) return;

        UpdateTargetVelocity();

        for (int i = 0; i < fireArms.Length; i++)
        {
            AimOne(i, fireArms[i]);
        }
    }

    private void UpdateTargetVelocity()
    {
        if (target == null) { _targetVel = Vector3.zero; return; }

        if (targetRigidbody != null)
        {
            _targetVel = targetRigidbody.linearVelocity;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0.0001f) return;

        Vector3 now = target.position;
        _targetVel = (now - _lastTargetPos) / dt;
        _lastTargetPos = now;
    }

    private void AimOne(int idx, FireArm fa)
    {
        if (fa == null) return;
        if (fa.turret == null || fa.barrel == null) return;
        if (!_rt[idx].inited) return;

        Transform turretT = fa.turret.transform;
        Transform barrelT = fa.barrel.transform;

        // 1) Choose origin for interception calc
        Vector3 origin = (fa.muzzleCenterPoint != null) ? fa.muzzleCenterPoint.position : barrelT.position;

        // 2) Compute aim point (lead). If no solution, fall back to current target pos.
        Vector3 aimPoint = target.position;
        if (fa.bulletSpeed > 0.01f)
        {
            // Math.InterceptionPoint(a=targetPos, b=origin, vA=targetVel, sB=projectileSpeed, out c)
            if (Math.InterceptionPoint(target.position, origin, _targetVel, fa.bulletSpeed, out var c))
                aimPoint = c;
        }

        // 3) Yaw: rotate turret toward aimPoint on XZ plane, with optional limit around initial pose
        UpdateTurretYaw(idx, fa, turretT, aimPoint);

        // 4) Pitch: rotate barrel toward aimPoint, with up/down limits around initial pose
        UpdateBarrelPitch(idx, fa, turretT, barrelT, aimPoint);
    }

    private void UpdateTurretYaw(int idx, FireArm fa, Transform turretT, Vector3 aimPoint)
    {
        Transform parent = turretT.parent;

        Vector3 toAimWorld = aimPoint - turretT.position;
        toAimWorld.y = 0f;
        if (toAimWorld.sqrMagnitude < 0.0001f) return;

        // direction in parent space (so yaw is clean)
        Vector3 dirParent =
            parent != null ? parent.InverseTransformDirection(toAimWorld.normalized) : toAimWorld.normalized;

        float desiredYawDeg = Mathf.Atan2(dirParent.x, dirParent.z) * Mathf.Rad2Deg;

        // interpret desiredYawDeg as "yaw delta from initial"
        // Compute delta relative to initial forward in parent space.
        Vector3 initialFwdWorld = turretT.parent != null
            ? turretT.parent.TransformDirection((_rt[idx].turretInitialLocalRot * Vector3.forward))
            : (_rt[idx].turretInitialLocalRot * Vector3.forward);

        Vector3 initialFwdParent =
            parent != null ? parent.InverseTransformDirection(initialFwdWorld) : initialFwdWorld;

        initialFwdParent.y = 0f;
        if (initialFwdParent.sqrMagnitude < 0.0001f) initialFwdParent = Vector3.forward;

        float initialYawDeg = Mathf.Atan2(initialFwdParent.x, initialFwdParent.z) * Mathf.Rad2Deg;

        float rawDelta = Mathf.DeltaAngle(initialYawDeg, desiredYawDeg);

        float limit = fa.turretRotationLimit;
        if (limit > 0f && limit < 360f)
        {
            float half = limit * 0.5f;
            rawDelta = Mathf.Clamp(rawDelta, -half, half);
        }
        // if >= 360 => no clamp

        float maxStep = Mathf.Max(0f, fa.turretRotationSpeed) * Time.deltaTime;
        _rt[idx].yawDeltaDeg = Mathf.MoveTowardsAngle(_rt[idx].yawDeltaDeg, rawDelta, maxStep);

        // Apply yaw delta around parent up axis (local Y)
        turretT.localRotation = Quaternion.AngleAxis(_rt[idx].yawDeltaDeg, Vector3.up) * _rt[idx].turretInitialLocalRot;
    }

    private void UpdateBarrelPitch(int idx, FireArm fa, Transform turretT, Transform barrelT, Vector3 aimPoint)
    {
        Vector3 toAimWorld = aimPoint - barrelT.position;
        if (toAimWorld.sqrMagnitude < 0.0001f) return;

        // compute in turret space (after yaw) so pitch is independent
        Vector3 dirTurret = turretT.InverseTransformDirection(toAimWorld.normalized);

        // pitch angle: positive = up (looking above horizon)
        // dirTurret.z is forward; y is up.
        float desiredPitchUpDeg = Mathf.Atan2(dirTurret.y, Mathf.Max(0.0001f, dirTurret.z)) * Mathf.Rad2Deg;

        float upLimit = Mathf.Max(0f, fa.barrelElevationLimitUp);
        float downLimit = Mathf.Max(0f, fa.barrelElevationLimitDown);

        desiredPitchUpDeg = Mathf.Clamp(desiredPitchUpDeg, -downLimit, upLimit);

        float maxStep = Mathf.Max(0f, fa.barrelElevationSpeed) * Time.deltaTime;
        _rt[idx].pitchDeg = Mathf.MoveTowards(_rt[idx].pitchDeg, desiredPitchUpDeg, maxStep);

        // Unity: +X rotation pitches DOWN, so apply negative to pitch UP
        barrelT.localRotation = Quaternion.AngleAxis(-_rt[idx].pitchDeg, Vector3.right) * _rt[idx].barrelInitialLocalRot;
    }

    public void FireWeapon(FireArm fireArm)
    {

    }
}
