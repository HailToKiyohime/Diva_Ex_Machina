using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 單一合成插槽資料
[System.Serializable]
public class CraftingSlot
{
    // 在預覽武器上實際組裝出的零件實體 (例如 Receiver / Scope / Barrel 的 GameObject)
    public GameObject assembledPart;
    public Transform attachmentPointTransform;
    // 此插槽允許的武器零件類型
    public WeaponPartType equipmentType;
    // 這個插槽目前使用中的背包物品資料
    public ItemInstance item;
}

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    // ===== Crafting UI 元件 =====
    //製作類型
    public int craftingType = 0; //0:槍武器 1:近戰武器 2:肩扛式武器
    // 左側「合成零件插槽按鈕」們的父物件 (Receiver / Scope / Barrel)
    public Transform craftingPartsButtonParent;
    public Transform rangeWeaponPartsButtonParent;
    public Transform meleeWeaponPartsButtonParent;
    public Transform shoulderWeaponPartsButtonParent;

    // 右側「背包物品按鈕」們的父物件
    public Transform itemsButtonParent;
    // 左側插槽的 ToggleGroup，用來判斷目前有沒有選中插槽
    private ToggleGroup craftingPartsToggleGroup;
    public ToggleGroup rangeWeaponPartsToggleGroup;
    public ToggleGroup meleeWeaponPartsToggleGroup;
    public ToggleGroup shoulderWeaponPartsToggleGroup;
    // 武器預覽的父節點
    public Transform weaponPreviewTransform;
    // 目前場景中的武器預覽實體
    public GameObject weaponPreview;
    // 目前正在預覽 / 組裝中的武器資料 (ScriptableObject)
    public RangeWeapon rangeWeapon;
    public MeleeWeapon meleeWeapon;
    public ShoulderWeapon shoulderWeapon;
    // 記錄每一個合成插槽的狀態
    [SerializeField] public List<CraftingSlot> craftingSlots = new();

    public GameObject rangeWeaponCraftingSlotPage;
    public GameObject meleeWeaponCraftingSlotPage;
    public GameObject shoulderWeaponCraftingSlotPage;

    [Header("UI Button Prefab")]
    // 右側背包物品按鈕的預置物
    public GameObject buttonPrefab;
    // 左側合成插槽按鈕的預置物
    public GameObject craftingSlotPrefab;
    public Sprite barrelIcon;
    public Sprite scopeIcon;
    public Sprite handleIcon;
    public Sprite coatingIcon;
    public Sprite cannonIcon;
    public Sprite cannonBarrelIcon;

    public UIPageSwitch uiPageSwitch;

    public ColorPicker weaponPartColorPicker;
    public GameObject weaponColorBlock;

    public TMP_InputField newWeaponName;


    [Header("Crafting Stat Block")]
    public TextMeshProUGUI leftStatBlock;
    public TextMeshProUGUI rightStatBlock;

    [Header("Tooltip")]
    public CraftingTooltip craftingTooltip;
    [Tooltip("若為 true：bulletPerShot / roundPerTap 為 1 時也會顯示 x 1；若為 false：1 會被省略")]
    public bool showX1Multipliers = false;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        craftingPartsButtonParent = rangeWeaponPartsButtonParent;
        craftingPartsToggleGroup = rangeWeaponPartsToggleGroup;

        if (craftingTooltip == null)
            craftingTooltip = GetComponentInChildren<CraftingTooltip>(true);
        if (craftingTooltip != null)
            craftingTooltip.Init(this);

    }





    // ===== 封裝用小工具 =====

    // 取得顯示名稱（有 newWeaponName 就優先）
    private string GetDisplayName(ItemInstance inv)
    {
        if (inv is RangeWeaponInstance rwi && !string.IsNullOrEmpty(rwi.newWeaponName))
            return rwi.newWeaponName;
        return inv.item.itemName;
    }

    // 判斷 RangeWeaponInstance 是否已經鍛造過（有裝零件）
    private bool IsForgedWeapon(RangeWeaponInstance rwi)
    {
        if (rwi.attachment == null) return false;
        foreach (var part in rwi.attachment)
        {
            if (part != null && part.item != null)
                return true;
        }
        return false;
    }
    private bool IsForgedWeapon(MeleeWeaponInstance mwi)
    {
        if (mwi.attachment == null) return false;
        foreach (var part in mwi.attachment)
        {
            if (part != null && part.item != null)
                return true;
        }
        return false;
    }
    private bool IsForgedWeapon(ShoulderWeaponInstance swi)
    {
        if (swi.attachment == null) return false;
        foreach (var part in swi.attachment)
        {
            if (part != null && part.item != null)
                return true;
        }
        return false;
    }

    // ===== Crafting Stat Block =====

    /// <summary>
    /// Crafting 頁面的 Stat Block：只顯示「成品武器」結果（武器本體 + 已選零件），
    /// 不包含任何角色身上（例如護甲）的 Buff。
    /// </summary>
    private void RefreshCraftingStatBlock()
    {
        if (leftStatBlock == null || rightStatBlock == null)
            return;

        // 尚未選武器：清空
        if (craftingSlots == null || craftingSlots.Count == 0 || craftingSlots[0]?.item is not RangeWeaponInstance)
        {
            leftStatBlock.text = string.Empty;
            rightStatBlock.text = string.Empty;
            return;
        }

        var ws = BuildWeaponStatsFromCraftingSlots_NoArmor();

        // --- 讀武器屬性（目前你的系統是全部加總） ---
        float phys = ws.GetAttribute(Attributes.PhysicalDamage);
        float expl = ws.GetAttribute(Attributes.ExplosionDamage);
        float ener = ws.GetAttribute(Attributes.EnergyDamage);
        float cold = ws.GetAttribute(Attributes.ColdDamage);

        float reloadTime = ws.GetAttribute(Attributes.ReloadTime);
        float timeBetweenShooting = ws.GetAttribute(Attributes.TimeBetweenShooting);
        float spread = ws.GetAttribute(Attributes.Spread);
        float critChance = ws.GetAttribute(Attributes.CriticalChance);
        float critMulti = ws.GetAttribute(Attributes.CriticalMultiplier);

        int bulletPerShot = Mathf.Max(1, Mathf.RoundToInt(ws.GetAttribute(Attributes.BulletPerShot)));
        int roundPerTap = Mathf.Max(1, Mathf.RoundToInt(ws.GetAttribute(Attributes.RoundPerPull)));
        int magazineSize = Mathf.Max(0, Mathf.RoundToInt(ws.GetAttribute(Attributes.MagazineSize)));
        int firingMode = Mathf.RoundToInt(ws.GetAttribute(Attributes.FiringMode));

        // Rapid Fire：rounds / sec
        float rapidFire = (timeBetweenShooting > 0.0001f) ? (1 / timeBetweenShooting) : 0f;

        // --- Format ---
        leftStatBlock.text =
            BuildDamageLine(phys, expl, ener, cold, bulletPerShot, roundPerTap) + "\n" +
            $"Reload: {FormatSeconds(reloadTime)}" + "\n" +
            $"Rapid Fire: {FormatNumber(rapidFire)} r/s" + "\n" +
            $"Spread: {FormatNumber(spread)}°";

        rightStatBlock.text =
            $"Magazine Size: {magazineSize}" + "\n" +
            $"Critical Chance: {FormatPercent01(critChance)}" + "\n" +
            $"Critical Multiplier: x{FormatNumber(critMulti)}" + "\n" +
            $"Firing Mode: {GetFiringModeName(firingMode)}";
    }

    /// <summary>
    /// 把 craftingSlots 裡的「武器本體 + 已選零件」的 buffs 合併成一份 WeaponStats。
    /// 注意：不包含任何手甲/護甲。
    /// </summary>
    private WeaponStats BuildWeaponStatsFromCraftingSlots_NoArmor()
    {
        var ws = new WeaponStats();
        ws.Reset();

        if (craftingSlots == null) return ws;

        foreach (var slot in craftingSlots)
        {
            if (slot?.item == null) continue;

            if (slot.item is RangeWeaponInstance rwi)
            {
                ws.rangeweapon = rwi;
                if (rwi.buffs != null) ws.buffs.AddRange(rwi.buffs);
            }
            else if (slot.item is PartInstance pi)
            {
                if (pi.buffs != null) ws.buffs.AddRange(pi.buffs);
            }
        }

        return ws;
    }

    private string BuildDamageLine(float phys, float expl, float ener, float cold, int bulletPerShot, int roundPerTap)
    {
        // 只顯示非 0 類型；若只有一種傷害，就不要多餘的 '+'
        var parts = new List<string>(4);
        if (Mathf.Abs(phys) > 0.0001f) parts.Add($"<color=#FFFFFF>{FormatNumber(phys)}</color>");
        if (Mathf.Abs(expl) > 0.0001f) parts.Add($"<color=#FF0000>{FormatNumber(expl)}</color>");
        if (Mathf.Abs(ener) > 0.0001f) parts.Add($"<color=#FFFF00>{FormatNumber(ener)}</color>");
        if (Mathf.Abs(cold) > 0.0001f) parts.Add($"<color=#7FD7FF>{FormatNumber(cold)}</color>");

        string damageCore;
        if (parts.Count <= 0)
        {
            damageCore = "0";
        }
        else if (parts.Count == 1)
        {
            damageCore = parts[0];
        }
        else
        {
            damageCore = "(" + string.Join(" +", parts) + ")";
        }

        var sb = new StringBuilder();
        sb.Append("Damage: ").Append(damageCore);

        // bulletPerShot / roundPerTap：主人可選擇 1 要不要顯示
        if (showX1Multipliers || bulletPerShot != 1)
            sb.Append(" x ").Append(bulletPerShot);
        if (showX1Multipliers || roundPerTap != 1)
            sb.Append(" x ").Append(roundPerTap);

        return sb.ToString();
    }

    private static string GetFiringModeName(int mode)
    {
        return mode switch
        {
            0 => "Single",
            1 => "Auto",
            2 => "Charge",
            _ => mode.ToString()
        };
    }

    private static string FormatSeconds(float v)
    {
        if (v < 0f) v = 0f;
        return $"{v:0.##}s";
    }

    private static string FormatPercent01(float v)
    {
        // 你的系統目前用 0~1（例如 0.05 = 5%）
        float pct = v * 100f;
        if (pct < 0f) pct = 0f;
        return $"{pct:0.##}%";
    }

    private static string FormatNumber(float v)
    {
        // 盡量輸出乾淨：接近整數就不顯示小數
        float r = Mathf.Round(v);
        if (Mathf.Abs(v - r) < 0.0001f)
            return ((int)r).ToString();
        return v.ToString("0.##");
    }

    // 左側所有 slot 的 Remove 按鈕關掉
    private void HideAllRemoveButtonsOnCraftingSlots()
    {
        for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
        {
            var removeBtnGo = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(i).gameObject,
                "Remove Equipment Button"
            );
            if (removeBtnGo != null)
                removeBtnGo.SetActive(false);
        }
    }

    // 某個 slot 是否已有東西
    private bool SlotHasEquipment(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= craftingSlots.Count) return false;
        var slot = craftingSlots[slotIndex];
        return slot != null && slot.item != null && slot.item.item != null;
    }

    // 指定 slot 的 Remove 按鈕依照有無裝備刷新
    private void RefreshRemoveButtonForSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= craftingPartsButtonParent.childCount)
            return;

        bool hasEquipment = SlotHasEquipment(slotIndex);
        var removeBtnGo = InventoryManager.Instance.FindChild(
            craftingPartsButtonParent.GetChild(slotIndex).gameObject,
            "Remove Equipment Button"
        );
        if (removeBtnGo != null)
            removeBtnGo.SetActive(hasEquipment);
    }

    // 右側「背包物品按鈕」建立（合成畫面專用）
    private void CreateInventoryButtonForCraftingItem(
        ItemInstance inv,
        bool slotHasEquipment,
        int slotIndex
    )
    {
        var button = Instantiate(buttonPrefab, itemsButtonParent);

        var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
        if (icon != null)
            icon.sprite = inv.item.icon;

        var label = button.transform.Find("Item Name")?.GetComponent<TMP_Text>();
        if (label != null)
            label.text = GetDisplayName(inv);

        var btn = button.GetComponent<Toggle>();
        if (btn == null) return;

        // 插槽已有裝備 → 顯示 Remove 按鈕
        if (slotHasEquipment &&
            slotIndex >= 0 &&
            slotIndex < craftingPartsButtonParent.childCount)
        {
            var removeBtnGo = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(slotIndex).gameObject,
                "Remove Equipment Button"
            );
            if (removeBtnGo != null)
                removeBtnGo.SetActive(true);
        }

        ItemInstance capturedItem = inv;
        // Hover Tooltip（顯示零件差異 / 武器本體 buff）
        var tip = button.GetComponent<CraftingItemTooltipTrigger>();
        if (tip == null) tip = button.AddComponent<CraftingItemTooltipTrigger>();
        tip.Init(this, capturedItem);

        btn.onValueChanged.AddListener(isOn =>
        {
            if (!isOn) return;
            OnClickInventoryItem(capturedItem, btn);
        });

        // 已經裝在某個 slot 的東西不能再選
        foreach (var slot in craftingSlots)
        {
            if (slot != null && capturedItem == slot.item)
            {
                btn.interactable = false;
                break;
            }
        }
    }

    // ===== 提供給 UI 的入口 =====

    // 給 UI 按鈕用的簡化呼叫：打開 Receiver 清單
    public void OpenReceiverInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Gun);
    // 打開 Scope 清單
    public void OpenScopeInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Scope);
    // 打開 Barrel 清單
    public void OpenBarrelInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Barrel);
    //打開 Handle 清單

    // 打開 RangeWeapon的背包清單
    public void OpenRangeWeaponInventory()
    {
        AssignRemovePartButtonListener();

        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
        {
            // 沒選插槽或正在開顏色頁面 → 關掉 Remove 按鈕 + 清空列表
            HideAllRemoveButtonsOnCraftingSlots();
            ClearInventoryButton();
            return;
        }

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {
            if (inv == null || inv.item == null || inv.item.type != ItemType.RangeWeapon)
                continue;

            // 只列出「還沒鍛造的 blueprint 武器」
            if (inv is RangeWeaponInstance rwi && IsForgedWeapon(rwi))
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }

    // 打開「武器零件」的背包清單，會再用 weaponPartType 過濾
    public void OpenRangeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
            return;

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {
            if (inv == null || inv.item == null || inv.item.type != itemType)
                continue;

            if (!(inv.item is RangeWeaponPart rwp) || rwp.partType != weaponPartType)
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }

    public void OpenMeleeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
            return;

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {
            if (inv == null || inv.item == null)
                continue;

            // 原本只允許 inv.item.type == itemType
            // 為了讓 Coating 更穩（避免你未來把 Coating 設成不同 ItemType），這裡對 Coating 放寬一次
            if (inv.item.type != itemType && inv.item is not MeleeWeaponCoating)
                continue;

            bool matched =
                (inv.item is MeleeWeaponPart mwp && mwp.partType == weaponPartType) ||
                (inv.item is MeleeWeaponCoating mwc && mwc.partType == weaponPartType);

            if (!matched)
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }
    //打開 Melee Weapon 清單
    public void OpenMeleeWeaponInventory()
    {
        AssignRemovePartButtonListener();
        Debug.Log("OpenMeleeWeaponInventory called.:" + craftingPartsToggleGroup.name);
        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
        {
            Debug.Log("No slot selected or color page is open.");
            // 沒選插槽或正在開顏色頁面 → 關掉 Remove 按鈕 + 清空列表
            HideAllRemoveButtonsOnCraftingSlots();
            ClearInventoryButton();
            return;
        }

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {

            if (inv == null || inv.item == null || inv.item.type != ItemType.MeleeWeapon)
                continue;

            // 只列出「還沒鍛造的 blueprint 武器」
            if (inv is MeleeWeaponInstance mwi && IsForgedWeapon(mwi))
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }
    //打開 shoulder Weapon 清單
    public void OpenShoulderWeaponInventory()
    {
        AssignRemovePartButtonListener();
        Debug.Log("OpenShoulderWeaponInventory called.:" + craftingPartsToggleGroup.name);
        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
        {
            Debug.Log("No slot selected or color page is open.");
            // 沒選插槽或正在開顏色頁面 → 關掉 Remove 按鈕 + 清空列表
            HideAllRemoveButtonsOnCraftingSlots();
            ClearInventoryButton();
            return;
        }
        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {

            if (inv == null || inv.item == null || inv.item.type != ItemType.ShoulderCannon)
                continue;

            // 只列出「還沒鍛造的 blueprint 武器」
            if (inv is ShoulderWeaponInstance swi && IsForgedWeapon(swi))
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }
    public void OpenShoulderWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        if (!craftingPartsToggleGroup.AnyTogglesOn() || weaponColorBlock.activeSelf)
            return;

        ClearInventoryButton();

        int slotIndex = GetSelectedSlotIndex();
        HideAllRemoveButtonsOnCraftingSlots();
        bool slotHasEquipment = SlotHasEquipment(slotIndex);

        foreach (var inv in InventoryManager.Instance.inventory)
        {
            if (inv == null || inv.item == null || inv.item.type != itemType)
                continue;

            if (!(inv.item is ShoulderWeaponPart swp) || swp.partType != weaponPartType)
                continue;

            CreateInventoryButtonForCraftingItem(inv, slotHasEquipment, slotIndex);
        }
    }
    // 右側「背包物品按鈕」被勾選時的處理
    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        // 整把武器
        if (item.item is RangeWeapon rw)
        {
            // 先清掉舊預覽與舊插槽（避免重複產生 part slot）
            if (weaponPreview != null)
                Destroy(weaponPreview);

            CleanCraftingSlots();   // 清左側 UI + craftingSlots

            // 建立新的武器預覽
            GameObject weapon = Instantiate(rw.weaponPrefab, weaponPreviewTransform);
            weaponPreview = weapon;
            rangeWeapon = rw;

            // 槽 0：整把武器本體
            craftingSlots.Add(new CraftingSlot
            {
                assembledPart = weapon,
                attachmentPointTransform = null,
                equipmentType = WeaponPartType.Gun,
                item = item
            });

            if (item is RangeWeaponInstance rwi)
            {
                newWeaponName.text = !string.IsNullOrEmpty(rwi.newWeaponName)
                    ? rwi.newWeaponName
                    : item.item.itemName;
            }

            // 之後的槽：各個掛點
            foreach (var at in rw.attachmentPoints)
            {
                string slotName = at.pointTransform.name;
                craftingSlots.Add(new CraftingSlot
                {
                    assembledPart = null,
                    attachmentPointTransform = FindChildRecursive(weaponPreview.transform, slotName),
                    equipmentType = at.allowPart,
                    item = null
                });
            }

            // 依照新的 craftingSlots 重新產生左側插槽按鈕
            CreateCraftingSlots();

            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
        }
        else if (item.item is MeleeWeapon mw)
        {
            // 先清掉舊預覽與舊插槽（避免重複產生 part slot）
            if (weaponPreview != null)
                Destroy(weaponPreview);
            CleanCraftingSlots();   // 清左側 UI + craftingSlots
            RefreshMeleeDefaultHandleState();
            RefreshMeleeDefaultCoatingState();
            // 建立新的武器預覽

            GameObject weapon = Instantiate(mw.weaponPrefab, weaponPreviewTransform);
            weaponPreview = weapon;
            meleeWeapon = mw;

            // 槽 0：整把武器本體
            craftingSlots.Add(new CraftingSlot
            {
                assembledPart = weapon,
                attachmentPointTransform = null,
                equipmentType = WeaponPartType.Blade,
                item = item
            });

            if (item is MeleeWeaponInstance mwi)
            {
                newWeaponName.text = !string.IsNullOrEmpty(mwi.newWeaponName)
                    ? mwi.newWeaponName
                    : item.item.itemName;
            }

            // 之後的槽：各個掛點
            foreach (var at in mw.attachmentPoints)
            {
                string slotName = at.pointTransform.name;
                craftingSlots.Add(new CraftingSlot
                {
                    assembledPart = null,
                    attachmentPointTransform = FindChildRecursive(weaponPreview.transform, slotName),
                    equipmentType = at.allowPart,
                    item = null
                });
            }
            // 依照新的 craftingSlots 重新產生左側插槽按鈕
            CreateCraftingSlots();

            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }


        }
        // 武器零件
        else if (item.item is RangeWeaponPart rwp)
        {
            if (rangeWeapon == null || weaponPreview == null)
                return;

            foreach (var attachmentPoint in craftingSlots)
            {
                if (rwp.partType != attachmentPoint.equipmentType ||
                    attachmentPoint.attachmentPointTransform == null)
                    continue;
                if (attachmentPoint.assembledPart != null)
                {
                    Destroy(attachmentPoint.assembledPart);
                }

                GameObject part = Instantiate(rwp.rangeWeaponPartPrefab, attachmentPoint.attachmentPointTransform);
                attachmentPoint.assembledPart = part;
                attachmentPoint.item = item;
            }
            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
        }
        else if (item.item is MeleeWeaponPart mwp)
        {
            if (meleeWeapon == null || weaponPreview == null)
                return;

            foreach (var attachmentPoint in craftingSlots)
            {
                if (mwp.partType != attachmentPoint.equipmentType ||
                    attachmentPoint.attachmentPointTransform == null)
                    continue;
                if (attachmentPoint.assembledPart != null)
                {
                    Destroy(attachmentPoint.assembledPart);
                }

                GameObject part = Instantiate(mwp.meleeWeaponPartPrefab, attachmentPoint.attachmentPointTransform);
                attachmentPoint.assembledPart = part;
                attachmentPoint.item = item;
            }
            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
            RefreshMeleeDefaultHandleState();
        }
        else if (item.item is MeleeWeaponCoating mwc)
        {
            if (meleeWeapon == null || weaponPreview == null)
                return;

            foreach (var attachmentPoint in craftingSlots)
            {
                if (mwc.partType != attachmentPoint.equipmentType ||
                    attachmentPoint.attachmentPointTransform == null)
                    continue;

                if (attachmentPoint.assembledPart != null)
                    Destroy(attachmentPoint.assembledPart);

                GameObject part = Instantiate(mwc.meleeCoatingPrefab, attachmentPoint.attachmentPointTransform);
                part.transform.localScale = new Vector3(meleeWeapon.swordLength, 1, 1);
                attachmentPoint.assembledPart = part;
                attachmentPoint.item = item;
            }

            // 右側只留下這個 Coating 為選中（跟你其他分支一致）
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
            RefreshMeleeDefaultCoatingState();
        }else if (item.item is ShoulderWeapon sw)
        {
            // 先清掉舊預覽與舊插槽（避免重複產生 part slot）
            if (weaponPreview != null)
                Destroy(weaponPreview);

            CleanCraftingSlots();   // 清左側 UI + craftingSlots

            // 建立新的武器預覽
            GameObject weapon = Instantiate(sw.weaponPrefab, weaponPreviewTransform);
            weaponPreview = weapon;
            shoulderWeapon = sw;

            // 槽 0：整把武器本體
            craftingSlots.Add(new CraftingSlot
            {
                assembledPart = weapon,
                attachmentPointTransform = null,
                equipmentType = WeaponPartType.Cannon,
                item = item
            });

            if (item is ShoulderWeaponInstance swi)
            {
                newWeaponName.text = !string.IsNullOrEmpty(swi.newWeaponName)
                    ? swi.newWeaponName
                    : item.item.itemName;
            }

            // 之後的槽：各個掛點
            foreach (var at in sw.attachmentPoints)
            {
                string slotName = at.pointTransform.name;
                craftingSlots.Add(new CraftingSlot
                {
                    assembledPart = null,
                    attachmentPointTransform = FindChildRecursive(weaponPreview.transform, slotName),
                    equipmentType = at.allowPart,
                    item = null
                });
            }

            // 依照新的 craftingSlots 重新產生左側插槽按鈕
            CreateCraftingSlots();

            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
        }else if (item.item is ShoulderWeaponPart swp)
        {
            if (shoulderWeapon == null || weaponPreview == null)
                return;

            foreach (var attachmentPoint in craftingSlots)
            {
                if (swp.partType != attachmentPoint.equipmentType ||
                    attachmentPoint.attachmentPointTransform == null)
                    continue;
                if (attachmentPoint.assembledPart != null)
                {
                    Destroy(attachmentPoint.assembledPart);
                }

                GameObject part = Instantiate(swp.shoulderWeaponPartPrefab, attachmentPoint.attachmentPointTransform);
                attachmentPoint.assembledPart = part;
                attachmentPoint.item = item;
            }
            // 右側只留下這把武器為選中
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.isOn = false;
                    t.interactable = true;
                }
            }
        }

            // 更新左側插槽圖示
            int selectedIndex = GetSelectedSlotIndex();
        if (selectedIndex >= 0 && selectedIndex < craftingPartsButtonParent.childCount)
        {
            Image spriteImage = InventoryManager.Instance
                .FindChild(craftingPartsButtonParent.GetChild(selectedIndex).gameObject, "Item Icon")
                .GetComponent<Image>();

            spriteImage.sprite = item.item.icon;
            spriteImage.color = new Color(1, 1, 1, 1);
        }

        // 更新 Remove 按鈕顯示
        RefreshRemoveButtonForSlot(GetSelectedSlotIndex());

        // 這個物品已被使用，不允許再點
        btn.interactable = false;

        // 刷新 Crafting Stat Block（武器本體 + 已選零件）
        RefreshCraftingStatBlock();
        RefreshTooltipIfVisible();
    }

    // 清空右側背包按鈕列表
    public void ClearInventoryButton()
    {
        if (craftingTooltip != null) craftingTooltip.HideTooltip();
        for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(itemsButtonParent.GetChild(i).gameObject);
    }

    // 從左側合成插槽中找出目前被勾選的插槽 index
    public int GetSelectedSlotIndex()
    {
        for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
        {
            var child = craftingPartsButtonParent.GetChild(i);
            var t = child.GetComponentInChildren<Toggle>(true);
            if (t != null && t.isOn)
                return i;
        }
        return -1;
    }

    // 遞迴尋找指定名稱的子物件
    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // Tooltip 已搬到 CraftingTooltip.cs；這裡保留入口供 CraftingManager 內部刷新
    private void RefreshTooltipIfVisible()
    {
        if (craftingTooltip != null)
            craftingTooltip.RefreshTooltipIfVisible();
    }

    public void ShowTooltip(ItemInstance item)
    {
        if (craftingTooltip != null)
            craftingTooltip.ShowTooltip(item);
    }

    public void HideTooltip()
    {
        if (craftingTooltip != null)
            craftingTooltip.HideTooltip();
    }

    public void removePart()
    {
        int index = GetSelectedSlotIndex();
        if (index < 0 || index >= craftingSlots.Count)
            return;

        if (index == 0) // 移除整把武器
        {
            foreach (var slot in craftingSlots)
            {
                if (slot.assembledPart != null)
                    Destroy(slot.assembledPart);
            }

            // 移除左側 UI（保留第 0 個按鈕）
            for (int i = craftingPartsButtonParent.childCount - 1; i > 0; i--)
            {
                Destroy(craftingPartsButtonParent.GetChild(i).gameObject);
            }

            craftingSlots.Clear();
            weaponPreview = null;
            rangeWeapon = null;

            // 重置第 0 個 icon
            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(0).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);

            RefreshInventoryAfterRemove(0, default);

            RefreshCraftingStatBlock();
            RefreshTooltipIfVisible();
            RefreshMeleeDefaultHandleState();
            RefreshMeleeDefaultCoatingState();
        }
        else
        {
            var slot = craftingSlots[index];
            if (slot.assembledPart != null)
                Destroy(slot.assembledPart);

            slot.assembledPart = null;
            slot.item = null;

            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(index).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);

            RefreshInventoryAfterRemove(index, slot.equipmentType);

            RefreshCraftingStatBlock();
            RefreshTooltipIfVisible();
            RefreshMeleeDefaultHandleState();
            RefreshMeleeDefaultCoatingState();
        }
    }

    public void removePart(int index)
    {
        if (index < 0 || index >= craftingSlots.Count)
            return;

        if (index == 0) // 移除整把武器
        {
            foreach (var slot in craftingSlots)
            {
                if (slot.assembledPart != null)
                    Destroy(slot.assembledPart);
            }

            // 移除左側 UI（保留第 0 個按鈕）
            for (int i = craftingPartsButtonParent.childCount - 1; i > 0; i--)
            {
                Destroy(craftingPartsButtonParent.GetChild(i).gameObject);
            }

            craftingSlots.Clear();
            weaponPreview = null;
            rangeWeapon = null;

            // 重置第 0 個 icon
            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(0).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);

            OpenRangeWeaponInventory();

            RefreshCraftingStatBlock();
            RefreshTooltipIfVisible();
            RefreshMeleeDefaultHandleState();
            RefreshMeleeDefaultCoatingState();
        }
        else
        {
            var slot = craftingSlots[index];
            if (slot.assembledPart != null)
                Destroy(slot.assembledPart);

            slot.assembledPart = null;
            slot.item = null;

            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(index).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);

            OpenRangeWeaponPartsInventory(ItemType.WeaponPart, slot.equipmentType);

            RefreshCraftingStatBlock();
            RefreshTooltipIfVisible();
            RefreshMeleeDefaultHandleState();
            RefreshMeleeDefaultCoatingState();
        }
    }


    public void AssignRemovePartButtonListener()
    {
        foreach (Transform slotButton in craftingPartsButtonParent)
        {
            var removeBtnGo = InventoryManager.Instance.FindChild(
                slotButton.gameObject,
                "Remove Equipment Button"
            );
            if (removeBtnGo != null)
            {
                var button = removeBtnGo.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(removePart);
                }
            }
        }
    }

    public void CreateCraftingSlots()
    {
        if (craftingSlots.Count <= 1) return;

        // 從 index 1 開始（0 是整把武器按鈕）
        for (int i = 1; i < craftingSlots.Count; i++)
        {
            var slot = craftingSlots[i];
            var slotButton = Instantiate(craftingSlotPrefab, craftingPartsButtonParent);

            Image icon = FindChildRecursive(slotButton.transform, "Slot Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                if (slot.equipmentType == WeaponPartType.Barrel)
                    icon.sprite = barrelIcon;
                else if (slot.equipmentType == WeaponPartType.Scope)
                    icon.sprite = scopeIcon;
                else if (slot.equipmentType == WeaponPartType.Handle)
                    icon.sprite = handleIcon;
                else if (slot.equipmentType == WeaponPartType.Coating)
                    icon.sprite = coatingIcon;
                else if (slot.equipmentType == WeaponPartType.Cannon)
                    icon.sprite = cannonIcon;
                else if (slot.equipmentType == WeaponPartType.CannonBarrel)
                    icon.sprite = cannonBarrelIcon;
            }

            var btn = slotButton.GetComponent<Toggle>();
            if (btn != null)
            {
                btn.group = craftingPartsToggleGroup;
                uiPageSwitch.toggles.Add(btn);

                int slotIndex = i;
                var capturedSlot = slot;

                btn.onValueChanged.AddListener(isOn =>
                {
                    if (!isOn)
                    {
                        HideAllRemoveButtonsOnCraftingSlots();
                        ClearInventoryButton();
                        return;
                    }
                    if (craftingType == 0)
                    {
                        OpenRangeWeaponPartsInventory(ItemType.WeaponPart, capturedSlot.equipmentType);
                    }
                    else if (craftingType == 1)
                    {
                        OpenMeleeWeaponPartsInventory(ItemType.WeaponPart, capturedSlot.equipmentType);
                    }else if(craftingType == 2)
                    {
                        OpenShoulderWeaponPartsInventory(ItemType.WeaponPart, capturedSlot.equipmentType);
                    }
                        SelectWeaponPartToColor(slotIndex);
                });
            }
        }
        uiPageSwitch.UpdateToggles();
    }

    public void SelectWeaponPartToColor(int equipmentSlotsIndex)
    {
        if (craftingSlots == null)
        {
            Debug.LogWarning("SelectWeaponPartToColor: craftingSlots is null");
            return;
        }

        if (equipmentSlotsIndex < 0 || equipmentSlotsIndex >= craftingSlots.Count)
        {
            Debug.LogWarning(
                $"SelectWeaponPartToColor: index out of range. index={equipmentSlotsIndex}, count={craftingSlots.Count}"
            );
            return;
        }

        var slot = craftingSlots[equipmentSlotsIndex];

        // 顏色區塊沒開就不用做事
        if (!weaponColorBlock.activeSelf)
            return;

        if (slot != null && slot.assembledPart != null && craftingPartsToggleGroup.AnyTogglesOn())
        {
            weaponPartColorPicker.targetItemInstance = slot.item;   // ✅ 加這行：回寫顏色用
            weaponPartColorPicker.targetGameObject = slot.assembledPart;
            weaponPartColorPicker.AddTargetMaterialsToList();
            weaponPartColorPicker.CreateButtons();
        }
        else
        {
            weaponPartColorPicker.targetItemInstance = null;
            weaponPartColorPicker.targetGameObject = null;
            weaponPartColorPicker.targetMaterials = new List<Material>();
            weaponPartColorPicker.currentMaterialIndex = -1;
            weaponPartColorPicker.currentTextureIndex = -1;
            weaponPartColorPicker.ClearnButton();
        }
    }

    public void OpenColorPage()
    {
        int index = GetSelectedSlotIndex();

        // 把所有裝備槽上的「Remove Equipment Button」先關掉
        HideAllRemoveButtonsOnCraftingSlots();

        var slots = craftingSlots;
        if (index < 0 || index >= slots.Count)
            return;
        if (!weaponColorBlock.activeSelf) return;

        var go = slots[index].assembledPart;
        if (!go)
        {
            weaponPartColorPicker.targetGameObject = null;
            weaponPartColorPicker.targetMaterials = new List<Material>();
            weaponPartColorPicker.currentMaterialIndex = -1;
            weaponPartColorPicker.currentTextureIndex = -1;
            weaponPartColorPicker.ClearnButton();
        }
        else
        {
            weaponPartColorPicker.targetItemInstance = slots[index].item;  // ✅ 加這行
            weaponPartColorPicker.targetGameObject = go;
            weaponPartColorPicker.AddTargetMaterialsToList();
            weaponPartColorPicker.CreateButtons();
            if (slots[index].item is RangeWeaponInstance rwi)
            {
                // 可選：用材質當作 fallback
                var renderer = go.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var mat = renderer.material;
                    if (mat != null && mat.HasProperty("_BaseColor"))
                        weaponPartColorPicker.CursorToColor(mat.GetColor("_BaseColor"));
                }
            }
        }
    }

    public GameObject FindChild(GameObject parentObject, string targetName)
    {
        foreach (Transform childTransform in parentObject.transform)
        {
            if (childTransform.gameObject.name == targetName)
                return childTransform.gameObject;

            GameObject found = FindChild(childTransform.gameObject, targetName);
            if (found != null)
                return found;
        }
        return null;
    }

    public void Forge()
    {
        Debug.Log("Forge: start");

        if (craftingSlots == null || craftingSlots.Count == 0)
        {
            Debug.LogWarning("Forge: craftingSlots is empty");
            return;
        }

        var baseSlot = craftingSlots[0];
        if (baseSlot == null || baseSlot.item == null)
        {
            Debug.LogWarning("Forge: base slot has no item");
            return;
        }
        Debug.Log($"Forge baseSlot.item runtime type = {baseSlot.item.GetType().Name}");
        Debug.Log($"Forge baseSlot.item.item SO type = {baseSlot.item.item?.GetType().Name}");
        // =========================
        // Range Weapon
        // =========================
        if (baseSlot.item is RangeWeaponInstance rangeWeaponInstance)
        {
            if (baseSlot.assembledPart != null)
            {
                string baseShaderName;
                var baseColors = ExtractColorsFromGameObject(baseSlot.assembledPart, out baseShaderName);

                if (baseColors.Count > 0)
                {
                    rangeWeaponInstance.colors = baseColors;
                    rangeWeaponInstance.shaderName = baseShaderName;
                }
            }

            var rangeWeaponPartInstances = new List<PartInstance>();
            for (int i = 1; i < craftingSlots.Count; i++)
            {
                var slot = craftingSlots[i];
                if (slot == null || slot.item == null) continue;

                if (slot.item is PartInstance pi)
                {
                    if (slot.assembledPart != null)
                    {
                        string partShaderName;
                        var partColors = ExtractColorsFromGameObject(slot.assembledPart, out partShaderName);

                        if (partColors.Count > 0)
                        {
                            pi.colors = partColors;
                            pi.shaderName = partShaderName;
                        }
                    }

                    rangeWeaponPartInstances.Add(pi);
                }
                else
                {
                    Debug.LogWarning($"Forge: slot[{i}] item is not PartInstance, actual={slot.item.GetType().Name}");
                }
            }

            if (!string.IsNullOrEmpty(newWeaponName.text))
            {
                Debug.Log("Forge: setting new weapon name to " + newWeaponName.text);
                rangeWeaponInstance.newWeaponName = newWeaponName.text;
            }

            if (rangeWeaponPartInstances.Count == 0)
            {
                Debug.LogWarning("Forge: no parts selected, cannot forge");
                return;
            }

            InventoryManager.Instance.AddCraftedRangeWeaponToInventory(rangeWeaponInstance, rangeWeaponPartInstances);

            InventoryManager.Instance.RemoveItemFromInventory(baseSlot.item);
            foreach (var part in rangeWeaponPartInstances)
                InventoryManager.Instance.RemoveItemFromInventory(part);

            if (weaponPreview != null)
            {
                Destroy(weaponPreview);
                weaponPreview = null;
            }
            CleanCraftingSlots();

            Debug.Log("Forge: success (Range)");
            return;
        }

        // =========================
        // Melee Weapon
        // =========================
        if (baseSlot.item is MeleeWeaponInstance meleeWeaponInstance)
        {
            if (baseSlot.assembledPart != null)
            {
                string baseShaderName;
                var baseColors = ExtractColorsFromGameObject(baseSlot.assembledPart, out baseShaderName);

                if (baseColors.Count > 0)
                {
                    meleeWeaponInstance.colors = baseColors;
                    meleeWeaponInstance.shaderName = baseShaderName;
                }
            }

            var meleeWeaponPartInstances = new List<PartInstance>();
            for (int i = 1; i < craftingSlots.Count; i++)
            {
                var slot = craftingSlots[i];
                if (slot == null || slot.item == null) continue;

                if (slot.item is PartInstance pi)
                {
                    if (slot.assembledPart != null)
                    {
                        string partShaderName;
                        List<Color> partColors;

                        if (pi.partType == WeaponPartType.Coating || pi.item is MeleeWeaponCoating)
                            partColors = ExtractCoatingColorsFromGameObject(slot.assembledPart, out partShaderName);
                        else
                            partColors = ExtractColorsFromGameObject(slot.assembledPart, out partShaderName);

                        if (partColors.Count > 0)
                        {
                            pi.colors = partColors;
                            pi.shaderName = partShaderName;
                        }
                    }

                    meleeWeaponPartInstances.Add(pi);
                }
                else
                {
                    Debug.LogWarning($"Forge: slot[{i}] item is not PartInstance, actual={slot.item.GetType().Name}");
                }
            }

            if (!string.IsNullOrEmpty(newWeaponName.text))
            {
                Debug.Log("Forge: setting new weapon name to " + newWeaponName.text);
                meleeWeaponInstance.newWeaponName = newWeaponName.text;
            }

            if (meleeWeaponPartInstances.Count == 0)
            {
                Debug.LogWarning("Forge: no parts selected, cannot forge");
                return;
            }

            InventoryManager.Instance.AddCraftedMeleeWeaponToInventory(meleeWeaponInstance, meleeWeaponPartInstances);

            InventoryManager.Instance.RemoveItemFromInventory(baseSlot.item);
            foreach (var part in meleeWeaponPartInstances)
                InventoryManager.Instance.RemoveItemFromInventory(part);

            if (weaponPreview != null)
            {
                Destroy(weaponPreview);
                weaponPreview = null;
            }
            CleanCraftingSlots();

            Debug.Log("Forge: success (Melee)");
            return;
        }

        // =========================
        // Shoulder Weapon  ✅修正：拉到最外層
        // =========================
        if (baseSlot.item is ShoulderWeaponInstance shoulderWeaponInstance)
        {
            if (baseSlot.assembledPart != null)
            {
                string baseShaderName;
                var baseColors = ExtractColorsFromGameObject(baseSlot.assembledPart, out baseShaderName);

                if (baseColors.Count > 0)
                {
                    shoulderWeaponInstance.colors = baseColors;
                    shoulderWeaponInstance.shaderName = baseShaderName;
                }
            }

            var shoulderWeaponPartInstances = new List<PartInstance>();
            for (int i = 1; i < craftingSlots.Count; i++)
            {
                var slot = craftingSlots[i];
                if (slot == null || slot.item == null) continue;

                if (slot.item is PartInstance pi)
                {
                    if (slot.assembledPart != null)
                    {
                        string partShaderName;
                        var partColors = ExtractColorsFromGameObject(slot.assembledPart, out partShaderName);

                        if (partColors.Count > 0)
                        {
                            pi.colors = partColors;
                            pi.shaderName = partShaderName;
                        }
                    }

                    shoulderWeaponPartInstances.Add(pi);
                }
                else
                {
                    Debug.LogWarning($"Forge: slot[{i}] item is not PartInstance, actual={slot.item.GetType().Name}");
                }
            }

            if (!string.IsNullOrEmpty(newWeaponName.text))
            {
                Debug.Log("Forge: setting new weapon name to " + newWeaponName.text);
                shoulderWeaponInstance.newWeaponName = newWeaponName.text;
            }

            if (shoulderWeaponPartInstances.Count == 0)
            {
                Debug.LogWarning("Forge: no parts selected, cannot forge");
                return;
            }

            InventoryManager.Instance.AddCraftedShoulderWeaponToInventory(shoulderWeaponInstance, shoulderWeaponPartInstances);

            InventoryManager.Instance.RemoveItemFromInventory(baseSlot.item);
            foreach (var part in shoulderWeaponPartInstances)
                InventoryManager.Instance.RemoveItemFromInventory(part);

            if (weaponPreview != null)
            {
                Destroy(weaponPreview);
                weaponPreview = null;
            }
            CleanCraftingSlots();

            Debug.Log("Forge: success (Shoulder)");
            return;
        }

        Debug.LogWarning($"Forge: unsupported base slot item type: {baseSlot.item.GetType().Name}");
    }


    public void CleanCraftingSlots()
    {
        for (int i = craftingPartsButtonParent.childCount - 1; i > 0; i--)
        {
            Destroy(craftingPartsButtonParent.GetChild(i).gameObject);
        }

        if (craftingPartsButtonParent.childCount > 0)
        {
            Image spriteImage = InventoryManager.Instance.FindChild(
                craftingPartsButtonParent.GetChild(0).gameObject, "Item Icon"
            ).GetComponent<Image>();
            spriteImage.sprite = null;
            spriteImage.color = new Color(1, 1, 1, 0);
        }

        craftingSlots.Clear();
        HideAllRemoveButtonsOnCraftingSlots();

        // 同步清空 Stat Block
        if (leftStatBlock != null) leftStatBlock.text = string.Empty;
        if (rightStatBlock != null) rightStatBlock.text = string.Empty;
    }

    // 從實際場景中的 GameObject 抽出顏色與 shader 名稱
    private List<Color> ExtractColorsFromGameObject(GameObject go, out string shaderName)
    {
        shaderName = null;
        var colors = new List<Color>();

        if (!go) return colors;

        var renderer = go.GetComponentInChildren<Renderer>();
        if (!renderer) return colors;

        var mat = renderer.material;
        if (!mat || mat.shader == null) return colors;

        shaderName = mat.shader.name;

        if (shaderName.Contains("Mix 3"))
        {
            if (mat.HasProperty("_BaseColor")) colors.Add(mat.GetColor("_BaseColor"));
            if (mat.HasProperty("_Layer1Color")) colors.Add(mat.GetColor("_Layer1Color"));
            if (mat.HasProperty("_Layer2Color")) colors.Add(mat.GetColor("_Layer2Color"));
        }
        else if (shaderName.Contains("Mix 4"))
        {
            if (mat.HasProperty("_BaseColor")) colors.Add(mat.GetColor("_BaseColor"));
            if (mat.HasProperty("_Layer1Color")) colors.Add(mat.GetColor("_Layer1Color"));
            if (mat.HasProperty("_Layer2Color")) colors.Add(mat.GetColor("_Layer2Color"));
            if (mat.HasProperty("_Layer3Color")) colors.Add(mat.GetColor("_Layer3Color"));
        }
        else if (shaderName.Contains("Mix 5"))
        {
            if (mat.HasProperty("_BaseColor")) colors.Add(mat.GetColor("_BaseColor"));
            for (int i = 1; i < 5; i++)
            {
                string prop = $"_Layer{i}Color";
                if (mat.HasProperty(prop))
                    colors.Add(mat.GetColor(prop));
            }
        }

        return colors;
    }
    private List<Color> ExtractCoatingColorsFromGameObject(GameObject go, out string shaderName)
    {
        shaderName = "EffectColorController";
        var colors = new List<Color>();
        if (!go) return colors;

        // coating 的 controller 可能在 prefab parent 或 children
        var ecc = go.GetComponentInParent<EffectColorController>()
               ?? go.GetComponentInChildren<EffectColorController>(true);

        if (ecc == null) return colors;

        ecc.CacheColorsFromGroups();              // 讀出每組粒子顏色
        if (ecc.colors != null && ecc.colors.Count > 0)
            colors = new List<Color>(ecc.colors); // snapshot

        return colors;
    }

    public void OpenInventoryPage()
    {
        int index = GetSelectedSlotIndex();

        Debug.Log($"OpenInventoryPage: selected slot index {index}");
        if (index < 0 || index >= craftingSlots.Count)
        {
            Debug.LogWarning($"OpenInventoryPage: invalid slot index {index}");
            return;
        }
        if (craftingSlots[index].equipmentType == WeaponPartType.Barrel)
        {
            OpenBarrelInventory();
        }
        else if (craftingSlots[index].equipmentType == WeaponPartType.Scope)
        {
            OpenScopeInventory();
        }
        else if (craftingSlots[index].equipmentType == WeaponPartType.Gun)
        {
            OpenRangeWeaponInventory();
        }
    }

    public void SwitchWeaponCraftingTab(int weaponType)
    {
        craftingType = weaponType;
        if (craftingType == 0)
        {
            removePart(0);
            ClearInventoryButton();
            craftingPartsToggleGroup.SetAllTogglesOff();
            craftingPartsButtonParent = rangeWeaponPartsButtonParent;
            craftingPartsToggleGroup = rangeWeaponPartsToggleGroup;
            rangeWeaponCraftingSlotPage.SetActive(true);
            meleeWeaponCraftingSlotPage.SetActive(false);
            shoulderWeaponCraftingSlotPage.SetActive(false);
        }
        else if (craftingType == 1)
        {
            removePart(0);
            ClearInventoryButton();
            craftingPartsToggleGroup.SetAllTogglesOff();
            craftingPartsButtonParent = meleeWeaponPartsButtonParent;
            craftingPartsToggleGroup = meleeWeaponPartsToggleGroup;
            rangeWeaponCraftingSlotPage.SetActive(false);
            meleeWeaponCraftingSlotPage.SetActive(true);
            shoulderWeaponCraftingSlotPage.SetActive(false);
        }
        else if (craftingType == 2)
        {
            removePart(0);
            ClearInventoryButton();
            craftingPartsToggleGroup.SetAllTogglesOff();
            craftingPartsButtonParent = shoulderWeaponPartsButtonParent;
            craftingPartsToggleGroup = shoulderWeaponPartsToggleGroup;
            rangeWeaponCraftingSlotPage.SetActive(false);
            meleeWeaponCraftingSlotPage.SetActive(false);
            shoulderWeaponCraftingSlotPage.SetActive(true);
        }
    }
    private void SetDefaultMeleeHandleActive(bool active)
    {
        if (meleeWeapon == null || weaponPreview == null) return;

        // ScriptableObject 參考的是 prefab 資產，不是 runtime clone
        // 所以要用 name 去 weaponPreview 裡找對應 child
        if (meleeWeapon.defaultHandle != null)
        {
            var t = FindChildRecursive(weaponPreview.transform, meleeWeapon.defaultHandle.name);
            if (t != null) t.gameObject.SetActive(active);
        }
        else
        {
            // 保底：如果主人沒填 defaultHandle，就嘗試用常見名字找
            var t = FindChildRecursive(weaponPreview.transform, "default handle");
            if (t != null) t.gameObject.SetActive(active);
        }
    }

    // 依照 Handle slot 有沒有裝零件，自動刷新 default handle 顯示
    private void RefreshMeleeDefaultHandleState()
    {
        if (meleeWeapon == null || weaponPreview == null) return;

        bool hasHandlePart = false;

        if (craftingSlots != null)
        {
            foreach (var slot in craftingSlots)
            {
                if (slot == null) continue;
                if (slot.equipmentType != WeaponPartType.Handle) continue;
                if (slot.item == null || slot.item.item == null) continue;

                hasHandlePart = true;
                break;
            }
        }

        // 有選 Handle 零件 -> 關掉 default handle
        // 沒選 Handle 零件 -> 打開 default handle
        SetDefaultMeleeHandleActive(!hasHandlePart);
    }
    private void SetDefaultMeleeCoatingActive(bool active)
    {
        if (meleeWeapon == null || weaponPreview == null) return;

        // ScriptableObject 參考的是 prefab 資產，不是 runtime clone
        // 所以用 name 去 weaponPreview 裡找對應 child
        if (meleeWeapon.defaultCoatingEffect != null)
        {
            var t = FindChildRecursive(weaponPreview.transform, meleeWeapon.defaultCoatingEffect.name);
            if (t != null) t.gameObject.SetActive(active);
        }
        else
        {
            // 保底：如果主人沒填 defaultCoatingEffect，就嘗試用常見名字找
            var t = FindChildRecursive(weaponPreview.transform, "default coating");
            if (t != null) t.gameObject.SetActive(active);
        }
    }

    // 依照 Coating slot 有沒有裝零件，自動刷新 default coating 顯示
    private void RefreshMeleeDefaultCoatingState()
    {
        if (meleeWeapon == null || weaponPreview == null) return;

        bool hasCoatingPart = false;

        if (craftingSlots != null)
        {
            foreach (var slot in craftingSlots)
            {
                if (slot == null) continue;
                if (slot.equipmentType != WeaponPartType.Coating) continue;
                if (slot.item == null || slot.item.item == null) continue;

                hasCoatingPart = true;
                break;
            }
        }

        // 有選 Coating -> 關掉 default coating
        // 沒選 Coating -> 打開 default coating
        SetDefaultMeleeCoatingActive(!hasCoatingPart);
    }

    private void RefreshInventoryAfterRemove(int removedIndex, WeaponPartType removedPartType)
    {
        if (removedIndex == 0)
        {
            if (craftingType == 0) OpenRangeWeaponInventory();
            else if (craftingType == 1) OpenMeleeWeaponInventory();
            else if (craftingType == 2) OpenShoulderWeaponInventory();
            return;
        }

        if (craftingType == 0)
            OpenRangeWeaponPartsInventory(ItemType.WeaponPart, removedPartType);
        else if (craftingType == 1)
            OpenMeleeWeaponPartsInventory(ItemType.WeaponPart, removedPartType);
        else if (craftingType == 2)
            OpenShoulderWeaponPartsInventory(ItemType.WeaponPart, removedPartType);
    }
}