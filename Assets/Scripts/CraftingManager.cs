using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingSlot {
    public GameObject assembledPart;
    public WeaponPartType equipmentType;
    public ItemInstance item;
}


public class CraftingManager : MonoBehaviour
{
    public Transform craftingPartsButtonParent;// ����s���e��
    public Transform itemsButtonParent;// ����s���e��
    public ToggleGroup craftingPartsToggleGroup;
    public Transform weaponPreviewTransform;
    public GameObject weaponPreview;
    public RangeWeapon rangeWeapon;
    [SerializeField] public List<CraftingSlot> craftingSlots = new();

    [Header("UI Button Prefab")]
    public GameObject buttonPrefab;

    public void OpenReceiverInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Receiver);
    public void OpenScopeInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Scope);
    public void OpenBarrelInventory() => OpenRangeWeaponPartsInventory(ItemType.WeaponPart, WeaponPartType.Barrel);

    public void OpenRangeWeaponInventory() => OpenWeaponInventory(ItemType.RangeWeapon);
    public void OpenWeaponInventory(ItemType itemType)
    {
        if (craftingPartsToggleGroup.AnyTogglesOn())
        {
            // ���M�ťk�䪺���~�C��
            ClearInventoryButton();
            // ���O��ثe�����Ӹ˳Ƽ�
            int slotIndex = GetSelectedSlotIndex();

            // ��Ҧ��˳ƼѤW���uRemove Equipment Button�v������
            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
            {
                var removeBtn = InventoryManager.Instance.FindChild(craftingPartsButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
                if (removeBtn != null)
                    removeBtn.SetActive(false);
            }
            // �ˬd�o�ӼѲ{�b�O���O���˪F��
            bool slotHasEquipment = false;
            if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
            {
                slotHasEquipment = craftingSlots[slotIndex].item != null;
            }
            // �}�l�ͦ��ŦX�o�����������~���s
            foreach (var inv in InventoryManager.Instance.inventory)
            {
                // �S���~�Ϋ��O����N���L
                if (inv == null || inv.item == null || inv.item.type != itemType)
                    continue;

                // �ͦ����s
                var button = Instantiate(buttonPrefab, itemsButtonParent);

                // �ϥ�
                var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = inv.item.icon;

                // �W��
                var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                if (label != null)
                    label.text = inv.item.itemName;

                // Toggle
                var btn = button.GetComponent<Toggle>();
                if (btn != null)
                {
                    // �p�G�o�Ӽѥ��ӴN���˳ơA�N�⨺�ӼѪ� Remove Button ���}
                    if (slotHasEquipment && slotIndex >= 0)
                    {
                        var removeBtn = InventoryManager.Instance.FindChild(craftingPartsButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
                        if (removeBtn != null)
                            removeBtn.SetActive(true);
                    }
                    // ���F�קK���]���D�A���@�ӥ��a�ܼ�
                    ItemInstance capturedItem = inv;
                    btn.onValueChanged.AddListener(isOn =>
                    {
                        if (!isOn) return;
                        OnClickInventoryItem(capturedItem, btn);
                    });
                    // �p�G�o�Ӫ��~�w�g�˦b��@�ӼѤW�A�N�⥦���
                    foreach (var slot in craftingSlots)
                    {
                        if (slot != null && capturedItem == slot.item)
                        {
                            btn.interactable = false;
                            break;
                        }
                    }
                }
            }
        }
    }
    public void OpenRangeWeaponPartsInventory(ItemType itemType, WeaponPartType weaponPartType)
    {
        if (craftingPartsToggleGroup.AnyTogglesOn())
        {
            // ���M�ťk�䪺���~�C��
            ClearInventoryButton();
            // ���O��ثe�����Ӹ˳Ƽ�
            int slotIndex = GetSelectedSlotIndex();

            // ��Ҧ��˳ƼѤW���uRemove Equipment Button�v������
            for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
            {
                var removeBtn = InventoryManager.Instance.FindChild(craftingPartsButtonParent.GetChild(i).gameObject, "Remove Equipment Button");
                if (removeBtn != null)
                    removeBtn.SetActive(false);
            }
            // �ˬd�o�ӼѲ{�b�O���O���˪F��
            bool slotHasEquipment = false;
            if (slotIndex >= 0 && slotIndex < craftingSlots.Count)
            {
                slotHasEquipment = craftingSlots[slotIndex].item != null;
            }
            // �}�l�ͦ��ŦX�o�����������~���s
            foreach (var inv in InventoryManager.Instance.inventory)
            {
                // �S���~�Ϋ��O����N���L
                if (inv == null || inv.item == null || inv.item.type != itemType)
                    continue;

                // �p�G�O�Z���s��A�٭n�ˬd�l����
                if (itemType == ItemType.WeaponPart)
                {
                    if (inv.item is RangeWeaponPart rangeWeaponPart)
                    {
                        // ���P�����N���L
                        if (rangeWeaponPart.partType != weaponPartType)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // ���O RangeWeaponPart �]���L
                        continue;
                    }
                }
                // �ͦ����s
                var button = Instantiate(buttonPrefab, itemsButtonParent);

                // �ϥ�
                var icon = button.transform.Find("Item Icon")?.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = inv.item.icon;

                // �W��
                var label = button.transform.Find("Item Name")?.GetComponent<TMPro.TMP_Text>();
                if (label != null)
                    label.text = inv.item.itemName;

                // Toggle
                var btn = button.GetComponent<Toggle>();
                if (btn != null)
                {
                    // �p�G�o�Ӽѥ��ӴN���˳ơA�N�⨺�ӼѪ� Remove Button ���}
                    if (slotHasEquipment && slotIndex >= 0)
                    {
                        var removeBtn = InventoryManager.Instance.FindChild(craftingPartsButtonParent.GetChild(slotIndex).gameObject, "Remove Equipment Button");
                        if (removeBtn != null)
                            removeBtn.SetActive(true);
                    }
                    // ���F�קK���]���D�A���@�ӥ��a�ܼ�
                    ItemInstance capturedItem = inv;
                    btn.onValueChanged.AddListener(isOn =>
                    {
                        if (!isOn) return;
                        OnClickInventoryItem(capturedItem, btn);
                    });
                    // �p�G�o�Ӫ��~�w�g�˦b��@�ӼѤW�A�N�⥦���
                    foreach (var slot in craftingSlots)
                    {
                        if (slot != null && capturedItem == slot.item)
                        {
                            btn.interactable = false;
                            break;
                        }
                    }
                }
            }
        }
    }

    private void OnClickInventoryItem(ItemInstance item, Toggle btn)
    {
        if (item.item is RangeWeapon rw)
        {
            // ���M���ª��w������w��
            if (weaponPreview != null)
            {
                Destroy(weaponPreview);
            }
            GameObject weapon = Instantiate(rw.weaponPrefab, weaponPreviewTransform);
            weaponPreview = weapon;
            rangeWeapon = rw;
        }
        else if (item.item is RangeWeaponPart rwp)
        {
            if (rangeWeapon == null || weaponPreview == null)
                return;

            foreach (var attachmentPoint in rangeWeapon.attachmentPoints)
            {
                if (rwp.partType != attachmentPoint.allowPart || attachmentPoint.pointTransform == null)
                    continue;

                // �� ScriptableObject �̦s�� Transform �W�r�A�h������ weaponPreview �W��
                string slotName = attachmentPoint.pointTransform.name;
                Transform parentInScene = FindChildRecursive(weaponPreview.transform, slotName);

                if (parentInScene == null)
                {
                    //Debug.LogWarning($"�䤣�챾�I {slotName}�A�нT�{ weaponPrefab �����P�W�l����C");
                    continue;
                }

                Instantiate(rwp.rangeWeaponPartPrefab, parentInScene);
            }
        }
    }

    public void ClearInventoryButton()
    {
        for (int i = itemsButtonParent.childCount - 1; i >= 0; i--)
            Destroy(itemsButtonParent.GetChild(i).gameObject);
    }
    public int GetSelectedSlotIndex()
    {
        for (int i = 0; i < craftingPartsButtonParent.childCount; i++)
        {
            var child = craftingPartsButtonParent.GetChild(i);
            var t = child.GetComponentInChildren<Toggle>(true); // ���\�_��/����
            if (t != null && t.isOn)
                return i;
        }
        return -1;
    }

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
}
