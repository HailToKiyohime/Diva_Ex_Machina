using UnityEngine;

public class LandshipNavigation : MonoBehaviour
{
    public Transform[] dockingPoint;     // docking points (trigger)
    public Transform ghostShip;          // Ghost ship root (contains baked navmesh)
    public Transform core;               // Core target on real ship
}