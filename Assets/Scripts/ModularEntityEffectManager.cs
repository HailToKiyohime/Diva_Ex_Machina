using MoreMountains.Feedbacks;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class ModularEntityEffectManager : MonoBehaviour
{

    public MMF_Player damageFeedback;

    public void PlayDamageFeedback(float damage)
    {
        damageFeedback.PlayFeedbacks(this.transform.position, damage);
    }
}
