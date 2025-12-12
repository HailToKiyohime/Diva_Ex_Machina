using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject CombatCameraSet;
    [SerializeField] private GameObject CharacterEquipmentCameraSet;
    [SerializeField] private GameObject CharacterCraftingCameraSet;

    [SerializeField] private GameObject CombatUICanvas;
    [SerializeField] private GameObject CharacterEquipmentCanvas;
    [SerializeField] private GameObject CharacterCraftingCanvas;
    public int currentCameraSet = 0; // 0: Combat, 1: Equipment, 2: Crafting

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
}
