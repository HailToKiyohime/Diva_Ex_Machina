using MoreMountains.Feedbacks;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public MMF_Player[] feedback;
    public static SFXManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
public void PlayFeedback(string feedbackName)
    {
        foreach (var fb in feedback)
        {
            if (fb != null && fb.name == feedbackName)
            {
                fb.PlayFeedbacks();
                break;
            }
        }
    }
}
