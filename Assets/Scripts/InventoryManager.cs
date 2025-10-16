using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemInstance
{
    public ItemObject item;
    public int amount;
}
[System.Serializable]
public class WeaponInstance: ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
}
[System.Serializable]
public class ArmorInstance : ItemInstance
{
    public List<EquipmentBuff> buffs = new List<EquipmentBuff>();
}

public class InventoryManager : MonoBehaviour
{
    public ItemObject test;
    [SerializeReference] public List<ItemInstance> slots = new();
    private PlayerControllers playerController;
    public void Add(ItemObject item)
    {
        if (item is Armor a)
        {
            var inst = new ArmorInstance { item = item, amount = 1 };

            foreach (EquipmentBuff buff in a.buffs)
            {
                inst.buffs.Add(buff);
            }

            var pickedBuff = a.GetRandomBuff();
            if (pickedBuff != null)
                inst.buffs.Add(pickedBuff.buff);

            slots.Add(inst);
        }
        else
        {
            var inst = new ItemInstance
            {
                item = item,
                amount = 1,
            };

            slots.Add(inst);
        }
    }

    public void printSlots()
    {
        foreach (ItemInstance item in slots)
        {
            if (item is ArmorInstance a)
            {
                string buffText = "";
                foreach (EquipmentBuff buff in a.buffs)
                {
                    buffText = buffText + "Buff: " + buff.attribute + ", Vale:" + buff.value + " |";
                }


                Debug.Log("Item:" + a.item.itemName + " ," + buffText);
            }
            else if (item is ItemInstance i)
            {
                Debug.Log("Item:" + i.item.itemName);
            }
        }
    }
    private void Awake()
    {
        playerController = new PlayerControllers();
    }
    public void Update()
    {
        if (playerController.Player.Save.WasPressedThisFrame())
        {
            Debug.Log("Add");
            Add(test);
        }
        else if (playerController.Player.Load.WasPressedThisFrame())
        {
            Debug.Log("printSlots");
            printSlots();
        }
    }
}
