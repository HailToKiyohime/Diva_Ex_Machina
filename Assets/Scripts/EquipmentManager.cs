using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class EquipmentSlot
{
    public GameObject equipedItem;
    public ItemType equipmentType;

    public ItemInstance item;
}


public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }
    public Transform equipmentPage;
    [SerializeField] public List<EquipmentSlot> equipmentSlots = new();

    [Header("Equipment Stat Block")]
    [Tooltip("左側：Health / Defence / Energy Efficiency / Speed")]
    public TextMeshProUGUI leftStatBlock;
    [Tooltip("右側：LH Attack / RH Attack")]
    public TextMeshProUGUI rightStatBlock;
    [Tooltip("若為 true：Infinity 顯示為 ∞；若為 false：顯示為 Infinite")]
    public bool showInfinitySymbol = true;

    private bool _hookedPlayerStats;

    void Awake()
    {
        // If an instance already exists and it's not this one, destroy this new instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // Exit to prevent further execution of this Awake method
        }
        // Otherwise, set this instance as the Singleton
        Instance = this;
    }

    private void OnEnable()
    {
        HookPlayerStatsEvents();
    }

    private void Start()
    {
        HookPlayerStatsEvents();
        RefreshEquipmentStatBlock();
    }

    private void OnDisable()
    {
        UnhookPlayerStatsEvents();
    }

    private void HookPlayerStatsEvents()
    {
        if (_hookedPlayerStats) return;
        if (PlayerStats.Instance == null) return;

        PlayerStats.Instance.OnHandWeaponDataChanged += RefreshEquipmentStatBlock;
        _hookedPlayerStats = true;
    }

    private void UnhookPlayerStatsEvents()
    {
        if (!_hookedPlayerStats) return;
        if (PlayerStats.Instance == null) return;

        PlayerStats.Instance.OnHandWeaponDataChanged -= RefreshEquipmentStatBlock;
        _hookedPlayerStats = false;
    }

    // ===== Equipment Stat Block UI =====
    public void RefreshEquipmentStatBlock()
    {
        if (leftStatBlock == null || rightStatBlock == null)
            return;

        var ps = PlayerStats.Instance;
        if (ps == null)
        {
            leftStatBlock.text = string.Empty;
            rightStatBlock.text = string.Empty;
            return;
        }

        int health = ps.GetDisplayHealth();
        int defence = ps.GetDisplayDefenceAverage();
        int speed = ps.GetDisplaySpeed();
        var eff = ps.GetEnergyEfficiencyInfo();

        string flyStr;
        if (eff.flyInfinite || float.IsPositiveInfinity(eff.flySustainSeconds))
            flyStr = showInfinitySymbol ? "∞" : "Infinite";
        else
            flyStr = $"{FormatNumber(eff.flySustainSeconds)}s";

        string dashCountStr = (eff.dashCountFromFull == int.MaxValue)
            ? (showInfinitySymbol ? "∞" : "Infinite")
            : eff.dashCountFromFull.ToString();

        string dashRateStr = float.IsPositiveInfinity(eff.sustainableDashPerSecond)
            ? (showInfinitySymbol ? "∞/s" : "Infinite/s")
            : $"{FormatNumber(eff.sustainableDashPerSecond)}/s";

        leftStatBlock.text =
            $"Health: {health}\n" +
            $"Defence: {defence}\n" +
            $"EN Efficiency: Fly {flyStr} | Dash {dashCountStr} ({dashRateStr})\n" +
            $"Speed: {speed}";

        rightStatBlock.text =
            $"LH Attack: {ps.GetDisplayLhAttack()}\n" +
            $"RH Attack: {ps.GetDisplayRhAttack()}";
    }

    private static string FormatNumber(float v)
    {
        if (Mathf.Abs(v - Mathf.Round(v)) < 0.0001f)
            return Mathf.RoundToInt(v).ToString();
        return v.ToString("0.##");
    }

    public bool TryEquipFromInventory(ItemInstance item)
    {
        if (item == null || item.item == null) return false;

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            var slot = equipmentSlots[i];

            if (slot.equipmentType != item.item.type) continue;

            // ²M±¼ÂÂ¹ê¨Ò
            if (slot.equipedItem) Destroy(slot.equipedItem);

            Debug.Log($"TryEquipFromInventory: found matching slot {i} for item {item.item.itemName}");
            // ¸Ë¥Ò¡G¥Í¦¨¨Ã¸j°©
            if (item.item is Armor armor && armor.skinnedMeshRenderer)
            {
                Debug.Log($"TryEquipFromInventory: equipping armor {armor.itemName} to slot {i}");
                slot.equipedItem = BoneCombiner.Instance.InstantiateMesh(armor.skinnedMeshRenderer);
                Debug.Log($"TryEquipFromInventory: BoneCombiner instantiated mesh for {armor.itemName}");
                if (!slot.equipedItem)
                {
                    Debug.LogWarning("TryEquipFromInventory: BoneCombiner failed to instantiate mesh");
                    return false; // ¹ê¨Ò¤Æ¥¢±Ñ®É¤£­n·í§@¦¨¥\
                }
                Debug.Log($"TryEquipFromInventory: equipped armor instance created for {armor.itemName}");
                slot.item = item;

                // ­Y¦³¦s¦â±m¡A³o¸Ì®M¦^¥h¡]¥i¿ï¡^
                if (item is ArmorInstance ai && slot.equipedItem)
                {
                    var smr = slot.equipedItem.GetComponent<SkinnedMeshRenderer>();
                    if (smr)
                    {
                        var mat = smr.material; // ¨C¥ó¸Ë³Æ¿W¥ß§÷½è
                        if (!string.IsNullOrEmpty(ai.shaderName))
                        {
                            // ½d¨Ò¡G¨Ì shaderName ®M¦^ÃC¦â
                            if (ai.shaderName.Contains("Mix 3") && ai.colors.Count >= 3)
                            {
                                mat.SetColor("_BaseColor", ai.colors[0]);
                                mat.SetColor("_Layer1Color", ai.colors[1]);
                                mat.SetColor("_Layer2Color", ai.colors[2]);
                            }
                            else if (ai.shaderName.Contains("Mix 4") && ai.colors.Count >= 4)
                            {
                                mat.SetColor("_BaseColor", ai.colors[0]);
                                mat.SetColor("_Layer1Color", ai.colors[1]);
                                mat.SetColor("_Layer2Color", ai.colors[2]);
                                mat.SetColor("_Layer3Color", ai.colors[3]);
                            }
                            else if (ai.shaderName.Contains("Mix 5") && ai.colors.Count >= 5)
                            {
                                mat.SetColor("_BaseColor", ai.colors[0]);
                                for (int k = 1; k < 5; k++) mat.SetColor($"_Layer{k}Color", ai.colors[k]);
                            }
                        }
                    }
                }
                Debug.Log($"TryEquipFromInventory: armor {armor.itemName} equipped to slot {i}");
                // ¡¹ ¸Ë³Æ¦¨¥\«á­«ºâÄÝ©Ê
                PlayerStats.Instance?.RecalculateFromEquipment();
                RefreshEquipmentStatBlock();
                return true; // ¦¨¥\«á¥ß§Y¦^¶Ç
            }
            return false; // Ãþ«¬§k¦X¦ý¤£¬O Armor¡]©Î¯ÊRenderer¡^
        }
        return false; // §ä¤£¨ì¹ïÀ³¼Ñ
    }
    public bool TryEquipWeaponFromInventory(ItemInstance item, Transform mountPoint, int slotIndex)
    {
        if (item == null || item.item == null) return false;

        if (slotIndex < 0 || slotIndex >= equipmentSlots.Count)
        {
            Debug.LogWarning($"TryEquipWeaponFromInventory: invalid slot index {slotIndex}");
            return false;
        }

        var slot = equipmentSlots[slotIndex];

        if (slot.equipmentType != item.item.type)
        {
            Debug.LogWarning(
                $"TryEquipWeaponFromInventory: slot[{slotIndex}].equipmentType = {slot.equipmentType}, item.type = {item.item.type}"
            );
            return false;
        }

        // ²M±¼³o¤@®æ­ì¥»ªºªZ¾¹
        if (slot.equipedItem) Destroy(slot.equipedItem);

        // ¥u³B²z RangeWeaponInstance
        if (item is not RangeWeaponInstance rwi || rwi.item is not RangeWeapon rw)
        {
            Debug.LogWarning("TryEquipWeaponFromInventory: item is not RangeWeaponInstance/RangeWeapon");
            return false;
        }

        // 1) ¥DªZ¾¹±¾¦b mountPoint¡]³õ´ºª«¥ó¡^
        slot.equipedItem = Instantiate(rw.weaponPrefab, mountPoint, false);
        slot.equipedItem.transform.localRotation = Quaternion.Euler(new Vector3(-90, 90, 0));
        slot.item = item;
        if (FindChildRecursive(slot.equipedItem.transform, "MuzzlePoint") != null)
        {
            rwi.muzzlePoint = FindChildRecursive(slot.equipedItem.transform, "MuzzlePoint");
        }


        // 1-1) §â RangeWeaponInstance ¤W¦sªºÃC¦â®M¦^¥h
        if (rwi.colors != null && rwi.colors.Count > 0 && !string.IsNullOrEmpty(rwi.shaderName))
        {
            var rend = slot.equipedItem.GetComponentInChildren<Renderer>();
            if (rend)
            {
                var mat = rend.material; // ¹ê¨Ò§÷½è
                if (rwi.shaderName.Contains("Mix 3") && rwi.colors.Count >= 3)
                {
                    mat.SetColor("_BaseColor", rwi.colors[0]);
                    mat.SetColor("_Layer1Color", rwi.colors[1]);
                    mat.SetColor("_Layer2Color", rwi.colors[2]);
                }
                else if (rwi.shaderName.Contains("Mix 4") && rwi.colors.Count >= 4)
                {
                    mat.SetColor("_BaseColor", rwi.colors[0]);
                    mat.SetColor("_Layer1Color", rwi.colors[1]);
                    mat.SetColor("_Layer2Color", rwi.colors[2]);
                    mat.SetColor("_Layer3Color", rwi.colors[3]);
                }
                else if (rwi.shaderName.Contains("Mix 5") && rwi.colors.Count >= 5)
                {
                    mat.SetColor("_BaseColor", rwi.colors[0]);
                    for (int k = 1; k < 5; k++)
                        mat.SetColor($"_Layer{k}Color", rwi.colors[k]);
                }
            }
        }

        // 2) ªþ¥ó±¾¦b¡u¥DªZ¾¹¹ê¨Ò¡v¤Wªº¦P¦W±¾ÂI¡A¨Ã®M¦^ÃC¦â
        if (rwi.attachment != null)
        {
            foreach (var attach in rwi.attachment)
            {
                if (attach?.item is not RangeWeaponPart rwp) continue;

                foreach (var ap in rw.attachmentPoints)
                {
                    if (ap.allowPart != attach.partType) continue;

                    var target = FindChildRecursive(slot.equipedItem.transform, ap.pointTransform.name);
                    if (target == null)
                    {
                        Debug.LogWarning($"Equip: cannot find mount '{ap.pointTransform.name}' on weapon instance");
                        continue;
                    }

                    var part = Instantiate(rwp.rangeWeaponPartPrefab, target, false);
                    part.transform.localPosition = Vector3.zero;
                    part.transform.localRotation = Quaternion.identity;
                    part.transform.localScale = Vector3.one;
                    if (FindChildRecursive(part.transform, "MuzzlePoint") != null)
                    {
                        rwi.muzzlePoint = FindChildRecursive(part.transform, "MuzzlePoint");
                    }

                    if (attach.colors != null && attach.colors.Count > 0 && !string.IsNullOrEmpty(attach.shaderName))
                    {
                        var partRend = part.GetComponentInChildren<Renderer>();
                        if (partRend)
                        {
                            var mat = partRend.material;
                            if (attach.shaderName.Contains("Mix 3") && attach.colors.Count >= 3)
                            {
                                mat.SetColor("_BaseColor", attach.colors[0]);
                                mat.SetColor("_Layer1Color", attach.colors[1]);
                                mat.SetColor("_Layer2Color", attach.colors[2]);
                            }
                            else if (attach.shaderName.Contains("Mix 4") && attach.colors.Count >= 4)
                            {
                                mat.SetColor("_BaseColor", attach.colors[0]);
                                mat.SetColor("_Layer1Color", attach.colors[1]);
                                mat.SetColor("_Layer2Color", attach.colors[2]);
                                mat.SetColor("_Layer3Color", attach.colors[3]);
                            }
                            else if (attach.shaderName.Contains("Mix 5") && attach.colors.Count >= 5)
                            {
                                mat.SetColor("_BaseColor", attach.colors[0]);
                                for (int k = 1; k < 5; k++)
                                    mat.SetColor($"_Layer{k}Color", attach.colors[k]);
                            }
                        }
                    }
                }
            }
        }

        PlayerStats.Instance?.RecalculateFromEquipment();
        RefreshEquipmentStatBlock();
        Debug.Log($"Equipped weapon '{rw.itemName}' to slot index {slotIndex}");
        return true;
    }


    public void CleanEquipmentSlot(int equipmentSlotsIndex)
    {
        EquipmentSlot slot = equipmentSlots[equipmentSlotsIndex];
        Destroy(slot.equipedItem);
        slot.equipedItem = null;
        slot.item = null;
        PlayerStats.Instance?.RecalculateFromEquipment();
        RefreshEquipmentStatBlock();
    }
    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = FindChildRecursive(root.GetChild(i), name);
            if (c) return c;
        }
        return null;
    }
}

