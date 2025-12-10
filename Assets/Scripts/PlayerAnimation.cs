using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator anim;

    int bipedLayer;
    int hoverLayer;

    public float horizontalMovement;
    public float verticalMovement;

    public float dampTime = 0.1f; // к¤езо╔╢б
    void Awake()
    {
        bipedLayer = anim.GetLayerIndex("Walking_Bipedal");
        hoverLayer = anim.GetLayerIndex("Walking_Hover");
    }
    public void SetBipedMode()
    {
        SetAllLayersOff();
        anim.SetLayerWeight(bipedLayer, 1f);
    }
    public void SetHoverMode()
    {
        SetAllLayersOff();
        anim.SetLayerWeight(hoverLayer, 1f);
    }
    public void SetAllLayersOff()
    {
        anim.SetLayerWeight(bipedLayer, 0f);
        anim.SetLayerWeight(hoverLayer, 0f);
    }

    public void SetMovementParameters(float horizontal, float vertical)
    {
        horizontalMovement = horizontal;
        verticalMovement = vertical;
        anim.SetFloat("x", horizontalMovement);
        anim.SetFloat("y", verticalMovement);
    }
}
