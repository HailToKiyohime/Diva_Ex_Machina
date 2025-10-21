using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EquipmentSlot
{
    public GameObject equipedItem;
    public ItemInstance item;
}


public class EquipmentManager : MonoBehaviour
{
    [SerializeReference] public List<EquipmentSlot> equipmentSlots = new();
    public BoneCombiner boneCombiner;
    public Transform EquipmentPage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        CreateEquipmentSlot();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void CreateEquipmentSlot()
    {
        equipmentSlots.Clear();

        if (EquipmentPage == null) return;

        for (int i = 0; i < EquipmentPage.childCount; i++)
        {
            // �Y�ݭn�G�T�O�C�Ӥl���� ButtonID
            var child = EquipmentPage.GetChild(i).gameObject;
            var buttonID = child.GetComponent<ButtonID>() ?? child.AddComponent<ButtonID>();
            buttonID.ID = i;

            // �� �[�J��ҡA�Ӥ��O null
            equipmentSlots.Add(new EquipmentSlot());
        }
    }

    public bool TryEquipFromInventory(InventoryManager inv, int invIndex, int slotId)
    {
        // ����ˬd
        if (invIndex < 0 || invIndex >= inv.slots.Count) return false;
        if (slotId < 0 || slotId >= equipmentSlots.Count) return false;

        var inst = inv.slots[invIndex];
        if (inst == null || inst.item == null) return false;

        // �~�[�G�u�ܽd Armor�]��L���O�ۦ��X�R�^
        if (inst is ArmorInstance ai && ai.item is Armor armor)
        {
            // �����¸˳ơ]�Y���^��^�I�]
            if (equipmentSlots[slotId].item != null)
            {
                inv.AddInstance(equipmentSlots[slotId].item);  // �^�I�]
                                                               // ���U�¥~�[
                if (equipmentSlots[slotId].equipedItem)
                    Destroy(equipmentSlots[slotId].equipedItem);
            }


            // �]�m�s�˳ơ]�� ItemInstance �ಾ��˳Ƽѡ^
            equipmentSlots[slotId].item = inst;
            equipmentSlots[slotId].equipedItem =
                boneCombiner.InstantiateEquipmentRenderer(armor.skinnedMeshRenderer, ai.colors);

            if (equipmentSlots[slotId].item.item.type == ItemType.LegsArmor)
            {
                boneCombiner.HideLegs();
            }
            return true;
        }

        // �D Armor ���B�z�K�]�̻ݨD�X�R�^
        return false;
    }

    public void UnequipItem(int buttonID)
    {
        if (buttonID < 0 || buttonID >= equipmentSlots.Count) return;
        var slot = equipmentSlots[buttonID];

        if (slot.equipedItem) Destroy(slot.equipedItem);
        slot.equipedItem = null;

        if (slot.item != null)
        {
            // ��^�I�]�ç�s buckets
            InventoryManager.Instance.AddInstance(slot.item);
            InventoryManager.Instance.OpenPartsInventory(slot.item.item.type);
            if (slot.item.item.type == ItemType.LegsArmor)
            {
                boneCombiner.ShowLegs();
            }
            slot.item = null;
        }
    }
}
