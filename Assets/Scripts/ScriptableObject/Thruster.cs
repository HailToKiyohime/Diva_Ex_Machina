using UnityEngine;

[CreateAssetMenu(fileName = "New Armor", menuName = "Inventory/ThrusterArmor")]
public class Thruster : Armor
{
    public GameObject normalThrusterFlame;
    public GameObject boostedThrusterFlame;
    public Vector3 thrusterFlameOffset;
}
