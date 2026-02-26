using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private GameObject CombatCameraSet;
    [SerializeField] private GameObject BuildingCameraSet;
    [SerializeField] private GameObject CharacterEquipmentCameraSet;
    [SerializeField] private GameObject CharacterCraftingCameraSet;

    [SerializeField] private GameObject CombatUICanvas;
    [SerializeField] private GameObject CharacterEquipmentCanvas;
    [SerializeField] private GameObject CharacterCraftingCanvas;
    [SerializeField] private GameObject BuildingUICanvas;

    [SerializeField] private BuildSystem buildSystem; // Reference to the BuildSystem script
    public int currentCameraSet = 0; // 0: Combat, 1: Equipment, 2: Crafting
    [Header("Combat UI")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI distanceText;
    public RectTransform speedInfo;
    public RectTransform distanceInfo;
    public Color lockonColor;
    public Color normalColor;
    public Slider energyBar;

    [Header("Ammo Bar")]
    public Image leftHandAmmoBar;
    public Image rightHandAmmoBar;
    public Image leftShoulderAmmoBar;
    public Image rightShoulderAmmoBar;
    public Image leftHandAmmoFrame;
    public Image rightHandAmmoFrame;
    public Image leftShoulderAmmoFrame;
    public Image rightShoulderAmmoFrame;
    public Color ammoNormalColor ;
    public Color ammoReloadColor ;
    public float ammoReloadFlashSpeed = 6f;
    // hand reload state
    private bool _leftReloading;
    private bool _rightReloading;
    // shoulder reload state
    private bool _leftShoulderReloading;
    private bool _rightShoulderReloading;
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
        if (energyBar != null)
        {
            energyBar.minValue = 0f;
            energyBar.wholeNumbers = false;
        }

        RefreshEnergyBar();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            currentCameraSet = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            currentCameraSet = 1;
        }else if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            currentCameraSet = 2;
        }else if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            currentCameraSet = 3;
        }
        switch (currentCameraSet)
            {
                case 0:
                    CombatCameraSet.SetActive(true);
                    BuildingCameraSet.SetActive(false);
                    CharacterEquipmentCameraSet.SetActive(false);
                    CharacterCraftingCameraSet.SetActive(false);
                    CombatUICanvas.SetActive(true);
                    CharacterEquipmentCanvas.SetActive(false);
                    CharacterCraftingCanvas.SetActive(false);
                    BuildingUICanvas.SetActive(false);
                    buildSystem.enabled = false; // Disable BuildSystem in combat mode
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case 1:
                    CombatCameraSet.SetActive(false);
                    BuildingCameraSet.SetActive(false);
                    CharacterEquipmentCameraSet.SetActive(true);
                    CharacterCraftingCameraSet.SetActive(false);
                    CombatUICanvas.SetActive(false);
                    CharacterEquipmentCanvas.SetActive(true);
                    CharacterCraftingCanvas.SetActive(false);
                    BuildingUICanvas.SetActive(false);
                    buildSystem.enabled = false; // Disable BuildSystem in equipment mode
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case 2:
                    CombatCameraSet.SetActive(false);
                    BuildingCameraSet.SetActive(false);
                    CharacterEquipmentCameraSet.SetActive(false);
                    CharacterCraftingCameraSet.SetActive(true);
                    CombatUICanvas.SetActive(false);
                    CharacterEquipmentCanvas.SetActive(true);
                    CharacterCraftingCanvas.SetActive(true);
                    BuildingUICanvas.SetActive(false);
                    buildSystem.enabled = false; // Disable BuildSystem in crafting mode
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case 3:
                    CombatCameraSet.SetActive(false);
                    BuildingCameraSet.SetActive(true);
                    CharacterEquipmentCameraSet.SetActive(false);
                    CharacterCraftingCameraSet.SetActive(false);
                    CombatUICanvas.SetActive(false);
                    CharacterEquipmentCanvas.SetActive(false);
                    CharacterCraftingCanvas.SetActive(false);
                    BuildingUICanvas.SetActive(true);
                    buildSystem.enabled = true; // Enable BuildSystem in building mode
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
        }
    }
    private void LateUpdate()
    {
        if (currentCameraSet != 0) return;

        RefreshEnergyBar();
        UpdateAmmoBarColors();
    }
    public void RefreshEnergyBar()
    {
        if (energyBar == null) return;

        var stats = PlayerStats.Instance;
        if (stats == null) return;

        float max = Mathf.Max(1f, stats.maxEnergy); // �קK maxEnergy = 0 �y�����X�z���A
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
    public void changeLeftHandAmmoBar(float fillAmount)
    {
        if (leftHandAmmoBar != null)
        {
            leftHandAmmoBar.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }
    public void changeRightHandAmmoBar(float fillAmount)
    {
        if (rightHandAmmoBar != null)
        {
            rightHandAmmoBar.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }
    public void changeLeftShoulderAmmoBar(float fillAmount)
    {
        if (leftShoulderAmmoBar != null)
            leftShoulderAmmoBar.fillAmount = Mathf.Clamp01(fillAmount);
    }

    public void changeRightShoulderAmmoBar(float fillAmount)
    {
        if (rightShoulderAmmoBar != null)
            rightShoulderAmmoBar.fillAmount = Mathf.Clamp01(fillAmount);
    }

    public void SetAmmoState(
    float leftHandFill, bool leftHandReloading,
    float rightHandFill, bool rightHandReloading,
    float leftShoulderFill, bool leftShoulderReloading,
    float rightShoulderFill, bool rightShoulderReloading)
    {
        _leftReloading = leftHandReloading;
        _rightReloading = rightHandReloading;
        _leftShoulderReloading = leftShoulderReloading;
        _rightShoulderReloading = rightShoulderReloading;

        changeLeftHandAmmoBar(leftHandFill);
        changeRightHandAmmoBar(rightHandFill);
        changeLeftShoulderAmmoBar(leftShoulderFill);
        changeRightShoulderAmmoBar(rightShoulderFill);

        UpdateAmmoBarColors();
    }

    public void SetAmmoNormalized(float leftFill, float rightFill)
    {
        changeLeftHandAmmoBar(leftFill);
        changeRightHandAmmoBar(rightFill);
    }

    private void UpdateAmmoBarColors()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * ammoReloadFlashSpeed * Mathf.PI * 2f) + 1f) * 0.5f; // 0..1

        if (leftHandAmmoBar != null)
            leftHandAmmoBar.color = _leftReloading ? Color.Lerp(ammoReloadColor, Color.red, pulse) : ammoNormalColor;

        if (rightHandAmmoBar != null)
            rightHandAmmoBar.color = _rightReloading ? Color.Lerp(ammoReloadColor, Color.red, pulse) : ammoNormalColor;

        // ✅ NEW: shoulder colors
        if (leftShoulderAmmoBar != null)
            leftShoulderAmmoBar.color = _leftShoulderReloading ? Color.Lerp(ammoReloadColor, Color.red, pulse) : ammoNormalColor;

        if (rightShoulderAmmoBar != null)
            rightShoulderAmmoBar.color = _rightShoulderReloading ? Color.Lerp(ammoReloadColor, Color.red, pulse) : ammoNormalColor;
    }
    public void SetAmmoBarSize(float lockOnRange)
    {
        
        float newSize = lockOnRange*0.533f;
        float newSize2 = lockOnRange * 0.566f;
        leftHandAmmoFrame.rectTransform.sizeDelta = new Vector2(newSize, newSize);
        leftHandAmmoFrame.rectTransform.anchoredPosition = new Vector2(-(newSize / 2), -(newSize / 2));
        leftHandAmmoBar.rectTransform.sizeDelta = new Vector2(newSize, newSize);
        rightHandAmmoFrame.rectTransform.sizeDelta = new Vector2(newSize, newSize);
        rightHandAmmoFrame.rectTransform.anchoredPosition = new Vector2((newSize / 2), -(newSize / 2));
        rightHandAmmoBar.rectTransform.sizeDelta = new Vector2(newSize, newSize);
        leftShoulderAmmoFrame.rectTransform.sizeDelta = new Vector2(newSize2, newSize2);
        leftShoulderAmmoFrame.rectTransform.anchoredPosition = new Vector2(-(newSize2 / 2), -(newSize2 / 2));
        leftShoulderAmmoBar.rectTransform.sizeDelta = new Vector2(newSize2, newSize2);
        rightShoulderAmmoFrame.rectTransform.sizeDelta = new Vector2(newSize2, newSize2);
        rightShoulderAmmoFrame.rectTransform.anchoredPosition = new Vector2((newSize2 / 2), -(newSize2 / 2));
        rightShoulderAmmoBar.rectTransform.sizeDelta = new Vector2(newSize2, newSize2);
    }
}
