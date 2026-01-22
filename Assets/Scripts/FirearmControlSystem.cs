using UnityEngine;

[System.Serializable]
public class FireArm
{
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

    // 記錄每把槍上一幀計算出的攔截瞄準點（避免重算，且與 AimOne 一致）
    private Vector3[] _lastAimPoint;
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

    // ----------------------------
    // Firing runtime (per FireArm)
    // ----------------------------
    private int[] _ammo;                 // currentAmmo in magazine
    private float[] _cooldown;           // time remaining until next firing action allowed
    private bool[] _burstActive;         // currently executing burst (跨幀)
    private int[] _burstRemaining;       // remaining shot events in current burst
    private float[] _burstTimer;         // time until next shot event in burst
    private int[] _nextMuzzleIndex;      // for ShootingInTurn: which muzzle to use next firing action
    private int[] _burstMuzzleIndex;     // for ShootingInTurn: fixed muzzle during the current burst

    private void Awake()
    {
        if (fireArms == null) fireArms = new FireArm[0];
        _rt = new Runtime[fireArms.Length];

        _ammo = new int[fireArms.Length];
        _cooldown = new float[fireArms.Length];
        _burstActive = new bool[fireArms.Length];
        _burstRemaining = new int[fireArms.Length];
        _burstTimer = new float[fireArms.Length];
        _nextMuzzleIndex = new int[fireArms.Length];
        _burstMuzzleIndex = new int[fireArms.Length];
        _lastAimPoint = new Vector3[fireArms.Length];
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

            // init magazine
            _ammo[i] = Mathf.Max(0, fa.magazineSize);
            _cooldown[i] = 0f;
            _burstActive[i] = false;
            _burstRemaining[i] = 0;
            _burstTimer[i] = 0f;
            _nextMuzzleIndex[i] = 0;
            _burstMuzzleIndex[i] = 0;

            // if inspector left reloadTimer > 0, treat as reloading
            fa.reloadTimer = Mathf.Max(0f, fa.reloadTimer);
            if (fa.reloadTimer > 0f) _ammo[i] = 0;
        }
    }

    private void Update()
    {
        if (target == null || fireArms == null) return;

        UpdateTargetVelocity();

        for (int i = 0; i < fireArms.Length; i++)
        {
            AimOne(i, fireArms[i]);
            TickFiring(i, fireArms[i]);

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
        _lastAimPoint[idx] = aimPoint;
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

    // --------------------------------
    // Firing + reload + cooldown tick
    // --------------------------------
    private void TickFiring(int idx, FireArm fa)
    {
        if (fa == null) return;

        float dt = Time.deltaTime;

        // Reload ticking (fa.reloadTimer > 0 means reloading)
        if (fa.reloadTimer > 0f)
        {
            fa.reloadTimer -= dt;
            if (fa.reloadTimer <= 0f)
            {
                fa.reloadTimer = 0f;
                _ammo[idx] = Mathf.Max(0, fa.magazineSize);
            }
            // while reloading, we do not progress burst shots
            return;
        }

        // Cooldown ticking
        if (_cooldown[idx] > 0f)
        {
            _cooldown[idx] -= dt;
            if (_cooldown[idx] < 0f) _cooldown[idx] = 0f;
        }

        // Burst ticking
        if (!_burstActive[idx]) return;

        // wait for next shot event
        if (_burstTimer[idx] > 0f)
        {
            _burstTimer[idx] -= dt;
            if (_burstTimer[idx] > 0f) return;
            _burstTimer[idx] = 0f;
        }

        // If out of ammo mid-burst, start reload and abort burst (規格：不足就 reload，且這次不開火)
        if (_ammo[idx] <= 0)
        {
            AbortBurstAndStartReload(idx, fa);
            return;
        }

        // Fire one shot event
        DoShotEvent(idx, fa);

        // consume 1 round per shot event (Salvo 也只扣 1；總扣 roundsPerFire)
        _ammo[idx] = Mathf.Max(0, _ammo[idx] - 1);

        _burstRemaining[idx]--;

        if (_burstRemaining[idx] <= 0)
        {
            EndBurst(idx, fa);
        }
        else
        {
            // only meaningful when roundsPerFire > 1 (主人指定)
            _burstTimer[idx] = Mathf.Max(0f, fa.timeBetweenShots);
        }
    }

    // -------------------------
    // Public: begin firing action
    // -------------------------
    public void FireWeapon(FireArm fireArm)
    {
        if (fireArm == null) return;

        // Find index
        int idx = -1;
        for (int i = 0; i < fireArms.Length; i++)
        {
            if (ReferenceEquals(fireArms[i], fireArm))
            {
                idx = i;
                break;
            }
        }
        if (idx < 0) return;

        var fa = fireArm;

        // Validate
        if (fa.bulletPrefab == null) return;
        if (fa.muzzles == null || fa.muzzles.Length == 0) return;

        // Ignore while burst/cooldown/reloading (主人指定 ignore)
        if (_burstActive[idx]) return;
        if (_cooldown[idx] > 0f) return;
        if (fa.reloadTimer > 0f) return;

        int roundsPerFire = Mathf.Max(1, Mathf.RoundToInt(fa.roundsPerFire));

        // If not enough ammo for the whole firing action => start reload, do not shoot (主人指定)
        if (_ammo[idx] < roundsPerFire)
        {
            StartReload(idx, fa);
            return;
        }

        // Pick muzzle for ShootingInTurn: same muzzle for the whole burst
        if (fa.firingMode == FiringMode.ShootingInTurn)
        {
            int n = fa.muzzles.Length;
            if (n <= 0) return;
            int next = _nextMuzzleIndex[idx];
            if (next < 0) next = 0;
            if (next >= n) next = next % n;
            _burstMuzzleIndex[idx] = next;
        }
        else
        {
            _burstMuzzleIndex[idx] = 0; // unused in Salvo
        }

        // Start burst
        _burstActive[idx] = true;
        _burstRemaining[idx] = roundsPerFire;
        _burstTimer[idx] = 0f; // first shot immediately

        // Fire first shot event immediately
        DoShotEvent(idx, fa);
        _ammo[idx] = Mathf.Max(0, _ammo[idx] - 1);
        _burstRemaining[idx]--;

        if (_burstRemaining[idx] <= 0)
        {
            EndBurst(idx, fa);
        }
        else
        {
            _burstTimer[idx] = Mathf.Max(0f, fa.timeBetweenShots);
        }
    }

    // -------------------------
    // Shot event implementation
    // -------------------------
    private void DoShotEvent(int idx, FireArm fa)
    {
        int pellets = Mathf.Max(1, Mathf.RoundToInt(fa.bulletPerRound));
        float spread = Mathf.Max(0f, fa.bulletSpread);
        float speed = Mathf.Max(0f, fa.bulletSpeed);

        if (fa.firingMode == FiringMode.Salvo)
        {
            // every shot event: fire ALL muzzles
            for (int m = 0; m < fa.muzzles.Length; m++)
            {
                var muzzle = fa.muzzles[m];
                if (muzzle == null) continue;

                SpawnProjectilesFromMuzzle(muzzle, fa.bulletPrefab, pellets, spread, speed);
            }
        }
        else // ShootingInTurn
        {
            int mi = _burstMuzzleIndex[idx];
            if (mi < 0 || fa.muzzles == null || mi >= fa.muzzles.Length) return;

            var muzzle = fa.muzzles[mi];
            if (muzzle == null) return;

            SpawnProjectilesFromMuzzle(muzzle, fa.bulletPrefab, pellets, spread, speed);
        }
    }

    private void SpawnProjectilesFromMuzzle(Transform muzzle, GameObject bulletPrefab, int pellets, float spreadDeg, float speed)
    {
        Vector3 baseDir = muzzle.forward;

        for (int i = 0; i < pellets; i++)
        {
            Vector3 dir = baseDir;

            if (spreadDeg > 0.0001f)
            {
                // simple cone spread: random yaw/pitch
                float yaw = Random.Range(-spreadDeg, spreadDeg);
                float pitch = Random.Range(-spreadDeg, spreadDeg);
                dir = Quaternion.Euler(pitch, yaw, 0f) * baseDir;
                dir.Normalize();
            }

            Quaternion rot = Quaternion.LookRotation(dir, muzzle.up);
            GameObject b = Instantiate(bulletPrefab, muzzle.position, rot);

            Rigidbody rb = b.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = dir * speed;
            }
        }
    }

    // -------------------------
    // Burst end / reload helpers
    // -------------------------
    private void EndBurst(int idx, FireArm fa)
    {
        _burstActive[idx] = false;
        _burstRemaining[idx] = 0;
        _burstTimer[idx] = 0f;

        // advance muzzle turn AFTER a complete firing action
        if (fa.firingMode == FiringMode.ShootingInTurn && fa.muzzles != null && fa.muzzles.Length > 0)
        {
            _nextMuzzleIndex[idx] = (_nextMuzzleIndex[idx] + 1) % fa.muzzles.Length;
        }

        _cooldown[idx] = Mathf.Max(0f, fa.timeBetweenShooting);
    }

    private void StartReload(int idx, FireArm fa)
    {
        // begin reload (during reload: do not shoot)
        fa.reloadTimer = Mathf.Max(0f, fa.reloadTime);
        _ammo[idx] = 0;

        // if currently bursting (shouldn't happen from FireWeapon due to checks), abort safely
        _burstActive[idx] = false;
        _burstRemaining[idx] = 0;
        _burstTimer[idx] = 0f;
    }

    private void AbortBurstAndStartReload(int idx, FireArm fa)
    {
        _burstActive[idx] = false;
        _burstRemaining[idx] = 0;
        _burstTimer[idx] = 0f;
        StartReload(idx, fa);
    }

    public void RequestFireAll(float coneDeg)
    {
        if (target == null || fireArms == null) return;

        for (int i = 0; i < fireArms.Length; i++)
        {
            var fa = fireArms[i];
            if (fa == null) continue;

            // 只有在「炮口方向」已經對準攔截點一定角度內，才允許觸發一次 firing action
            if (!IsWithinFireCone(i, fa, coneDeg))
                continue;

            // 觸發一次 firing action（內部仍會處理 cooldown / burst / reload / ammo）
            FireWeapon(fa);
        }
    }

    private bool IsWithinFireCone(int idx, FireArm fa, float coneDeg)
    {
        if (fa == null) return false;

        // 以 muzzleCenterPoint 當作「槍口群中心」最穩；沒有就退回 barrel
        Transform dirT = fa.muzzleCenterPoint != null ? fa.muzzleCenterPoint.transform : null;
        if (dirT == null && fa.barrel != null) dirT = fa.barrel.transform;
        if (dirT == null) return false;

        Vector3 origin = (fa.muzzleCenterPoint != null) ? fa.muzzleCenterPoint.position : dirT.position;
        Vector3 toAim = _lastAimPoint[idx] - origin;
        if (toAim.sqrMagnitude < 0.0001f) return false;

        Vector3 fwd = dirT.forward;
        fwd.y = 0f;
        toAim.y = 0f;

        if (fwd.sqrMagnitude < 0.0001f) return false;
        if (toAim.sqrMagnitude < 0.0001f) return false;

        float angle = Vector3.Angle(fwd.normalized, toAim.normalized);
        return angle <= Mathf.Max(0f, coneDeg);
    }
}
