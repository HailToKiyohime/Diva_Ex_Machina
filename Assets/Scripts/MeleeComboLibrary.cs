using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 單段攻擊的突進模式。
/// </summary>
public enum MeleeDashMode
{
    None = 0,       // 這一段不位移
    Forward = 1,    // 沿角色當前面向突進
    ToTarget = 2,   // 朝 PlayerAiming 的鎖定目標突進；沒有目標時退化成 Forward
}

/// <summary>
/// 連段中的一段。
///
/// 【時間軸不在這裡】hitbox 開關、連段窗口、突進起點、該段結束，
/// 全部由動畫 clip 上的 Animation Event 觸發。這個 class 只描述「這一段是什麼」。
///
/// 【傷害公式】
///   最終傷害 = weapon.damage.{type} × weapon.melee.meleeOutput × damageMultiplier
///   四種傷害類型（physical / explosion / energy / cold）共用同一個倍率。
/// </summary>
[System.Serializable]
public class MeleeAttackStep
{
    [Header("Animation")]
    [Tooltip("左手揮這一段時播放的 State 名稱（One_Hand_Melee_Attack 層內）。\n" +
             "建議命名：{Class}_{Grip}_L_{Index}，例如 Sword_1H_L_01")]
    public string animStateNameLeft = "Sword_1H_L_01";

    [Tooltip("右手揮這一段時播放的 State 名稱。\n" +
             "留空則沿用左手的名稱（動畫本身已處理左右對稱時可以只填一個）")]
    public string animStateNameRight = "";

    [Tooltip("CrossFade 過渡時間（秒）。首段建議 0.04~0.06，後續段建議 0.08~0.12")]
    public float crossFadeTime = 0.08f;

    /// <summary>取得這一段在指定手上要播的 State 名稱。右手留空則退回左手。</summary>
    public string GetStateName(bool isLeftHand)
    {
        if (isLeftHand) return animStateNameLeft;
        return string.IsNullOrWhiteSpace(animStateNameRight) ? animStateNameLeft : animStateNameRight;
    }

    [Header("Damage")]
    [Tooltip("這一段的傷害倍率，乘在武器最終傷害之上")]
    public float damageMultiplier = 1f;

    [Tooltip("命中時沿命中方向對敵人施加的擊退力（0 = 不擊退）")]
    public float knockback = 0f;

    [Header("Dash")]
    public MeleeDashMode dashMode = MeleeDashMode.None;

    [Tooltip("突進距離倍率，乘在 PlayerStats.GetMeleeDashDistanceForHand（已含 buff）之上")]
    public float dashDistanceMultiplier = 1f;

    [Tooltip("突進持續時間（秒）。會被 melee.meleeSpeed 反向縮放，速度越快突進越短")]
    public float dashDuration = 0.15f;

    [Tooltip("突進速度曲線。X = 0~1 正規化時間，Y = 0~1 已走完的距離比例。\n" +
             "起手快煞車慢用 EaseOut，蓄力衝刺用 EaseIn")]
    public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("ToTarget 模式：停在目標前方多遠，避免穿模")]
    public float dashStopDistance = 1.5f;

    [Tooltip("ToTarget 模式：目標偏離面向超過這個角度就不追，退化成 Forward（度）")]
    [Range(0f, 180f)]
    public float dashMaxTargetAngle = 90f;

    [Header("Flow")]
    [Tooltip("這一段結束後、連段中斷時的硬直倍率。\n" +
             "乘在 PlayerStats.GetMeleeReloadTimeForHand 之上。收招大的終結技可以調高")]
    public float cooldownMultiplier = 1f;

    [Tooltip("這一段能否被 Dash 取消（給玩家逃生窗口）")]
    public bool cancellableByDash = true;

    [Header("VFX / Feel")]
    [Tooltip("留空則使用 MeleeWeapon.swordSlash")]
    public GameObject slashVfxOverride;

    [Tooltip("命中時的頓幀時長（秒，unscaled）。0 = 不頓幀")]
    public float hitStopDuration = 0f;

    [Header("Safety Fallback")]
    [Tooltip("★ 保險絲（秒）。時間到就強制結束這一段並關閉 hitbox。\n\n" +
             "時間點全部由 clip 上的 Animation Event 驅動，所以只要有一個 clip 漏放\n" +
             "AnimEvent_MeleeStepEnd，玩家就會永久卡在攻擊狀態。這個欄位是唯一的防線。\n" +
             "設成比該段動畫長度稍長即可（例如動畫 0.8 秒就設 1.2）。\n" +
             "0 = 停用，強烈不建議。")]
    public float maxStepDuration = 2f;
}

/// <summary>
/// 一整串連段。單手 / 雙手各一份。
/// </summary>
[System.Serializable]
public class ComboSequence
{
    [Tooltip("依序排列的連段。索引 0 是起手式")]
    public List<MeleeAttackStep> steps = new List<MeleeAttackStep>();

    [Tooltip("打完最後一段後，連段窗口內再按是否回到第 0 段循環。\n" +
             "false = 連段結束，進入硬直")]
    public bool loopCombo = false;

    public bool IsValid => steps != null && steps.Count > 0;
}

/// <summary>
/// 一個武器類型的完整招式組（單手 + 雙手）。
/// </summary>
[System.Serializable]
public class ClassMoveset
{
    public MeleeWeaponClass weaponClass;

    [Tooltip("雙手各持一件武器時使用")]
    public ComboSequence oneHanded = new ComboSequence();

    [Tooltip("另一隻手空著時使用")]
    public ComboSequence twoHanded = new ComboSequence();

