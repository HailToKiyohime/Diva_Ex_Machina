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
[CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Inventory/MeleeWeapon")]
public class MeleeWeapon : ItemObject
{
    public GameObject meleeWeaponPartPrefab;
    public MeleeWeaponStance meleeWeaponStance;
}
