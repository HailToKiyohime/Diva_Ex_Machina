using UnityEngine;

public class FalconBrain : ModularEntityBrain
{
    private FalconMovement falconMovement;
    public override void Start()
    {
        base.Start();
        falconMovement = modularEntityMovement as FalconMovement;
        if (falconMovement == null)
            Debug.LogError($"{name}: modularEntityMovement 不是 FalconMovement。");
    }

    // 飛行體懸停在空中：先投影到腳下地面，再量到目標點的距離，
    // 這樣 waypointArriveRadius 才是「地面上的水平範圍」，不含懸停高度。
    protected override float DistanceToPoint(Vector3 point)
    {
        Vector3 from = transform.position;

        if (falconMovement != null && falconMovement.HasGroundBelow)
            from = falconMovement.GroundBelowPoint;   // 腳下的地面位置
        else
            from.y = point.y;   // 下方沒地面（飛過峽谷/地圖外）→ 退回純水平距離

        return Vector3.Distance(from, point);
    }
}