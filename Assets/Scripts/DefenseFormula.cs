/// <summary>
/// 全遊戲共用的防禦公式（玩家、敵人、建築都走這裡）。
///
///   減傷比例 = 防禦 ÷ (防禦 + K)
///   受到傷害 = 原始傷害 × K ÷ (防禦 + K)
///
/// K = 1000 的意思：防禦 1000 時減傷剛好 50%。
///
///   防禦      0    250    500   1000   2000   3000
///   減傷      0%   20%    33%   50%    67%    75%
///
/// 這個公式的好處：
///   · 永遠到不了 100%，不會疊出無敵。
///   · 每 1000 點防禦都讓「等效血量」多一倍原血量（等效血量 = 血量 × (1 + 防禦 / K)），
///     每一點防禦的價值都一樣，數值容易平衡。
///
/// 負防禦（例如之後加入降防的減益）：改成「受到的傷害增加」，最多 2 倍，
///   受到傷害 = 原始傷害 × (2 − K ÷ (K − 防禦))
/// 這樣 −1000 時受到 1.5 倍傷害，不會出現除以零或負數傷害。
/// </summary>
public static class DefenseFormula
{
    /// <summary>防禦等於 K 時，減傷剛好 50%。要調整整體防禦強度就改這個值。</summary>
    public const float K = 1000f;

    /// <summary>傷害倍率：原始傷害乘上這個值就是實際受到的傷害。</summary>
    private static float DamageMultiplier(float defense)
    {
        if (defense >= 0f)
            return K / (K + defense);

        // 負防禦：傷害增加，最多趨近 2 倍
        return 2f - K / (K - defense);
    }

    /// <summary>四種傷害各自乘上對應防禦的倍率後加總。</summary>
    public static float Apply(DamageInfo dmg, float physicalDefense, float explosionDefense,
                              float energyDefense, float coldDefense)
    {
        return dmg.physical * DamageMultiplier(physicalDefense) +
               dmg.explosion * DamageMultiplier(explosionDefense) +
               dmg.energy * DamageMultiplier(energyDefense) +
               dmg.cold * DamageMultiplier(coldDefense);
    }
}
