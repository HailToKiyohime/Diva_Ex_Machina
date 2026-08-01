using UnityEngine;

/// <summary>
/// 近戰連段狀態機。
///
/// 【核心設計：共用計數器】
/// comboIndex 屬於「這一串連段」，不屬於任何一隻手。換手時計數器繼續往前走，
/// 只是換一把武器的招式表去查。
///
///     playedIndex = min(comboIndex, 該手總段數 - 1)
///     是終結技    = (playedIndex == 該手總段數 - 1)
///
/// 效果：短連段武器等於一張「隨時可插入的終結技牌」。長劍只有 3 段，代表
/// comboIndex ≥ 2 的任何時刻按長劍都會直接打出終結技；錘子 5 段則提供更長的
/// 傷害鋪陳。兩種玩法共用同一條規則，不需要任何特例。
///
/// 【冷卻的單一規則】
/// 任何一隻手只要不再是 activeHand，就立即進入 meleeReloadTime。
/// 涵蓋換手、終結技播完、窗口關閉中斷三種情況。
///
/// 【時間點全部由 Animation Event 驅動】
/// clip 上放四個 event，經 PlayerAnimation 轉發到這裡：
///   AnimEvent_MeleeHitboxOn / Off / ComboWindow / StepEnd
/// maxStepDuration 是唯一的保險絲 —— 任一 clip 漏放 StepEnd，玩家會卡住。
///
/// 掛在跟 AttackManager 同一個 GameObject 上。
/// </summary>
[RequireComponent(typeof(AttackManager))]
public class MeleeAttackController : MonoBehaviour
{
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private PlayerAnimation playerAnimation;

    [Header("Data")]
    [Tooltip("武器類型 × 握持 → 連段資料。整個專案通常只需要一份資產。")]
    [SerializeField] private MeleeComboLibrary comboLibrary;

    [Header("Damage")]
    [Tooltip("傷害的歸屬者，通常是玩家根物件。用來排除打到自己。留空則自動抓 AttackManager.playerRb。")]
    [SerializeField] private GameObject attacker;

    [Tooltip("近戰允許打到的層，設成敵人層")]
    [SerializeField] private LayerMask hittableLayers;

    [Header("Animation")]
    [Tooltip("Animator 裡近戰動畫所在的層名稱")]
    [SerializeField] private string meleeLayerName = "One_Hand_Melee_Attack";

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    // ───── 連段狀態（單一實例，因為同時只有一隻手在揮）─────

    private int _comboIndex = -1;          // 共用計數器，-1 = 待機
    private bool _attacking;               // 是否有一段正在播
    private bool _windowOpen;              // 連段窗口是否開啟
    private bool _activeIsLeft;            // 目前是哪隻手在揮
    private Weapon _activeWeapon;
    private MeleeAttackStep _activeStep;

    private float _stepDeadline;           // maxStepDuration 保險絲的到期時刻
    private float _leftCooldownUntil;      // 左手冷卻到什麼時候
    private float _rightCooldownUntil;

    private int _meleeLayerIndex = -1;

    public bool IsAttacking => _attacking;
    public int ComboIndex => _comboIndex;

    private void Awake()
    {
        if (attackManager == null) attackManager = GetComponent<AttackManager>();
        if (playerAnimation == null) playerAnimation = attackManager.playerAnimation;

        if (attacker == null && attackManager.playerRb != null)
            attacker = attackManager.playerRb.gameObject;

        if (playerAnimation != null && playerAnimation.anim != null)
        {
            _meleeLayerIndex = playerAnimation.anim.GetLayerIndex(meleeLayerName);
            if (_meleeLayerIndex < 0)
                Debug.LogError($"[MeleeAttackController] Animator 裡找不到層 '{meleeLayerName}'，近戰動畫不會播放。", this);
        }
    }

    private void Reset()
    {
        attackManager = GetComponent<AttackManager>();
    }

    private void Update()
    {
        // 保險絲：clip 漏放 AnimEvent_MeleeStepEnd 時強制收尾
        if (_attacking && _stepDeadline > 0f && Time.time >= _stepDeadline)
        {
            Debug.LogWarning($"[MeleeAttackController] '{GetActiveStateName()}' 超過 maxStepDuration 才結束。" +
                             $"檢查這個 clip 上有沒有放 AnimEvent_MeleeStepEnd。", this);
            OnStepEnd();
        }

        PushCooldownToUI();
    }

    // ────────────────────────────────────────────────
    //  輸入入口（由 AttackManager.TryAttack 呼叫）
    // ────────────────────────────────────────────────

