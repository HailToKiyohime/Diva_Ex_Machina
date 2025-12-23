using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponCoating : ItemObject
{
    public WeaponPartType partType;
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<RandomBuff> randomBuffs = new List<RandomBuff>();
}
