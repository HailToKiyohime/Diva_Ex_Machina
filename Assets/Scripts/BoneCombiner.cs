using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BoneCombiner : MonoBehaviour
{
    //[SerializeField] private SkinnedMeshRenderer[] skinnedMeshRendererPrefab;
    [SerializeField] private SkinnedMeshRenderer originalSkinnedMeshRenderer;
    [SerializeField] private Transform rootBone;
    private PlayerControllers playerController;

    public InventoryManager inventoryManager;
    public EquipmentManager equipmentManager;
    public GameObject legs;

    const string SaveFile = "save.es3";
    const string KeyInventorySlots = "inventory.slots";
    const string KeyEquipmentSlots = "inventory.equipment";
    //private Inputs
    public float test;

    private void Awake()
    {
        playerController = new PlayerControllers();
    }

    private void OnEnable()
    {
        playerController.Player.Enable();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        /*
        foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshRendererPrefab)
        {
            SkinnedMeshRenderer spawnedSkinnedMeshRenderer = Instantiate(skinnedMesh, transform);
            skinnedMesh.updateWhenOffscreen = true;
            skinnedMesh.bones = originalSkinnedMeshRenderer.bones;
            skinnedMesh.rootBone = rootBone;
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.Player.Save.WasPressedThisFrame())
        {
            SaveInventory();
        }
        else if (playerController.Player.Load.WasPressedThisFrame())
        {
            LoadInventory();
            inventoryManager.RebuildBuckets();
            inventoryManager.printSlots();
            //test = test2;
        }
    }

    public void SaveInventory()
    {
        ES3.Save(KeyInventorySlots, InventoryManager.Instance.slots, SaveFile);
        ES3.Save(KeyEquipmentSlots, equipmentManager.equipmentSlots, SaveFile);
    }

    // 讀檔
    public void LoadInventory()
    {
        if (ES3.KeyExists(KeyInventorySlots, SaveFile))
            InventoryManager.Instance.slots = ES3.Load<List<ItemInstance>>(KeyInventorySlots, SaveFile);
        else
            InventoryManager.Instance.slots = new List<ItemInstance>();

        if (ES3.KeyExists(KeyEquipmentSlots, SaveFile))
        {
            equipmentManager.CleanAllEquipItem();
            equipmentManager.equipmentSlots = ES3.Load<List<EquipmentSlot>>(KeyEquipmentSlots, SaveFile);
        }
        else
            equipmentManager.CreateEquipmentSlot(); // 或 new List<EquipmentSlot>()


        InventoryManager.Instance.ClearInventoryButton();
        InventoryManager.Instance.HideAllRemoveButtons();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); // 可選：清掉選取
        // 讀完重建 bucket 與見樣
        InventoryManager.Instance.RebuildBuckets();

        // 依資料重建已穿裝備的外觀
        for (int i = 0; i < equipmentManager.equipmentSlots.Count; i++)
        {
            var slot = equipmentManager.equipmentSlots[i];
            if (slot?.item is ArmorInstance ai && ai.item is Armor armor && armor.skinnedMeshRenderer)
            {
                slot.equipedItem = equipmentManager
                    .boneCombiner
                    .InstantiateEquipmentRenderer(armor.skinnedMeshRenderer, ai.colors);

                if (slot.item.item.type == ItemType.LegsArmor)
                    equipmentManager.boneCombiner.HideLegs();
            }
        }
    }

    public GameObject InstantiateEquipmentRenderer(SkinnedMeshRenderer skinnedMeshRenderer, List<Color> color)
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("no Skinned Mesh Renderer was found");
            return null;
        }
        // 產生實例
        var spawned = Instantiate(skinnedMeshRenderer, transform);
        spawned.updateWhenOffscreen = true;
        spawned.bones = originalSkinnedMeshRenderer.bones;
        spawned.rootBone = rootBone;

        // ✅ 這裡一定要 new 材質並設給 spawned
        Material mat = new Material(skinnedMeshRenderer.sharedMaterials[0]);

        if (mat.shader.name.Contains("Mix 3"))
        {
            mat.SetColor("_BaseColor", color[0]);
            mat.SetColor("_Layer1Color", color[1]);
            mat.SetColor("_Layer2Color", color[2]);
        }
        else if (mat.shader.name.Contains("Mix 4"))
        {
            mat.SetColor("_BaseColor", color[0]);
            mat.SetColor("_Layer1Color", color[1]);
            mat.SetColor("_Layer2Color", color[2]);
            mat.SetColor("_Layer3Color", color[3]);
        }
        else if (mat.shader.name.Contains("Mix 5"))
        {
            mat.SetColor("_BaseColor", color[0]);
            mat.SetColor("_Layer1Color", color[1]);
            mat.SetColor("_Layer2Color", color[2]);
            mat.SetColor("_Layer3Color", color[3]);
            mat.SetColor("_Layer4Color", color[4]);
        }

        // ✅ 指定給 spawned 而不是原 prefab
        spawned.material = mat;

        return spawned.gameObject;
    }

    public void HideLegs()
    {
        legs.SetActive(false);
    }
    public void ShowLegs()
    {
        legs.SetActive(true);
    }
}
