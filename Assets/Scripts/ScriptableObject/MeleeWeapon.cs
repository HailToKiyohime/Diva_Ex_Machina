using System.Collections.Generic;
using UnityEngine;

public enum MeleeWeaponPartAttribute
{
    LongHandle,
    ShortHandle,
    GreatBlade,
    LongBlade,
    ShortBlade,
    HammerHead,
    LanceHead,
}

[CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Inventory/MeleeWeapon")]
public class MeleeWeapon : ItemObject
{
    public GameObject weaponPrefab;

    [Tooltip("這把武器的刀刃屬性。與柄屬性一起交給 MeleeStanceRules 推導出武器類型")]
    public MeleeWeaponPartAttribute attribute;

    [Tooltip("未安裝 Handle 零件時，defaultHandle 視為哪一種柄。\n" +
             "空手可用的武器（Blade 本身）靠這個欄位才能算出武器類型")]
    public MeleeWeaponPartAttribute defaultHandleAttribute = MeleeWeaponPartAttribute.ShortHandle;

    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public List<RandomBuff> randomBuffs = new List<RandomBuff>();
    public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();

    public MeshRenderer meshRenderer;
    public Transform defaultHandle;
    public Transform mainHandGrip;
    public GameObject defaultCoatingEffect;
    public float swordLength = 1.0f;
    public GameObject swordSlash;

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