    /// <summary>
    /// 嘗試揮出一段。回傳 true 表示這次輸入被消耗掉了。
    ///
    /// 呼叫時機由 PlayerMovement.ProcessAttackFacingAndAttack 決定 ——
    /// 角色已經轉到面向目標（或對準超時）之後才會進來，跟遠程走同一條路徑。
    /// </summary>
    public bool TryStartMelee(Weapon w)
    {
        if (w == null || w.kind != HandWeaponKind.Melee) return false;
        if (comboLibrary == null)
        {
            Debug.LogWarning("[MeleeAttackController] 沒有指派 MeleeComboLibrary。", this);
            return false;
        }

        bool isLeft = (w == attackManager.leftHandWeapon);
        bool isRight = (w == attackManager.rightHandWeapon);
        if (!isLeft && !isRight) return false;

        // 這隻手還在冷卻 → 忽略輸入，連段狀態不變（等窗口自然關閉）
        if (IsHandOnCooldown(isLeft)) return false;

        // 揮擊中且窗口還沒開 → 太早，忽略
        if (_attacking && !_windowOpen) return false;

        int stepCount = comboLibrary.GetStepCount(w.melee.weaponClass, w.melee.grip);
        if (stepCount <= 0)
        {
            Debug.LogWarning($"[MeleeAttackController] {w.melee.weaponClass} / {w.melee.grip} 沒有連段資料。", this);
            return false;
        }

        // 共用計數器：起手歸 0，接段則往前走一格
        int nextCombo = (_comboIndex < 0) ? 0 : _comboIndex + 1;

        // 換手時，離開的那隻手立刻進入冷卻
        if (_attacking && _activeIsLeft != isLeft)
            BeginCooldown(_activeIsLeft);

        PlayStep(w, isLeft, nextCombo, stepCount);
        return true;
    }

    private void PlayStep(Weapon w, bool isLeft, int comboIndex, int stepCount)
    {
        // 夾住：計數器超過這隻手的段數時，固定打出它的最後一段（終結技）
        int playedIndex = Mathf.Min(comboIndex, stepCount - 1);

        var step = comboLibrary.GetStep(w.melee.weaponClass, w.melee.grip, playedIndex);
        if (step == null) return;

        _comboIndex = comboIndex;
        _activeWeapon = w;
        _activeIsLeft = isLeft;
        _activeStep = step;
        _attacking = true;
        _windowOpen = false;

        w.meleeRuntime.attacking = true;
        w.meleeRuntime.comboIndex = playedIndex;

        // 上一段的 hitbox 可能還開著（連段接得很快時），先關掉
        CloseAllHitboxes();

        float speed = Mathf.Max(0.01f, w.melee.meleeSpeed);
        _stepDeadline = (step.maxStepDuration > 0f)
            ? Time.time + (step.maxStepDuration / speed)
            : 0f;

        PlayAnimation(step, isLeft, speed);

        if (logStateChanges)
        {
            bool isFinisher = (playedIndex == stepCount - 1);
            Debug.Log($"[Melee] combo={comboIndex} played={playedIndex}/{stepCount - 1} " +
                      $"hand={(isLeft ? "L" : "R")} {(isFinisher ? "FINISHER" : "")} " +
                      $"state={step.GetStateName(isLeft)}", this);
        }
    }

    private void PlayAnimation(MeleeAttackStep step, bool isLeft, float speed)
    {
        if (playerAnimation == null || playerAnimation.anim == null) return;
        if (_meleeLayerIndex < 0) return;

        playerAnimation.SetToAttackLayer();

        // 註：meleeSpeed 目前只縮放保險絲，還沒套用到動畫播放速度。
        // 要讓動畫真的變快，需要在每個 State 上加 Speed Multiplier 參數
        // （Animator State Inspector → Speed → Multiplier 勾選 Parameter）。
        // 等基本連段跑通再處理。
        string stateName = step.GetStateName(isLeft);
        playerAnimation.anim.CrossFade(stateName, step.crossFadeTime, _meleeLayerIndex, 0f);
    }

    // ────────────────────────────────────────────────
    //  Animation Event 回呼（由 PlayerAnimation 轉發）
    // ────────────────────────────────────────────────

