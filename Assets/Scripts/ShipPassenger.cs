using UnityEngine;

public class ShipPassenger : MonoBehaviour
{
    public bool IsOnShip { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mobile Platform")) IsOnShip = true;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mobile Platform")) IsOnShip = false;
    }
}