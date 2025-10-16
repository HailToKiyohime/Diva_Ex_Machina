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
        foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshRendererPrefab)
        {
            SkinnedMeshRenderer spawnedSkinnedMeshRenderer = Instantiate(skinnedMesh, transform);
            skinnedMesh.updateWhenOffscreen = true;
            skinnedMesh.bones = originalSkinnedMeshRenderer.bones;
            skinnedMesh.rootBone = rootBone;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.Player.Save.WasPressedThisFrame())
        {
            inventoryManager.Add(inventoryManager.test);
            SaveInventory();
        }
        else if (playerController.Player.Load.WasPressedThisFrame())
        {
            LoadInventory();
            inventoryManager.printSlots();
            //test = test2;
        }
    }

    public void SaveInventory()
    {
        // 直接存整個清單；含子類型資訊與 ScriptableObject 引用
        ES3.Save(KeyInventory, inventoryManager.slots, SaveFile);
        // 也可：ES3.Save<List<ItemInstance>>(KeyInventory, slots, SaveFile);
    }

    // 讀檔
    public void LoadInventory()
    {
        if (ES3.KeyExists(KeyInventory, SaveFile))
            inventoryManager.slots = ES3.Load<List<ItemInstance>>(KeyInventory, SaveFile);
        else
            inventoryManager.slots = new List<ItemInstance>();
    }
}
