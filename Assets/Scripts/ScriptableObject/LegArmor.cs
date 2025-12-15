using UnityEngine;
[System.Serializable]
public class VisualChange
{
    public float heightOffset;
    public AnimationType animationType;
}

[CreateAssetMenu(fileName = "New Armor", menuName = "Inventory/LegArmor")]
public class LegArmor :Armor
{
    public VisualChange visualChange;
}
