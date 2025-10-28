using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BoneCombiner : MonoBehaviour
{
    public static BoneCombiner Instance { get; private set; }
    //[SerializeField] private SkinnedMeshRenderer[] skinnedMeshRendererPrefab;
    [SerializeField] private SkinnedMeshRenderer originalSkinnedMeshRenderer;
    [SerializeField] private Transform rootBone;
    private PlayerControllers playerController;

    public GameObject legs;

    //private Inputs
    public float test;

    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy this new instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // Exit to prevent further execution of this Awake method
        }
        // Otherwise, set this instance as the Singleton
        Instance = this;

        playerController = new PlayerControllers();
    }

    private void OnEnable()
    {
        playerController.Player.Enable();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public GameObject InstantiateMesh(SkinnedMeshRenderer prefab)
    {
        if (!prefab || !originalSkinnedMeshRenderer || !rootBone) return null;

        var inst = Instantiate(prefab, transform);
        inst.updateWhenOffscreen = true;                 // 改實例，不改prefab
        inst.bones = originalSkinnedMeshRenderer.bones;  // 綁到玩家骨架
        inst.rootBone = rootBone;
        return inst.gameObject;
    }
}
