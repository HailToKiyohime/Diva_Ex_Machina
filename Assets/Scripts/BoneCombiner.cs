using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoneCombiner : MonoBehaviour
{
    //[SerializeField] private SkinnedMeshRenderer[] skinnedMeshRendererPrefab;
    [SerializeField] private SkinnedMeshRenderer originalSkinnedMeshRenderer;
    [SerializeField] private Transform rootBone;
    private PlayerControllers playerController;

    public InventoryManager inventoryManager;

    public GameObject legs;

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
            inventoryManager.Add(inventoryManager.test7);
            SaveInventory();
        }
        else if (playerController.Player.Load.WasPressedThisFrame())
        {
            LoadInventory();

            inventoryManager.RebuildBuckets();
            /*
            var temp = inventoryManager.EquipTest();

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in temp)
            {
                InstantiateEquipmentRenderer(skinnedMeshRenderer);
            }

            */
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
