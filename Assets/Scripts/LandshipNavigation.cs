using System.Collections.Generic;
using UnityEngine;

public class LandshipNavigation : MonoBehaviour
{
    public static LandshipNavigation Instance { get; private set; }
    [Tooltip("真實船的 root。必須跟 ghostShip 是同一個物件的「本尊 / 分身」關係 ——\n" +
             "ShipNavProjector 靠這兩個 transform 的相對關係做座標投影，指錯不會報錯，\n" +
             "只會讓船上的路徑投影到錯誤的位置。\n\n" +
             "留空會退回這個元件自己的 transform，但只有在 LandshipNavigation 剛好\n" +
             "掛在船的 root 上時才正確，所以建議明確指定。")]
    public Transform realShip;           // Real ship root (ghostShip 的本尊)

    public Transform ghostShip;          // Ghost ship root (contains baked navmesh)
    public Transform core;               // Core target on real ship
    public Transform[] dockingPoints;        // Docking points on the real ship
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;

            // realShip 沒指定時退回自己的 transform。
            // 這個元件目前掛在真實船底下，所以多數情況會是對的 ——
            // 但如果它掛的是子物件而不是 root，投影就會偏掉，所以還是建議明確指定。
            if (realShip == null) realShip = transform;
        }
    }


}