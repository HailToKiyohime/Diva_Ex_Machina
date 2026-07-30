using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Melee Weapon Part", menuName = "Inventory/MeleeWeaponPart")]
public class MeleeWeaponPart : ItemObject
{
    public GameObject meleeWeaponPartPrefab;
    public WeaponPartType partType;
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<RandomBuff> randomBuffs = new List<RandomBuff>();
    public MeshRenderer meshRenderer;
    public MeleeWeaponPartAttribute attribute;

    [Header("Grip (Handle 零件才有意義)")]
    [Tooltip("主手握點，相對於「零件掛載點」的本地座標。\n" +
             "零件生成時 localPosition/Rotation 會被歸零，所以這個位移是從掛點沿零件本地軸量起。\n" +
             "長柄把握點往桿子下方推，刀刃因此自然離手更遠 —— 攻擊距離會自動變長。\n" +
             "(0,0,0) = 握在掛點原處，等同沒有長柄效果。")]
    public Vector3 mainHandGripOffset = Vector3.zero;

    [Tooltip("副手握點，同樣是相對於零件掛載點的本地座標。\n" +
             "目前還沒人讀，之後做雙手持的 Animation Rigging IK 目標時會用到。")]
    public Vector3 offHandGripOffset = Vector3.zero;

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