using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI distanceText;
    public RectTransform speedInfo;
    public RectTransform distanceInfo;
    public Color lockonColor;
    public Color normalColor;
    public Slider energyBar;
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
    private void Start()
    {
        // 初始化 Slider 基本設定
        if (energyBar != null)
        {
            energyBar.minValue = 0f;
            energyBar.wholeNumbers = false;
        }

        // 開場先刷新一次
        RefreshEnergyBar();
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
    private void LateUpdate()
    {
        if (currentCameraSet != 0) return;

        RefreshEnergyBar();
    }
    public void RefreshEnergyBar()
    {
        if (energyBar == null) return;

        var stats = PlayerStats.Instance;
        if (stats == null) return;

        float max = Mathf.Max(1f, stats.maxEnergy); // 避免 maxEnergy = 0 造成不合理狀態
        energyBar.maxValue = max;
        energyBar.value = Mathf.Clamp(stats.currentEnergy, 0f, max);
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
