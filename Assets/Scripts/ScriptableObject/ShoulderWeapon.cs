using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shoulder Weapon Part", menuName = "Inventory/ShoulderWeapon")]
public class ShoulderWeapon : ItemObject
{
    public GameObject weaponPrefab;
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<RandomBuff> randomBuffs = new List<RandomBuff>();
    public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();
    public MeshRenderer meshRenderer;
    public GameObject bullet;
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
