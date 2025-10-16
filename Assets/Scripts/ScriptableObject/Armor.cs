using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct EquipmentBuff
{
    public Attributes attribute;
    public float value;
}

[System.Serializable]
public class RandomBuff
{
    public EquipmentBuff buff;
    public float weight;
}

[CreateAssetMenu(fileName = "New Armor", menuName = "Inventory/Armor")]
public class Armor : ItemObject
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<RandomBuff> randomBuffs = new List<RandomBuff>();
    public RandomBuff GetRandomBuff()
    {
        if (randomBuffs == null || randomBuffs.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var rb in randomBuffs)
            totalWeight += Mathf.Max(0f, rb.weight);

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);
        float acc = 0f;
        foreach (var rb in randomBuffs)
        {
            acc += Mathf.Max(0f, rb.weight);
            if (roll <= acc) return rb;
        }
        return randomBuffs[randomBuffs.Count - 1];
    }
}
