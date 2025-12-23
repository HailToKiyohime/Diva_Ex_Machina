using System.Collections.Generic;
using UnityEngine;

public enum MeleeWeaponStance
{
    OneHandedPolearm,
    TwoHandedPolearm,
    OneHandedGreatSword,
    TwoHandedGreatSword,
    OneHandedSword,
    TwoHandedSword,
}
public enum MeleeWeaponPartAttribute { 
    LongHandle,
    ShortHandle,
    GreatBlade,
    LongBlade,
    DaggerBlade,
    HammerHead,
    LanceHead,
}


[CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Inventory/MeleeWeapon")]
public class MeleeWeapon : ItemObject
{
    public GameObject meleeWeaponPartPrefab;
    public MeleeWeaponStance meleeWeaponStance;
}
