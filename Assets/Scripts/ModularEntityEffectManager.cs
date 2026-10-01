using MoreMountains.Feedbacks;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class ModularEntityEffectManager : MonoBehaviour
{

    public MMF_Player damageFeedback;

    public void PlayDamageFeedback(float damage)
    {
        damageFeedback.PlayFeedbacks(this.transform.position, RoundForDisplay(damage));
    }

    /// <summary>
    /// 顯示用的傷害數值：四捨五入到整數。
    /// 有造成傷害（> 0）時至少顯示 1，避免被防禦減到很小的傷害顯示成「0」。
    ///
    /// 用 Floor(x + 0.5) 而不是 Mathf.Round：Mathf.Round 遇到 .5 會取偶數（2.5 → 2），
    /// 不是一般認知的四捨五入。
    /// 只影響顯示（Floating Text 吃的 intensity），實際扣血仍然是原本的小數值。
    /// </summary>
    private static float RoundForDisplay(float damage)
    {
        if (damage <= 0f) return 0f;
        return Mathf.Max(1f, Mathf.Floor(damage + 0.5f));
    }
}
