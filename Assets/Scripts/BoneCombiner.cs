using UnityEngine;
using UnityEngine.InputSystem;

public class BoneCombiner : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRendererPrefab;
    [SerializeField] private SkinnedMeshRenderer originalSkinnedMeshRenderer;
    [SerializeField] private Transform rootBone;
    private PlayerControllers playerController;

    public InventoryManager inventoryManager;
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
            ES3.Save("test", test);
        }
        else if (playerController.Player.Load.WasPressedThisFrame())
        {
            inventoryManager.printSlots();
            test = ES3.Load<float>("test");
            //test = test2;
        }
    }
}
