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

    // 右側「背包物品按鈕」們的父物件
    public Transform itemsButtonParent;
    // 左側插槽的 ToggleGroup，用來判斷目前有沒有選中插槽
    private ToggleGroup craftingPartsToggleGroup;
    public ToggleGroup rangeWeaponPartsToggleGroup;
    public ToggleGroup meleeWeaponPartsToggleGroup;
    // 武器預覽的父節點
    public Transform weaponPreviewTransform;
    // 目前場景中的武器預覽實體
    public GameObject weaponPreview;
    // 目前正在預覽 / 組裝中的武器資料 (ScriptableObject)
    public RangeWeapon rangeWeapon;
    public MeleeWeapon meleeWeapon;
    // 記錄每一個合成插槽的狀態
    [SerializeField] public List<CraftingSlot> craftingSlots = new();

    public GameObject rangeWeaponCraftingSlotPage;
    public GameObject meleeWeaponCraftingSlotPage;

    [Header("UI Button Prefab")]
    // 右側背包物品按鈕的預置物
    public GameObject buttonPrefab;
    // 左側合成插槽按鈕的預置物
    public GameObject craftingSlotPrefab;
    public Sprite barrelIcon;
    public Sprite scopeIcon;
    public Sprite handleIcon;
    public Sprite coatingIcon;

    public UIPageSwitch uiPageSwitch;

    public ColorPicker weaponPartColorPicker;
    public GameObject weaponColorBlock;

    public TMP_InputField newWeaponName;


    [Header("Crafting Stat Block")]
    public TextMeshProUGUI leftStatBlock;
    public TextMeshProUGUI rightStatBlock;

    [Tooltip("若為 true：bulletPerShot / roundPerTap 為 1 時也會顯示 x 1；若為 false：1 會被省略")]
    public bool showX1Multipliers = false;

    [Header("Tooltip")]
    [Tooltip("Tooltip 面板（跟隨滑鼠顯示）。建議放在 Crafting UI Canvas 之下，並設為預設關閉。")]
    public RectTransform tooltipPanel;
    public TextMeshProUGUI tooltipTitle;
    public TextMeshProUGUI tooltipBody;
    [Tooltip("若留空，會自動從 tooltipPanel 往上找 Canvas。若 Canvas 是 Screen Space - Camera，請填入該 Canvas。")]
    public Canvas tooltipCanvas;
    [Tooltip("Tooltip 相對滑鼠的偏移（像素）。")]
    public Vector2 tooltipOffset = new Vector2(16f, -16f);
    [Tooltip("若為 true：顯示 (current -> new)；若為 false：只顯示 +/- 差異。 ")]
    public bool tooltipShowCurrentAndNew = true;
    [Tooltip("若為 true：差異為 0 的屬性也會顯示。通常建議關閉以保持乾淨。 ")]
    public bool tooltipShowZeroDiff = false;

    private ItemInstance _tooltipItem;
    private bool _tooltipVisible;

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
    }

    private void Start()
    {
        HideTooltipImmediate();
    }

    private void Update()
    {
        if (_tooltipVisible)
            UpdateTooltipPosition(Input.mousePosition);
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
                ws.weapon = rwi;
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
            if (inv == null || inv.item == null || inv.item.type != itemType)
                continue;

            if (!(inv.item is MeleeWeaponPart mwp) || mwp.partType != weaponPartType)
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
        }else if (item.item is MeleeWeapon mw)
        {
            // 先清掉舊預覽與舊插槽（避免重複產生 part slot）
            if (weaponPreview != null)
                Destroy(weaponPreview);
            CleanCraftingSlots();   // 清左側 UI + craftingSlots
            RefreshMeleeDefaultHandleState();
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
        HideTooltipImmediate();
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


    // ===== Tooltip（Hover 顯示零件差異 / 武器本體 Buff） =====

    private void HideTooltipImmediate()
    {
        _tooltipItem = null;
        _tooltipVisible = false;
        if (tooltipPanel != null)
            tooltipPanel.gameObject.SetActive(false);
    }

    public void ShowTooltip(ItemInstance item)
    {
        if (tooltipPanel == null || tooltipTitle == null || tooltipBody == null)
            return;

        _tooltipItem = item;
        _tooltipVisible = item != null;

        tooltipPanel.gameObject.SetActive(_tooltipVisible);
        if (!_tooltipVisible)
            return;

        RefreshTooltipContent();
        UpdateTooltipPosition(Input.mousePosition);
    }

    public void HideTooltip()
    {
        HideTooltipImmediate();
    }

    private void RefreshTooltipIfVisible()
    {
        if (!_tooltipVisible || tooltipPanel == null || !tooltipPanel.gameObject.activeInHierarchy)
            return;
        RefreshTooltipContent();
    }

    private Canvas GetTooltipCanvas()
    {
        if (tooltipCanvas != null) return tooltipCanvas;
        if (tooltipPanel == null) return null;
        return tooltipPanel.GetComponentInParent<Canvas>();
    }

    private void UpdateTooltipPosition(Vector2 mouseScreenPos)
    {
        if (!_tooltipVisible || tooltipPanel == null) return;

        var canvas = GetTooltipCanvas();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Camera uiCam = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos, uiCam, out var localPos))
        {
            // 基本位置：滑鼠 + 偏移
            var target = localPos + tooltipOffset;
            tooltipPanel.anchoredPosition = target;

            // 邊界修正：避免 Tooltip 出畫面
            ClampTooltipToCanvas(canvasRect);
        }
    }

    private void ClampTooltipToCanvas(RectTransform canvasRect)
    {
        if (tooltipPanel == null) return;

        // 取得 Tooltip 在 Canvas Local 的四個角
        Vector3[] corners = new Vector3[4];
        tooltipPanel.GetWorldCorners(corners);

        // 把 world corners 轉回 canvas local
        for (int i = 0; i < 4; i++)
            corners[i] = canvasRect.InverseTransformPoint(corners[i]);

        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[2].y;

        Rect r = canvasRect.rect;
        Vector2 shift = Vector2.zero;
        if (maxX > r.xMax) shift.x -= (maxX - r.xMax);
        if (minX < r.xMin) shift.x += (r.xMin - minX);
        if (maxY > r.yMax) shift.y -= (maxY - r.yMax);
        if (minY < r.yMin) shift.y += (r.yMin - minY);

        if (shift != Vector2.zero)
            tooltipPanel.anchoredPosition += shift;
    }

    private void RefreshTooltipContent()
    {
        if (!_tooltipVisible || _tooltipItem == null || tooltipTitle == null || tooltipBody == null)
        {
            HideTooltipImmediate();
            return;
        }

        // Hover 武器：只顯示武器本體 buffs（不含零件總和、不含 craftingSlots 總和）
        if (_tooltipItem is RangeWeaponInstance rwi)
        {
            tooltipTitle.text = $"{GetDisplayName(rwi)}";
            tooltipBody.text = BuildOwnBuffText(rwi.buffs);
            return;
        }

        // Hover 零件：保留 (old -> new)，但顯示的是「零件自身修正值」而不是 delta。
        // 新值會依照更好/更差上綠/紅色。
        if (_tooltipItem is PartInstance pi)
        {
            WeaponPartType partType = GetPartTypeFromInstance(pi);
            tooltipTitle.text = $"{partType}: {GetDisplayName(pi)}";

            var current = FindCurrentPartInstance(partType);
            tooltipBody.text = BuildPartCompareText(current != null ? current.buffs : null, pi.buffs);
            return;
        }

        // 其他類型：保底顯示名稱
        tooltipTitle.text = GetDisplayName(_tooltipItem);
        tooltipBody.text = "";
    }

    private WeaponPartType GetPartTypeFromInstance(PartInstance pi)
    {
        if (pi != null && pi.item is RangeWeaponPart rwp)
            return rwp.partType;

        // 保底：用 slot.equipmentType 推測（找出目前這個 PartInstance 被裝在哪個 slot）
        if (craftingSlots != null)
        {
            foreach (var s in craftingSlots)
                if (s != null && s.item == pi)
                    return s.equipmentType;
        }
        return WeaponPartType.Gun;
    }

    private PartInstance FindCurrentPartInstance(WeaponPartType partType)
    {
        if (craftingSlots == null) return null;
        foreach (var s in craftingSlots)
        {
            if (s == null) continue;
            if (s.item is not PartInstance part) continue;
            if (s.equipmentType != partType) continue;
            if (part.item == null) continue;
            return part;
        }
        return null;
    }

    private struct BuffAgg
    {
        public float add;      // 加總後的加法值
        public float mul;      // 乘法因子（把所有 (1+value) 乘起來）
        public bool hasAdd;
        public bool hasMul;
    }

    /// <summary>
    /// 把 buffs 彙整成「加法總和」與「乘法因子」。
    /// - Add: add += value
    /// - Multiplier: mul *= (1 + value)
    /// </summary>
    private Dictionary<Attributes, BuffAgg> AggregateBuffs(List<EquipmentBuff> buffs)
    {
        var dict = new Dictionary<Attributes, BuffAgg>();
        if (buffs == null) return dict;

        foreach (var b in buffs)
        {
            if (!dict.TryGetValue(b.attribute, out var agg))
                agg = new BuffAgg { add = 0f, mul = 1f, hasAdd = false, hasMul = false };

            // FiringMode 用名稱顯示；這個屬性我們只吃 Add（或直接用 value）比較合理
            if (b.attribute == Attributes.FiringMode)
            {
                agg.add += b.value;
                agg.hasAdd = true;
                dict[b.attribute] = agg;
                continue;
            }

            if (b.mode == BuffApplyMode.Multiplier)
            {
                agg.mul *= (1f + b.value);
                agg.hasMul = true;
            }
            else // BuffApplyMode.Add（或未知就當 Add）
            {
                agg.add += b.value;
                agg.hasAdd = true;
            }

            dict[b.attribute] = agg;
        }

        return dict;
    }

    /// <summary>
    /// Hover 武器用：只顯示自身 buffs（不做 old/new 比較）。
    /// - Add: 顯示數字（不強制加 + 號，避免把「絕對值」看成 delta）
    /// - Multiplier: 顯示 x1.3（把所有 (1+value) 連乘）
    /// </summary>
    private string BuildOwnBuffText(List<EquipmentBuff> buffs)
    {
        var agg = AggregateBuffs(buffs);
        if (agg.Count <= 0) return "No buffs.";

        var keys = new List<Attributes>(agg.Keys);
        keys.Sort((a, b) => ((int)a).CompareTo((int)b));

        var sb = new StringBuilder();
        int shown = 0;

        foreach (var attr in keys)
        {
            var v = agg[attr];

            if (attr == Attributes.FiringMode)
            {
                int mode = Mathf.RoundToInt(v.add);
                sb.AppendLine($"Firing Mode: {GetFiringModeName(mode)}");
                shown++;
                continue;
            }

            var parts = new List<string>(2);
            if (v.hasAdd)
                parts.Add(FormatAddValue(attr, v.add));
            if (v.hasMul)
                parts.Add(FormatMulValue(v.mul));

            if (parts.Count == 0) continue;

            sb.AppendLine($"{PrettyAttrName(attr)}: {string.Join(" ", parts)}");
            shown++;
        }

        return shown > 0 ? sb.ToString().TrimEnd() : "No buffs.";
    }

    /// <summary>
    /// Hover 零件用：保留 (old -> new)，但顯示的是「零件自身修正值」；新值依照更好/更差上色。
    /// 例：Spread: &lt;color=green&gt;x1.3&lt;/color&gt; (0 -&gt; x1.3)
    /// </summary>
    private string BuildPartCompareText(List<EquipmentBuff> currentBuffs, List<EquipmentBuff> hoverBuffs)
    {
        var oldAgg = AggregateBuffs(currentBuffs);
        var newAgg = AggregateBuffs(hoverBuffs);

        // union of attributes
        var keySet = new HashSet<Attributes>();
        foreach (var k in oldAgg.Keys) keySet.Add(k);
        foreach (var k in newAgg.Keys) keySet.Add(k);

        if (keySet.Count == 0) return "No buffs.";

        var keys = new List<Attributes>(keySet);
        keys.Sort((a, b) => ((int)a).CompareTo((int)b));

        var sb = new StringBuilder();
        int shown = 0;

        foreach (var attr in keys)
        {
            oldAgg.TryGetValue(attr, out var o);
            newAgg.TryGetValue(attr, out var n);

            // FiringMode：用名稱顯示，不做好壞判斷（避免誤導）
            if (attr == Attributes.FiringMode)
            {
                string oldName = (o.hasAdd) ? GetFiringModeName(Mathf.RoundToInt(o.add)) : "0";
                string newName = (n.hasAdd) ? GetFiringModeName(Mathf.RoundToInt(n.add)) : "0";
                sb.AppendLine($"Firing Mode: {newName} ({oldName} -> {newName})");
                shown++;
                continue;
            }

            bool hasAdd = o.hasAdd || n.hasAdd;
            bool hasMul = o.hasMul || n.hasMul;
            bool both = hasAdd && hasMul;

            if (hasAdd)
            {
                float oldVal = o.hasAdd ? o.add : 0f;
                float newVal = n.hasAdd ? n.add : 0f;

                string oldDisp = FormatAddOrZero(attr, oldVal);
                string newDisp = FormatAddOrZero(attr, newVal);

                string label = both ? $"{PrettyAttrName(attr)} (Add)" : PrettyAttrName(attr);
                string coloredNew = ColorizeNewValue(attr, oldVal, newVal, newDisp, isMultiplier: false);

                if (tooltipShowZeroDiff || !Approximately(oldVal, newVal))
                {
                    sb.AppendLine($"{label}: {coloredNew} ({oldDisp} -> {newDisp})");
                    shown++;
                }
            }

            if (hasMul)
            {
                float oldFactor = o.hasMul ? o.mul : 1f;
                float newFactor = n.hasMul ? n.mul : 1f;

                string oldDisp = FormatMulOrZero(attr, oldFactor);
                string newDisp = FormatMulOrZero(attr, newFactor);

                string label = both ? $"{PrettyAttrName(attr)} (Mul)" : PrettyAttrName(attr);
                string coloredNew = ColorizeNewValue(attr, oldFactor, newFactor, newDisp, isMultiplier: true);

                if (tooltipShowZeroDiff || !Approximately(oldFactor, newFactor))
                {
                    sb.AppendLine($"{label}: {coloredNew} ({oldDisp} -> {newDisp})");
                    shown++;
                }
            }
        }

        return shown > 0 ? sb.ToString().TrimEnd() : "No buffs.";
    }

    private static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.0001f;
    }

    /// <summary>
    /// 哪些屬性是「越小越好」。用於 Tooltip 上色。
    /// </summary>
    private static bool IsSmallerBetter(Attributes attr)
    {
        return attr switch
        {
            Attributes.Spread => true,
            Attributes.ReloadTime => true,
            Attributes.TimeBetweenShooting => true,
            Attributes.TimeBetweenShots => true,
            Attributes.DashEnergyCost => true,
            Attributes.FlyEnergyCost => true,
            _ => false
        };
    }

    private string ColorizeNewValue(Attributes attr, float oldVal, float newVal, string newDisp, bool isMultiplier)
    {
        // Decide better/worse by attribute direction.
        // For multiplier, we compare factors (1.0 baseline). For add, we compare additive values.
        bool smallerBetter = IsSmallerBetter(attr);

        bool better;
        if (smallerBetter)
            better = newVal < oldVal - 0.0001f;
        else
            better = newVal > oldVal + 0.0001f;

        bool worse;
        if (smallerBetter)
            worse = newVal > oldVal + 0.0001f;
        else
            worse = newVal < oldVal - 0.0001f;

        if (better)
            return $"<color=green>{newDisp}</color>";
        if (worse)
            return $"<color=red>{newDisp}</color>";

        return newDisp;
    }

    private string FormatAddOrZero(Attributes attr, float v)
    {
        // For count-like stats, a "0" modifier means baseline is effectively x1.
        // Showing 0 here is confusing (e.g. RoundPerPull: (0 -> x6) implies "shoot 0 rounds").
        if (Mathf.Abs(v) < 0.0001f)
        {
            if (attr == Attributes.RoundPerPull || attr == Attributes.BulletPerShot)
                return "x1";
            return "0";
        }

        return FormatAddValue(attr, v);
    }

    private static string FormatMulOrZero(Attributes attr, float factor)
    {
        // Same baseline rule as above: for count-like stats, baseline should display as x1.
        if (Mathf.Abs(factor - 1f) < 0.0001f)
        {
            if (attr == Attributes.RoundPerPull || attr == Attributes.BulletPerShot)
                return "x1";
            return "0";
        }

        return $"x{FormatNumberStatic(factor)}";
    }

    private static string FormatMulValue(float factor)
    {
        return $"x{FormatNumberStatic(factor)}";
    }

    private string FormatAddValue(Attributes attr, float v)
    {
        // Add values shown as raw number with units where helpful.
        switch (attr)
        {
            case Attributes.CriticalChance:
                return FormatPercent01(v);
            case Attributes.CriticalMultiplier:
                return FormatNumber(v);
            case Attributes.ReloadTime:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
                return FormatSeconds(v);
            case Attributes.Spread:
                return $"{v:0.##}°";
            case Attributes.MagazineSize:
                return Mathf.RoundToInt(v).ToString();
            case Attributes.BulletPerShot:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            case Attributes.RoundPerPull:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            case Attributes.FiringMode:
                return Mathf.RoundToInt(v).ToString();
            default:
                return FormatNumber(v);
        }
    }

    private static string FormatNumberStatic(float v)
    {
        float r = Mathf.Round(v);
        if (Mathf.Abs(v - r) < 0.0001f)
            return ((int)r).ToString();
        return v.ToString("0.##");
    }

    private string PrettyAttrName(Attributes attr)
    {
        // 把 enum 名稱轉成較好讀的形式：PhysicalDamage -> Physical Damage
        string s = attr.ToString();
        return System.Text.RegularExpressions.Regex.Replace(s, "([a-z])([A-Z])", "$1 $2");
    }

    private string FormatAttrValue(Attributes attr, float v)
    {
        switch (attr)
        {
            case Attributes.CriticalChance:
                return FormatPercent01(v);
            case Attributes.CriticalMultiplier:
                return "x" + FormatNumber(v);
            case Attributes.ReloadTime:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
                return FormatSeconds(v);
            case Attributes.Spread:
                return $"{v:0.##}°";
            case Attributes.MagazineSize:
                return Mathf.RoundToInt(v).ToString();
            case Attributes.BulletPerShot:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            case Attributes.RoundPerPull:
                return $"x{Mathf.Max(1, Mathf.RoundToInt(v))}";
            default:
                return FormatNumber(v);
        }
    }

    private string FormatDelta(Attributes attr, float delta)
    {
        // delta 一律帶 +/-
        string sign = delta >= 0f ? "+" : "";
        switch (attr)
        {
            case Attributes.CriticalChance:
                return sign + FormatPercent01(delta);
            case Attributes.CriticalMultiplier:
                return sign + FormatNumber(delta);
            case Attributes.ReloadTime:
            case Attributes.TimeBetweenShooting:
            case Attributes.TimeBetweenShots:
                return sign + FormatSeconds(delta);
            case Attributes.Spread:
                return sign + $"{delta:0.##}°";
            case Attributes.MagazineSize:
            case Attributes.BulletPerShot:
            case Attributes.RoundPerPull:
                return sign + Mathf.RoundToInt(delta).ToString();
            default:
                return sign + FormatNumber(delta);
        }
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

            OpenRangeWeaponInventory();

            RefreshCraftingStatBlock();
            RefreshTooltipIfVisible();
            RefreshMeleeDefaultHandleState();
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
                else if(slot.equipmentType == WeaponPartType.Coating)
                    icon.sprite = coatingIcon;
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
                    }else if (craftingType == 1)
                    {
                        OpenMeleeWeaponPartsInventory(ItemType.WeaponPart, capturedSlot.equipmentType);
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
            weaponPartColorPicker.targetGameObject = slot.assembledPart;
            weaponPartColorPicker.AddTargetMaterialsToList();
            weaponPartColorPicker.CreateButtons();
        }
        else
        {
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

        if (!(baseSlot.item is RangeWeaponInstance weaponInstance))
        {
            Debug.LogWarning($"Forge: base slot item is not RangeWeaponInstance, actual={baseSlot.item.GetType().Name}");
            return;
        }

        // 先把「預覽武器本體」上改好的顏色，回寫到 RangeWeaponInstance
        if (baseSlot.assembledPart != null)
        {
            string baseShaderName;
            var baseColors = ExtractColorsFromGameObject(baseSlot.assembledPart, out baseShaderName);

            if (baseColors.Count > 0)
            {
                weaponInstance.colors = baseColors;
                weaponInstance.shaderName = baseShaderName;
            }
        }

        // 收集零件（同時同步每個零件的顏色）
        var rangeWeaponPartInstances = new List<PartInstance>();

        if (craftingSlots.Count > 1)
        {
            for (int i = 1; i < craftingSlots.Count; i++)
            {
                var slot = craftingSlots[i];
                if (slot == null || slot.item == null)
                    continue;

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
        }

        if (!string.IsNullOrEmpty(newWeaponName.text))
        {
            Debug.Log("Forge: setting new weapon name to " + newWeaponName.text);
            weaponInstance.newWeaponName = newWeaponName.text;
        }

        if (rangeWeaponPartInstances.Count == 0)
        {
            Debug.LogWarning("Forge: no parts selected, cannot forge");
            return;
        }

        // 1) 生成鍛造後的新武器，加入背包
        InventoryManager.Instance.AddCraftedRangeWeaponToInventory(weaponInstance, rangeWeaponPartInstances);

        // 2) 消耗掉原本的 blueprint 武器 + 零件
        InventoryManager.Instance.RemoveItemFromInventory(baseSlot.item);
        foreach (var part in rangeWeaponPartInstances)
        {
            InventoryManager.Instance.RemoveItemFromInventory(part);
        }

        // 清畫面
        if (weaponPreview != null)
        {
            Destroy(weaponPreview);
            weaponPreview = null;
        }
        CleanCraftingSlots();

        Debug.Log("Forge: success");
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
            craftingPartsButtonParent = rangeWeaponPartsButtonParent;
            craftingPartsToggleGroup = rangeWeaponPartsToggleGroup;
            rangeWeaponCraftingSlotPage.SetActive(true);
            meleeWeaponCraftingSlotPage.SetActive(false);
        }
        else if (craftingType == 1)
        {
            craftingPartsButtonParent = meleeWeaponPartsButtonParent;
            craftingPartsToggleGroup = meleeWeaponPartsToggleGroup;
            rangeWeaponCraftingSlotPage.SetActive(false);
            meleeWeaponCraftingSlotPage.SetActive(true);
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
}