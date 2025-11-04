using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[System.Serializable]
public class ItemInstance
{
    public ItemObject item;
    public int amount;
}
[System.Serializable]
public class RangeWeaponInstance : ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
    public ItemObject[] attachment;
    public Transform muzzlePoint;
}
[System.Serializable]
public class ArmorInstance : ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();

    // 用來存不同 shader 的顏色
    [SerializeField]
    public List<Color> colors = new List<Color>();

    // 可選：記錄此裝備用的 shader 名稱，方便還原
    [SerializeField]
    public string shaderName;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    [Header("Inventory Slot")]
    [SerializeReference] public List<ItemInstance> inventory = new();

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;
    public Transform itemsButtonParent;// 放按鈕的容器
    public Transform inventoryButtonParent;// 放按鈕的容器
    [Header("Color Block/ Inventory Block")]
    public GameObject inventoryBlock;
    public GameObject colorBlock;
    public GameObject statBlock;
    [Header("Button Toggle Group")]
    public ToggleGroup inventoryToggleGroup;

    private GameObject currentPage;
    [SerializeField]
    private UIPageSwitch pageSwitch;
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
    private void Start()
    {
        //EquipmentManager.Instance.CreateEquipmentSlot();
    }
    public void AddItemToInventory(ItemObject item)
    {
        if (item is Armor a)
        {
            var inst = new ArmorInstance { item = item, amount = 1 };

            var smr = a.skinnedMeshRenderer;
            if (smr && smr.sharedMaterial)
            {
                var mat = smr.sharedMaterial;
                var shader = mat.shader;
                inst.shaderName = shader.name;

                // 根據 shader 名稱決定要存幾個顏色
                if (shader.name.Contains("Mix 3"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                }
                else if (shader.name.Contains("Mix 4"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    inst.colors.Add(mat.GetColor("_Layer1Color"));
                    inst.colors.Add(mat.GetColor("_Layer2Color"));
                    inst.colors.Add(mat.GetColor("_Layer3Color"));
                }
                else if (shader.name.Contains("Mix 5"))
                {
                    inst.colors.Add(mat.GetColor("_BaseColor"));
                    for (int i = 1; i < 5; i++)
                        inst.colors.Add(mat.GetColor($"_Layer{i}Color"));
                }
            }

            foreach (EquipmentBuff buff in a.buffs)
            {
                inst.buffs.Add(buff);
            }

            var pickedBuff = a.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            inventory.Add(inst);
        }
        else
        {
            var inst = new ItemInstance
            {
                item = item,
                amount = 1,
            };
            inventory.Add(inst);
        }
    }

    public void OpenWeaponInventory() => OpenPartsInventory(ItemType.Weapon);
    public void OpenHeadArmorInventory() => OpenPartsInventory(ItemType.HeadArmor);
    public void OpenChestArmorInventory() => OpenPartsInventory(ItemType.ChestArmor);
    public void OpenLeftHandInventory() => OpenPartsInventory(ItemType.LeftHandArmor);
    public void OpenRightHandInventory() => OpenPartsInventory(ItemType.RightHandArmor);
    public void OpenWaistArmorInventory() => OpenPartsInventory(ItemType.WaistArmor);
    public void OpenLegsArmorInventory() => OpenPartsInventory(ItemType.LegsArmor);
    public void OpenThrusterInventory() => OpenPartsInventory(ItemType.Thruster);
    public void OpenConsumableInventory() => OpenPartsInventory(ItemType.Consumable);
    public void OpenMaterialInventory() => OpenPartsInventory(ItemType.Material);

    public void OpenPartsInventory(ItemType itemType)
    {
        Debug.Log("colorBlock.activeSelf: " + colorBlock.activeSelf);
        if (colorBlock.activeSelf != true)
        {
            Debug.Log("inventoryToggleGroup.AnyTogglesOn(): "+ inventoryToggleGroup.AnyTogglesOn());
            if (inventoryToggleGroup.AnyTogglesOn())
            {

                // 先清空右邊的物品列表
                ClearInventoryButton();

                // 先記住目前選到哪個裝備槽
                int slotIndex = GetSelectedSlotIndex();

                // 把所有裝備槽上的「Remove Equipment Button」先關掉
                for (int i = 0; i < inventoryButtonParent.childCount; i++)
                {
                    var removeBtn = FindChild(inventoryButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
                    if (removeBtn != null)
                        removeBtn.SetActive(false);
                }

                // 檢查這個槽現在是不是有裝東西
                bool slotHasEquipment = false;
                if (slotIndex >= 0 &&
                    EquipmentManager.Instance != null &&
                    slotIndex < EquipmentManager.Instance.equipmentSlots.Count)
                {
                    slotHasEquipment = EquipmentManager.Instance.equipmentSlots[slotIndex].equipedItem != null;
                }

                // 開始生成符合這個類型的物品按鈕
                foreach (var inv in inventory)
                {
                    // 沒物品或型別不對就跳過
                    if (inv == null || inv.item == null || inv.item.type != itemType)
                        continue;

                    // 生成按鈕
                    var button = Instantiate(buttonPrefab, itemsButtonParent);

                    // 圖示
                    var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
                    if (icon != null)
                        icon.sprite = inv.item.icon;

                    // 名稱
                    var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                    if (label != null)
                        label.text = inv.item.itemName;

                    // Toggle
                    var btn = button.GetComponent<Toggle>();
                    if (btn != null)
                    {
                        // 如果這個槽本來就有裝備，就把那個槽的 Remove Button 打開
                        if (slotHasEquipment && slotIndex >= 0)
                        {
                            var removeBtn = FindChild(inventoryButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
                            if (removeBtn != null)
                                removeBtn.SetActive(true);
                        }

                        // 為了避免閉包問題，做一個本地變數
                        ItemInstance capturedItem = inv;
                        btn.onValueChanged.AddListener(isOn =>
                        {
                            if (!isOn) return;
                            OnClickInventoryItem(capturedItem, btn);
                        });

                        // 如果這個物品已經裝在任一個槽上，就把它鎖住
                        foreach (var slot in EquipmentManager.Instance.equipmentSlots)
                        {
                            if (slot != null && capturedItem == slot.item)
                            {
                                btn.interactable = false;
                                break;
                            }
                        }
                    }
                }
                statBlock.SetActive(false);
                inventoryBlock.SetActive(true);
                currentPage = inventoryBlock;
            }
            else
            {
                statBlock.SetActive(true);
                inventoryBlock.SetActive(false);
                // 把所有裝備槽上的「Remove Equipment Button」先關掉
                for (int i = 0; i < inventoryButtonParent.childCount; i++)
                {
                    var removeBtn = FindChild(inventoryButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
                    if (removeBtn != null)
                        removeBtn.SetActive(false);
                }

                currentPage = statBlock;
            }
            pageSwitch.pages[0] = currentPage;
        }
        else
        {
            if (!inventoryToggleGroup.AnyTogglesOn())
            {
                ColorPicker.Instance.targetGameObject = null;
                ColorPicker.Instance.targetMaterials = new List<Material>();
                ColorPicker.Instance.currentMaterialIndex = -1;
                ColorPicker.Instance.currentTextureIndex = -1;
                ColorPicker.Instance.ClearnButton();
            }
        }
    }
    public void ClearInventoryButton()
    {
        for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(itemsButtonParent.GetChild(i).gameObject);
    }
    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        Debug.Log("Working");
        if (EquipmentManager.Instance.TryEquipFromInventory(item))
        {
            // 解鎖同一清單中的其他按鈕
            foreach (var t in itemsButtonParent.GetComponentsInChildren<Toggle>(true))
            {
                if (t != btn)
                {
                    t.interactable = true;
                    t.isOn = false;
                }
            }

            Image spriteImage = FindChild(inventoryButtonParent.GetChild(GetSelectedSlotIndex()).gameObject, "Item Icon").GetComponent<Image>();
            spriteImage.sprite = item.item.icon;
            spriteImage.color = new Color(1,1,1,1);
            FindChild(inventoryButtonParent.GetChild(GetSelectedSlotIndex()).gameObject, "Remove Equipment Button").SetActive(true);
            // 鎖住當前已裝備的按鈕
            btn.interactable = false;
        }
        else
        {
            // 失敗就還原選取
            btn.isOn = false;
        }
    }

    public void OpenInventoryPage()
    {
        int index = GetSelectedSlotIndex();
        var slots = EquipmentManager.Instance.equipmentSlots;
        if (index < 0 || index >= slots.Count) // 先驗證索引範圍
        {
            Debug.LogWarning($"OpenColorPage: invalid slot index {index}");
            return;
        }
        //if (!inventoryBlock.activeSelf||!statBlock.activeSelf) return;
        OpenPartsInventory(slots[index].equipmentType);

    }

    public void Unequip()
    {
        int index = GetSelectedSlotIndex();
        EquipmentManager.Instance.CleanEquipmentSlot(index);
        FindChild(inventoryButtonParent.GetChild(index).gameObject, "Remove Equipment Button").SetActive(false);
        Image spriteImage = FindChild(inventoryButtonParent.GetChild(GetSelectedSlotIndex()).gameObject, "Item Icon").GetComponent<Image>();
        spriteImage.sprite = null;
        spriteImage.color = new Color(1, 1, 1, 0);
        OpenInventoryPage();
        if (colorBlock.activeSelf)
        {
            SelectPartToColor(index);
        }
    }


    //Color

    public void OpenColorPage()
    {
        int index = GetSelectedSlotIndex();
        // 把所有裝備槽上的「Remove Equipment Button」先關掉
        for (int i = 0; i < inventoryButtonParent.childCount; i++)
        {
            var removeBtn = FindChild(inventoryButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
            if (removeBtn != null)
                removeBtn.SetActive(false);
        }
        var slots = EquipmentManager.Instance.equipmentSlots;
        if (index < 0 || index >= slots.Count) // 先驗證索引範圍
        {
            return;
        }
        if (!colorBlock.activeSelf) return;

        var go = slots[index].equipedItem;
        if (!go)
        {
            ColorPicker.Instance.targetGameObject = null;
            ColorPicker.Instance.targetMaterials = new List<Material>();
            ColorPicker.Instance.currentMaterialIndex = -1;
            ColorPicker.Instance.currentTextureIndex = -1;
            ColorPicker.Instance.ClearnButton();
        }
        else
        {
            ColorPicker.Instance.targetGameObject = go;
            ColorPicker.Instance.AddTargetMaterialsToList();
            ColorPicker.Instance.CreateButtons();
        }
    }
    public void SelectPartToColor(int equipmentSlotsIndex)
    {
        if (EquipmentManager.Instance.equipmentSlots[equipmentSlotsIndex].equipedItem != null && colorBlock.activeSelf == true)
        {
            ColorPicker.Instance.targetGameObject = EquipmentManager.Instance.equipmentSlots[equipmentSlotsIndex].equipedItem;
            ColorPicker.Instance.AddTargetMaterialsToList();
            ColorPicker.Instance.CreateButtons();
        }
        else if(EquipmentManager.Instance.equipmentSlots[equipmentSlotsIndex].equipedItem == null && colorBlock.activeSelf == true)
        {
            ColorPicker.Instance.targetGameObject = null;
            ColorPicker.Instance.targetMaterials = new List<Material>();
            ColorPicker.Instance.currentMaterialIndex = -1;
            ColorPicker.Instance.currentTextureIndex = -1;
            ColorPicker.Instance.ClearnButton();
        }
    }

    public int GetSelectedSlotIndex()
    {
        for (int i = 0; i < inventoryButtonParent.childCount; i++)
        {
            var child = inventoryButtonParent.GetChild(i);
            var t = child.GetComponentInChildren<Toggle>(true); // 允許巢狀/隱藏
            if (t != null && t.isOn)
                return i;
        }
        return -1;
    }

    public GameObject FindChild(GameObject parentObject, string targetName)
    {
        foreach (Transform childTransform in parentObject.transform)
        {
            if (childTransform.gameObject.name == targetName)
            {
                return childTransform.gameObject; // Found the grandchild
            }

            // Recursively search in the child's children (grandchild's children, etc.)
            GameObject foundGrandchild = FindChild(childTransform.gameObject, targetName);
            if (foundGrandchild != null)
            {
                return foundGrandchild;
            }
        }
        return null; // Grandchild not found in this branch
    }
}

    
