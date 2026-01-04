using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class EquipmentSlot
{
    public GameObject equipedItem;

    // ✅ 新：允許多種 ItemType
    [SerializeField] public List<ItemType> equipmentTypes = new List<ItemType>();

    // ✅ 舊：先保留，避免你原本 Inspector 設定立刻丟失（遷移用）
    [SerializeField, HideInInspector] public ItemType equipmentType;

    public ItemInstance item;

    public bool Accepts(ItemType t)
    {
        if (equipmentTypes != null && equipmentTypes.Count > 0)
            return equipmentTypes.Contains(t);

        // 若主人還沒填新 list，就用舊欄位當 fallback
        return equipmentType == t;
    }

#if UNITY_EDITOR
    // 自動把舊值塞進 list（只在 list 是空的時候）
    public void MigrateIfNeeded()
    {
        if ((equipmentTypes == null || equipmentTypes.Count == 0) && equipmentType != 0)
            equipmentTypes = new List<ItemType> { equipmentType };
    }
#endif
}


public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }
    public Transform equipmentPage;
    [SerializeField] public List<EquipmentSlot> equipmentSlots = new();

    [Header("Equipment Stat Block")]
    [Tooltip("Left side: Health / Defence / Jump Height / Max EN / EN regeneration / Rate of Climb / Dash Speed / Cruising Speed")]
    public TextMeshProUGUI leftStatBlock;
    [Tooltip("Right side: LH Attack / RH Attack")]
    public TextMeshProUGUI rightStatBlock;

    [Tooltip("TMP <pos=...> for left value column")]
    public float leftValuePos = 180f;
    [Tooltip("TMP <pos=...> for right value column")]
    public float rightValuePos = 150f;

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

        // Left side (8 stats)
        string left =
            LineLeft("Health", ps.GetDisplayHealth()) + "\n" +
            LineLeft("Defence", ps.GetDisplayDefenceAverage()) + "\n" +
            LineLeft("Jump Height", FormatFloat(ps.GetDisplayJumpHeight())) + "\n" +
            LineLeft("Max EN", ps.GetDisplayMaxEnergy()) + "\n" +
            LineLeft("EN regeneration", FormatFloat(ps.GetDisplayEnergyRegen())) + "\n" +
            LineLeft("Rate of Climb", FormatFloat(ps.GetDisplayFlySpeed())) + "\n" +
            LineLeft("Dash Speed", FormatFloat(ps.GetDisplayDashSpeed())) + "\n" +
            LineLeft("Cruising Speed", FormatFloat(ps.GetDisplaySprintSpeed()));

        // Right side (2 stats)
        string right =
            LineRight("LH Attack", ps.GetDisplayLhAttack()) + "\n" +
            LineRight("RH Attack", ps.GetDisplayRhAttack());

        leftStatBlock.text = left;
        rightStatBlock.text = right;
    }

    private string LineLeft(string label, int value)
        => $"{label}:<pos={leftValuePos}>{value}";

    private string LineLeft(string label, string value)
        => $"{label}:<pos={leftValuePos}>{value}";

    private string LineRight(string label, int value)
        => $"{label}:<pos={rightValuePos}>{value}";

    private static string FormatFloat(float v)
        => v.ToString("0.##");

    public bool TryEquipFromInventory(ItemInstance item)
    {
        if (item == null || item.item == null) return false;

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            var slot = equipmentSlots[i];

            if (!slot.Accepts(item.item.type)) continue;

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

        if (!slot.Accepts(item.item.type))
        {
            Debug.LogWarning(
                $"TryEquipWeaponFromInventory: slot[{slotIndex}].equipmentType = {slot.equipmentType}, item.type = {item.item.type}"
            );
            return false;
        }

        if (slot.equipedItem) Destroy(slot.equipedItem);


        if (item is RangeWeaponInstance rwi && rwi.item is RangeWeapon rw)
        {
            // 1) ¥DªZ¾¹±¾¦b mountPoint¡]³õ´ºª«¥ó¡^
            slot.equipedItem = Instantiate(rw.weaponPrefab, mountPoint, false);
            //slot.equipedItem.transform.localRotation = Quaternion.Euler(new Vector3(-90, 90, 0));
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
        }else if (item is MeleeWeaponInstance mwi && mwi.item is MeleeWeapon mw)
        {
            slot.equipedItem = Instantiate(mw.weaponPrefab, mountPoint, false);

            // 找到 instance 內的 grip（用 SO 的名字）
            var gripName = mw.mainHandGrip != null ? mw.mainHandGrip.name : null;
            var grip = !string.IsNullOrEmpty(gripName) ? FindChildRecursive(slot.equipedItem.transform, gripName) : null;

            if (grip != null)
            {
                // 如果主人需要額外修正握把朝向，就改這個 offset
                // 先給 identity，確認位置/旋轉都能貼合，再慢慢調
                Quaternion gripOffset = Quaternion.identity;

                SnapRootSoChildMatches(slot.equipedItem.transform, grip, mountPoint, gripOffset);

                // 接著把武器設為 mountPoint 子物件（保持當前世界姿態）
                slot.equipedItem.transform.SetParent(mountPoint, true);
            }
            else
            {
                Debug.LogWarning($"Equip (Melee): cannot find grip by name '{gripName}' under '{slot.equipedItem.name}'");
            }
            slot.item = item;
            if (mwi.colors != null && mwi.colors.Count > 0 && !string.IsNullOrEmpty(mwi.shaderName))
            {
                var rend = slot.equipedItem.GetComponentInChildren<Renderer>();
                if (rend)
                {
                    var mat = rend.material;
                    if (mwi.shaderName.Contains("Mix 3") && mwi.colors.Count >= 3)
                    {
                        mat.SetColor("_BaseColor", mwi.colors[0]);
                        mat.SetColor("_Layer1Color", mwi.colors[1]);
                        mat.SetColor("_Layer2Color", mwi.colors[2]);
                    }
                    else if (mwi.shaderName.Contains("Mix 4") && mwi.colors.Count >= 4)
                    {
                        mat.SetColor("_BaseColor", mwi.colors[0]);
                        mat.SetColor("_Layer1Color", mwi.colors[1]);
                        mat.SetColor("_Layer2Color", mwi.colors[2]);
                        mat.SetColor("_Layer3Color", mwi.colors[3]);
                    }
                    else if (mwi.shaderName.Contains("Mix 5") && mwi.colors.Count >= 5)
                    {
                        mat.SetColor("_BaseColor", mwi.colors[0]);
                        for (int k = 1; k < 5; k++)
                            mat.SetColor($"_Layer{k}Color", mwi.colors[k]);
                    }
                }
            }


            if (mwi.attachment != null)
            {
                foreach (var attach in mwi.attachment)
                {
                    if (attach?.item is not MeleeWeaponPart mwp) continue;

                    foreach (var ap in mw.attachmentPoints)
                    {
                        if (ap.allowPart != attach.partType) continue;

                        var target = FindChildRecursive(slot.equipedItem.transform, ap.pointTransform.name);
                        if (target == null)
                        {
                            Debug.LogWarning($"Equip: cannot find mount '{ap.pointTransform.name}' on weapon instance");
                            continue;
                        }
                        if (attach.partType == WeaponPartType.Handle)
                        {
                            if (mw.defaultHandle != null)
                            {
                                var t = FindChildRecursive(slot.equipedItem.transform, mw.defaultHandle.name);
                                if (t != null) t.gameObject.SetActive(false);
                            }
                            else
                            {
                                // 保底：如果主人沒填 defaultHandle，就嘗試用常見名字找
                                var t = FindChildRecursive(slot.equipedItem.transform, "default handle");
                                if (t != null) t.gameObject.SetActive(false);
                            }
                        }
                        var part = Instantiate(mwp.meleeWeaponPartPrefab, target, false);
                        part.transform.localPosition = Vector3.zero;
                        part.transform.localRotation = Quaternion.identity;
                        part.transform.localScale = Vector3.one;

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
            Debug.Log($"Equipped weapon '{mw.itemName}' to slot index {slotIndex}");
            return true;
        }
        else
        {
            return false;
        }
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
    private static void SnapRootSoChildMatches(Transform root, Transform child, Transform target, Quaternion childRotationOffset)
    {
        if (root == null || child == null || target == null) return;

        // 1) 先對齊旋轉：把 child 轉到 target（可加 offset）
        Quaternion desiredChildRot = target.rotation * childRotationOffset;
        Quaternion deltaRot = desiredChildRot * Quaternion.Inverse(child.rotation);
        root.rotation = deltaRot * root.rotation;

        // 2) 再對齊位置：把 child 拉到 target
        Vector3 deltaPos = target.position - child.position;
        root.position += deltaPos;
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (equipmentSlots == null) return;
        foreach (var s in equipmentSlots)
            s?.MigrateIfNeeded();
    }
#endif
}