    /// <summary>刀刃開始有傷害。</summary>
    public void OnHitboxOn()
    {
        if (!_attacking || _activeWeapon == null || _activeStep == null) return;

        var hitbox = _activeWeapon.melee.hitbox;
        if (hitbox == null)
        {
            Debug.LogWarning("[MeleeAttackController] 這把武器沒有 hitbox，揮空氣。", this);
            return;
        }

        float mul = _activeWeapon.melee.meleeOutput * _activeStep.damageMultiplier;
        var ps = PlayerStats.Instance;

        hitbox.Configure(new MeleeHitData
        {
            attacker = (attacker != null) ? attacker : gameObject,
            baseDamage = new DamageInfo(
                _activeWeapon.damage.physicalDamage * mul,
                _activeWeapon.damage.explosionDamage * mul,
                _activeWeapon.damage.energyDamage * mul,
                _activeWeapon.damage.coldDamage * mul),
            criticalChance = (ps != null) ? ps.criticalChance : 0f,
            criticalMultiplier = (ps != null) ? ps.criticalMultiplier : 1f,
            knockback = _activeStep.knockback,
            hittableLayers = hittableLayers,
        });

        hitbox.Open();
    }

    /// <summary>刀刃結束傷害判定。</summary>
    public void OnHitboxOff()
    {
        CloseAllHitboxes();
    }

    /// <summary>開始接受下一段輸入。</summary>
    public void OnComboWindow()
    {
        if (!_attacking) return;
        _windowOpen = true;
    }

    /// <summary>
    /// 這一段結束。
    ///
    /// 走到這裡代表玩家沒有在窗口內接下一段（接了的話 TryStartMelee 會直接
    /// PlayStep 覆蓋掉狀態，這個 event 對舊的那一段就不再有意義）。
    /// 所以無論是終結技播完還是中途斷掉，處理方式相同：收招 + 進冷卻。
    /// </summary>
    public void OnStepEnd()
    {
        if (!_attacking) return;

        CloseAllHitboxes();
        BeginCooldown(_activeIsLeft);
        ResetCombo();
    }

    // ────────────────────────────────────────────────
    //  狀態轉換
    // ────────────────────────────────────────────────

    private void ResetCombo()
    {
        if (_activeWeapon != null)
        {
            _activeWeapon.meleeRuntime.attacking = false;
            _activeWeapon.meleeRuntime.comboIndex = -1;
        }

        _comboIndex = -1;
        _attacking = false;
        _windowOpen = false;
        _activeWeapon = null;
        _activeStep = null;
        _stepDeadline = 0f;

        if (playerAnimation != null)
            playerAnimation.SetOffAttackLayer();

        if (logStateChanges)
            Debug.Log("[Melee] combo reset", this);
    }

    private void BeginCooldown(bool isLeft)
    {
        var w = isLeft ? attackManager.leftHandWeapon : attackManager.rightHandWeapon;
        if (w == null || w.kind != HandWeaponKind.Melee) return;

        // 該段的 cooldownMultiplier（收招大的終結技可以調高）
        float mul = (_activeStep != null) ? Mathf.Max(0f, _activeStep.cooldownMultiplier) : 1f;
        float duration = Mathf.Max(0f, w.melee.reloadTime * mul);

        if (isLeft) _leftCooldownUntil = Time.time + duration;
        else _rightCooldownUntil = Time.time + duration;

        w.meleeRuntime.reloading = duration > 0f;
        w.meleeRuntime.cooldownNormalized = 0f;

        if (logStateChanges)
            Debug.Log($"[Melee] {(isLeft ? "L" : "R")} cooldown {duration:F2}s", this);
    }

    private bool IsHandOnCooldown(bool isLeft)
    {
        return Time.time < (isLeft ? _leftCooldownUntil : _rightCooldownUntil);
    }

    private void CloseAllHitboxes()
    {
        var l = attackManager.leftHandWeapon;
        var r = attackManager.rightHandWeapon;

        if (l != null && l.melee.hitbox != null) l.melee.hitbox.Close();
        if (r != null && r.melee.hitbox != null) r.melee.hitbox.Close();
    }

    // 把冷卻進度餵給彈藥環（CalcAmmoBarFill 會讀 cooldownNormalized）
    private void PushCooldownToUI()
    {
        UpdateHandCooldown(attackManager.leftHandWeapon, _leftCooldownUntil, true);
        UpdateHandCooldown(attackManager.rightHandWeapon, _rightCooldownUntil, false);
    }

    private void UpdateHandCooldown(Weapon w, float until, bool isLeft)
    {
        if (w == null || w.kind != HandWeaponKind.Melee) return;
        if (!w.meleeRuntime.reloading) return;

        float remaining = until - Time.time;
        if (remaining <= 0f)
        {
            w.meleeRuntime.reloading = false;
            w.meleeRuntime.cooldownNormalized = 1f;
            return;
        }

        float total = Mathf.Max(0.0001f, w.melee.reloadTime);
        w.meleeRuntime.cooldownNormalized = Mathf.Clamp01(1f - (remaining / total));
    }

    private string GetActiveStateName()
    {
        return (_activeStep != null) ? _activeStep.GetStateName(_activeIsLeft) : "(none)";
    }
}