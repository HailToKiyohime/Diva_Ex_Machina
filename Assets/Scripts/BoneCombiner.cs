using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoneCombiner : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRendererPrefab;
    [SerializeField] private SkinnedMeshRenderer originalSkinnedMeshRenderer;
    [SerializeField] private Transform rootBone;
    private PlayerControllers playerController;

    public InventoryManager inventoryManager;

    const string SaveFile = "save.es3";
    const string KeyInventory = "inventory";
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
            inventoryManager.Add(inventoryManager.test1);
            inventoryManager.Add(inventoryManager.test2);
            inventoryManager.Add(inventoryManager.test3);
            inventoryManager.Add(inventoryManager.test4);
            inventoryManager.Add(inventoryManager.test5);
            inventoryManager.Add(inventoryManager.test6);
            inventoryManager.Add(inventoryManager.test7);
            SaveInventory();
        }
        else if (playerController.Player.Load.WasPressedThisFrame())
        {
            LoadInventory();

            inventoryManager.RebuildBuckets();

            var temp = inventoryManager.EquipTest();

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in temp)
            {
                InstantiateEquipmentRenderer(skinnedMeshRenderer);
            }


            inventoryManager.printSlots();
            //test = test2;
        }
    }

    public void SaveInventory()
    {
        // �����s��ӲM��F�t�l������T�P ScriptableObject �ޥ�
        ES3.Save(KeyInventory, inventoryManager.slots, SaveFile);
        // �]�i�GES3.Save<List<ItemInstance>>(KeyInventory, slots, SaveFile);
    }

    // Ū��
    public void LoadInventory()
    {
        if (ES3.KeyExists(KeyInventory, SaveFile))
            inventoryManager.slots = ES3.Load<List<ItemInstance>>(KeyInventory, SaveFile);
        else
            inventoryManager.slots = new List<ItemInstance>();
    }

    public void InstantiateEquipmentRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("no Skinned Mesh Renderer was found");
            return;
        }
        // ���͹��
        var spawned = Instantiate(skinnedMeshRenderer, transform);

        // �@�߹�uspawned�v�]�m
        spawned.updateWhenOffscreen = true;
        spawned.bones = originalSkinnedMeshRenderer.bones;
        spawned.rootBone = rootBone;
    }
}
