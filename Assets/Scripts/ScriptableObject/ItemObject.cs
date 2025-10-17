using UnityEngine;

public enum ItemType { 
    Weapon, 
    HeadArmor,
    ChestArmor,
    LeftHandArmor,
    RightHandArmor,
    WaistArmor,
    LegsArmor,
    Thruster,
    Consumable, 
    Material }

public enum Attributes
{
    //Damage Type
    PhysicalDamage,
    ExplosionDamage,
    EnergyDamage,
    ColdDamage,
    //Defence Type
    PhysicalDefense,
    ExplosionDefense,
    EnergyDefense,
    ColdDefense,
    //Critical Attack

}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemObject : ScriptableObject
{
    public string itemName;
    public ItemType type;
    public Sprite icon;
    public int maxStack;
    public GameObject dropPrefab;
}