    public ComboSequence Get(MeleeGrip grip)
        => (grip == MeleeGrip.TwoHanded) ? twoHanded : oneHanded;
}

/// <summary>
/// 依「武器類型 × 握持方式」查詢的連段資料表。
/// 整個專案通常只需要一份資產，指派給 MeleeAttackController。
///
/// 武器類型由 MeleeStanceRules 從 (blade, handle) 推導，
/// 握持方式由 MeleeStanceResolver.ResolveGrip 在 runtime 決定。
/// </summary>
[CreateAssetMenu(fileName = "MeleeComboLibrary", menuName = "Inventory/Melee Combo Library")]
public class MeleeComboLibrary : ScriptableObject
{
    [Tooltip("每個武器類型一筆。重複的類型以最後一筆為準（會在 Console 警告）")]
    public List<ClassMoveset> movesets = new List<ClassMoveset>();

    private Dictionary<MeleeWeaponClass, ClassMoveset> _lookup;

    private void OnEnable() => _lookup = null;

    private void BuildLookup()
    {
        _lookup = new Dictionary<MeleeWeaponClass, ClassMoveset>();
        if (movesets == null) return;

        foreach (var moveset in movesets)
        {
            if (moveset == null) continue;
            _lookup[moveset.weaponClass] = moveset;
        }
    }

    /// <summary>取得某個類型的招式組；沒有設定則回傳 null。</summary>
    public ClassMoveset GetMoveset(MeleeWeaponClass weaponClass)
    {
        if (_lookup == null) BuildLookup();
        return _lookup.TryGetValue(weaponClass, out var moveset) ? moveset : null;
    }

    /// <summary>取得某個類型 × 握持的連段；沒有設定或是空的則回傳 null。</summary>
    public ComboSequence GetSequence(MeleeWeaponClass weaponClass, MeleeGrip grip)
    {
        var moveset = GetMoveset(weaponClass);
        if (moveset == null) return null;

        var sequence = moveset.Get(grip);
        return (sequence != null && sequence.IsValid) ? sequence : null;
    }

    /// <summary>取得第 index 段；超出範圍或沒設定則回傳 null。</summary>
    public MeleeAttackStep GetStep(MeleeWeaponClass weaponClass, MeleeGrip grip, int index)
    {
        var sequence = GetSequence(weaponClass, grip);
        if (sequence == null) return null;
        if (index < 0 || index >= sequence.steps.Count) return null;

        return sequence.steps[index];
    }

    /// <summary>這個類型 × 握持有幾段；沒設定則回傳 0。</summary>
    public int GetStepCount(MeleeWeaponClass weaponClass, MeleeGrip grip)
    {
        var sequence = GetSequence(weaponClass, grip);
        return (sequence == null) ? 0 : sequence.steps.Count;
    }

    /// <summary>
    /// 算出「目前第 current 段之後」的下一段索引。
    /// 回傳 -1 代表連段結束（該進硬直）。
    /// current 傳 -1 代表起手，會回傳 0。
    /// </summary>
    public int GetNextStepIndex(MeleeWeaponClass weaponClass, MeleeGrip grip, int current)
    {
        var sequence = GetSequence(weaponClass, grip);
        if (sequence == null) return -1;

        int next = current + 1;
        if (next < sequence.steps.Count) return next;

        return sequence.loopCombo ? 0 : -1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _lookup = null;   // Inspector 改動後強制重建

        if (movesets == null) return;

        var seen = new HashSet<MeleeWeaponClass>();
        foreach (var moveset in movesets)
        {
            if (moveset == null) continue;

            if (!seen.Add(moveset.weaponClass))
                Debug.LogWarning($"[MeleeComboLibrary] 類型 '{moveset.weaponClass}' 重複定義，只有最後一筆會生效。", this);

            ValidateSequence(moveset.weaponClass, MeleeGrip.OneHanded, moveset.oneHanded);
            ValidateSequence(moveset.weaponClass, MeleeGrip.TwoHanded, moveset.twoHanded);
        }
    }

    private void ValidateSequence(MeleeWeaponClass weaponClass, MeleeGrip grip, ComboSequence sequence)
    {
        string label = $"{weaponClass} / {grip}";

        if (sequence == null || !sequence.IsValid)
        {
            Debug.LogWarning($"[MeleeComboLibrary] {label} 沒有任何連段，這個組合將無法攻擊。", this);
            return;
        }

        for (int i = 0; i < sequence.steps.Count; i++)
        {
            var step = sequence.steps[i];
            if (step == null) continue;

            if (string.IsNullOrWhiteSpace(step.animStateNameLeft))
                Debug.LogWarning($"[MeleeComboLibrary] {label} 第 {i} 段沒有填 animStateNameLeft。", this);

            if (step.maxStepDuration <= 0f)
                Debug.LogWarning($"[MeleeComboLibrary] {label} 第 {i} 段 maxStepDuration = 0，" +
                                 $"若動畫漏放 StepEnd 事件會卡在攻擊狀態。", this);

            if (step.dashMode != MeleeDashMode.None && step.dashDuration <= 0f)
                Debug.LogWarning($"[MeleeComboLibrary] {label} 第 {i} 段有突進但 dashDuration = 0。", this);

            if (step.dashMode != MeleeDashMode.None && (step.dashCurve == null || step.dashCurve.length == 0))
                Debug.LogWarning($"[MeleeComboLibrary] {label} 第 {i} 段的 dashCurve 是空的，突進距離會恆為 0。", this);
        }
    }
#endif
}