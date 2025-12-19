using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private GameObject CombatCameraSet;
    [SerializeField] private GameObject CharacterEquipmentCameraSet;
    [SerializeField] private GameObject CharacterCraftingCameraSet;

    [SerializeField] private GameObject CombatUICanvas;
    [SerializeField] private GameObject CharacterEquipmentCanvas;
    [SerializeField] private GameObject CharacterCraftingCanvas;
    public int currentCameraSet = 0; // 0: Combat, 1: Equipment, 2: Crafting
    [Header("Combat UI")]
    [SerializeField] public TextMeshProUGUI speedText;
    [SerializeField] public TextMeshProUGUI distanceText;
    [SerializeField] public RectTransform speedInfo;
    [SerializeField] public RectTransform distanceInfo;
    [SerializeField] public Color lockonColor;
    [SerializeField] public Color normalColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentCameraSet)
        {
            case 0:
                CombatCameraSet.SetActive(true);
                CharacterEquipmentCameraSet.SetActive(false);
                CharacterCraftingCameraSet.SetActive(false);
                CombatUICanvas.SetActive(true);
                CharacterEquipmentCanvas.SetActive(false);
                CharacterCraftingCanvas.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case 1:
                CombatCameraSet.SetActive(false);
                CharacterEquipmentCameraSet.SetActive(true);
                CharacterCraftingCameraSet.SetActive(false);
                CombatUICanvas.SetActive(false);
                CharacterEquipmentCanvas.SetActive(true);
                CharacterCraftingCanvas.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case 2:
                CombatCameraSet.SetActive(false);
                CharacterEquipmentCameraSet.SetActive(false);
                CharacterCraftingCameraSet.SetActive(true);
                CombatUICanvas.SetActive(false);
                CharacterEquipmentCanvas.SetActive(true);
                CharacterCraftingCanvas.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    public void SwitchToCombatUI()
    {
        currentCameraSet = 0;
    }
    public void SwitchToCharacterEquipmentUI()
    {
        currentCameraSet = 1;
    }
    public void SwitchToCharacterCraftingUI()
    {
        currentCameraSet = 2;
    }

}
