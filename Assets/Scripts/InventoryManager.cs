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
public class WeaponInstance : ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();

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
        EquipmentManager.Instance.CreateEquipmentSlot();
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
        if (inventoryBlock.activeSelf == true)
        {
            ClearInventoryButton();
            foreach (var item in inventory)
            {
                if (itemType == item.item.type)
                {
                    var button = Instantiate(buttonPrefab, itemsButtonParent);
                    var icon = button.transform.Find("Item Icon").GetComponent<Image>();
                    Debug.Log(icon);
                    if (icon) icon.sprite = item.item.icon;
                    var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                    if (label) label.text = item.item.itemName;

                    var btn = button.GetComponent<Toggle>();
                    if (btn != null)
                    {
                        btn.onValueChanged.AddListener(isOn =>
                        {
                            if (!isOn) return;
                            OnClickInventoryItem(item, btn); // 把 btn 傳進去
                        });
                        foreach (var slot in EquipmentManager.Instance.equipmentSlots)
                        {

                            if (item == slot.item)
                            {
                                btn.interactable = false;
                            }
                        }
                    }

                }
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
            
            FindGrandchildRecursive(inventoryButtonParent.GetChild(GetSelectedSlotIndex()).gameObject, "Item Icon").GetComponent<Image>().sprite = item.item.icon;

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
        if (!inventoryBlock.activeSelf) return;
        OpenPartsInventory(slots[index].equipmentType);
    }

    //Color

    public void OpenColorPage()
    {
        int index = GetSelectedSlotIndex();

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

    public GameObject FindGrandchildRecursive(GameObject parentObject, string targetName)
    {
        foreach (Transform childTransform in parentObject.transform)
        {
            if (childTransform.gameObject.name == targetName)
            {
                return childTransform.gameObject; // Found the grandchild
            }

            // Recursively search in the child's children (grandchild's children, etc.)
            GameObject foundGrandchild = FindGrandchildRecursive(childTransform.gameObject, targetName);
            if (foundGrandchild != null)
            {
                return foundGrandchild;
            }
        }
        return null; // Grandchild not found in this branch
    }
}

    
