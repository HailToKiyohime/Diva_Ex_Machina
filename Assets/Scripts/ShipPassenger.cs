using UnityEngine;

public class ShipPassenger : MonoBehaviour
{
    public bool isOnShip { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mobile Platform")) isOnShip = true;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mobile Platform")) isOnShip = false;
    }
}