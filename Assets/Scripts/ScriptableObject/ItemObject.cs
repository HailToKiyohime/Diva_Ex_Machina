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
    RangeWeapon,
    WeaponPart,
    Consumable,
    Material,
}

public enum WeaponPartType
{
    Gun,
    Scope,
    Barrel,
    Blade,
    Grip,
    Actuators
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